using System.Collections.Generic;
using DeepCopyExtensions;
using HoMM;
using HoMM.ClientClasses;

namespace Homm.Client
{
    internal class Move : IMove
    {
        protected readonly HommClient Client;
        protected readonly Pathfinder Pathfinder;
        protected readonly HommSensorData SensorData;
        protected readonly MapObjectData Target;
        protected readonly Dictionary<UnitType, int> EnemyArmy;

        protected readonly Combat.CombatResult CombatResult;
        protected virtual double TimeToStart => Pathfinder.TravelTimes[Target.Location.ToLocation()];
        protected virtual double TimeToMake => TimeToStart + (EnemyArmy != null ? 2 : 0);

        public Move(HommClient client, Pathfinder pathfinder, HommSensorData sensorData, MapObjectData target)
        {
            Client = client;
            Pathfinder = pathfinder;
            Target = target;
            SensorData = sensorData;
            EnemyArmy = Target.NeutralArmy?.Army
                        ?? (Target.Hero?.Name != sensorData.MyRespawnSide ? Target.Hero?.Army : null)
                        ?? (Target.Garrison?.Owner != sensorData.MyRespawnSide ? Target.Garrison?.Army : null);
            if (EnemyArmy != null)
                CombatResult = Combat.Resolve(new ArmiesPair(SensorData.MyArmy, EnemyArmy));
        }

        public virtual double GetPriority()
        {
            if (SensorData.WorldCurrentTime + TimeToStart > 90)
                return -1;
            return GetScoreGain();
        }

        protected virtual double GetScoreGain()
        {
            var score = 0d;
            var currentTime = SensorData.WorldCurrentTime;
            var timeSoFar = currentTime + TimeToMake;
            if (EnemyArmy != null)
            {
                timeSoFar += 2;
                if (!CombatResult.IsAttackerWin)
                    return -1;
                for (var u = UnitType.Infantry; u <= UnitType.Militia; u++)
                {
                    if (!EnemyArmy.ContainsKey(u))
                        continue;
                    score += (EnemyArmy[u] - CombatResult.DefendingArmy.GetOrDefault(u)) * (u == UnitType.Cavalry ? 2 : 1);
                }
                if (Target.Hero != null)
                    score *= 5;
            }
            var resource = Target.ResourcePile;
            if (resource != null)
                score += resource.Amount / 10;
            var mine = Target.Mine;
            if (mine != null)
                score += (int)((90 - timeSoFar) / 5) * (mine.Resource == Resource.Gold ? 5 : 2) * (mine.Owner != SensorData.MyRespawnSide ? 2 : 1);
            return score / TimeToMake;
        }

        public virtual HommSensorData GetNewSensorData()
        {
            var result = SensorData.DeepCopyByExpressionTree();
            result.Location = new LocationInfo(Target.Location.X, Target.Location.Y);
            for (var r = Resource.Gold; r <= Resource.Ebony; r++)
            {
                if (!result.MyTreasury.ContainsKey(r))
                    continue;
                result.MyTreasury[r] += Pathfinder.OwnedMines[r] * 10;
            }
            result.WorldCurrentTime += TimeToMake;
            result.MyArmy = CombatResult?.AttackingArmy ?? result.MyArmy;
            var resultTarget = Target.DeepCopyByExpressionTree();
            if (EnemyArmy != null)
            {
                resultTarget.NeutralArmy = null;
                resultTarget.Garrison = null;
                resultTarget.Hero = null;
            }
            if (Target.Mine != null)
                resultTarget.Mine.Owner = result.MyRespawnSide;
            if (Target.ResourcePile != null)
            {
                result.MyTreasury[Target.ResourcePile.Resource] =
                    result.MyTreasury.GetOrDefault(Target.ResourcePile.Resource) + Target.ResourcePile.Amount;
                resultTarget.ResourcePile = null;
            }
            result.Map.Objects.RemoveAll(x => x.Location.IsEqualTo(Target.Location));
            result.Map.Objects.Add(resultTarget);
            return result;
        }

        public virtual void Invoke()
        {
            Client.MoveTowards(Target.Location.ToLocation(), Pathfinder);
        }
    }
}
