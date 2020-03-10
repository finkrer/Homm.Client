using System;
using DeepCopyExtensions;
using HoMM;
using HoMM.ClientClasses;

namespace Homm.Client
{
    internal class MoveAndHire : Move
    {
        protected override double TimeToMake => base.TimeToMake + 0.5;

        public MoveAndHire(HommClient client, Pathfinder pathfinder, HommSensorData sensorData,
            MapObjectData target) : base(client, pathfinder, sensorData, target)
        {
        }

        protected override double GetScoreGain()
        {
            var score = CalculateAmount() * (Target.Dwelling.UnitType == UnitType.Cavalry ? 2 : 1);
            return score / TimeToMake;
        }

        public override void Invoke()
        {
            if (!SensorData.Location.IsEqualTo(Target.Location))
                base.Invoke();
            else
            {
                var amount = CalculateAmount();
                if (amount == 0)
                    return;
                Client.HireUnits(amount);
            }
        }

        public override HommSensorData GetNewSensorData()
        {
            var result = base.GetNewSensorData();
            var resultTarget = Target.DeepCopyByExpressionTree();
            resultTarget.Dwelling.AvailableToBuyCount -= CalculateAmount();
            result.Map.Objects.RemoveAll(x => x.Location.IsEqualTo(Target.Location));
            result.Map.Objects.Add(resultTarget);
            return result;
        }

        private int CalculateAmount()
        {
            int amount;
            switch (Target.Dwelling.UnitType)
            {
                case UnitType.Militia:
                    amount = Math.Min(Target.Dwelling.AvailableToBuyCount, SensorData.MyTreasury[Resource.Gold]);
                    break;
                case UnitType.Infantry:
                    amount = Min(Target.Dwelling.AvailableToBuyCount, SensorData.MyTreasury[Resource.Gold],
                        SensorData.MyTreasury[Resource.Iron]);
                    break;
                case UnitType.Ranged:
                    amount = Min(Target.Dwelling.AvailableToBuyCount, SensorData.MyTreasury[Resource.Gold],
                        SensorData.MyTreasury[Resource.Glass]);
                    break;
                case UnitType.Cavalry:
                    amount = Min(Target.Dwelling.AvailableToBuyCount, SensorData.MyTreasury[Resource.Gold] / 2,
                        SensorData.MyTreasury[Resource.Ebony] / 2);
                    break;
                default:
                    throw new Exception("Invalid unit type");
            }
            return amount;
        }

        private static int Min(int num1, int num2, int num3)
        {
            return Math.Min(Math.Min(num1, num2), Math.Min(num2, num3));
        }
    }
}
