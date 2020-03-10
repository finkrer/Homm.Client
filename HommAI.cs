//#define bigrams
using System;
using System.Collections.Generic;
using System.Linq;
using DeepCopyExtensions;
using HoMM;
using HoMM.ClientClasses;
using MoreLinq;

namespace Homm.Client
{
	internal class HommAi
	{
		private readonly HommClient client;
	    private readonly Dictionary<Location, MapObjectData> memorizedMap = new Dictionary<Location, MapObjectData>();
	    private int timesDwellingsUpdated;

		public HommAi(HommClient client)
		{
			this.client = client;
		}

		public void Act(HommSensorData sensorData)
		{
		    if (timesDwellingsUpdated < (int) sensorData.WorldCurrentTime / (7 * 5))
                UpdateDwellings();
            foreach (var pair in memorizedMap.Where(x => x.Value.Hero != null).ToList())
            {
                memorizedMap.Remove(pair.Key);
            }
            var pathfinder = new Pathfinder(sensorData, memorizedMap);
		    var possibleMoves = new List<IMove> { new Wait(client, sensorData) };
            possibleMoves.AddRange(pathfinder.ReachableObjects.MoveTargets.Select(target => new Move(client, pathfinder, sensorData, target)));
            possibleMoves.AddRange(pathfinder.ReachableObjects.MoveAndHireTargets.Select(target => new MoveAndHire(client, pathfinder, sensorData, target)));
#region bigrams
#if bigrams
            possibleMoves.RemoveAll(m => m.GetPriority() <= 0);
            var possibleBigrams = possibleMoves
                .AsParallel()
                .Select(m => new Pathfinder(m.GetNewSensorData(), memorizedMap.DeepCopyByExpressionTree()))
                .Select(p =>
                {
                    var result = new List<IMove> { new Wait(client, sensorData) };
                    result.AddRange(p.ReachableObjects.MoveTargets.Select(target => new Move(client, p, p.SensorData, target)));
                    result.AddRange(p.ReachableObjects.MoveAndHireTargets.Select(target => new MoveAndHire(client, p, p.SensorData, target)));
                    return result;
                })
                .Zip(possibleMoves.AsParallel(), (list, move) =>
                {
                    var result = new List<MoveNGram>();
                    result.AddRange(list.Select(move1 => new MoveNGram(move, move1)));
                    return result;
                })
                .SelectMany(x => x)
                .ToList();
            try
            {
		        possibleBigrams.MaxBy(m => m.GetPriority()).Invoke();
            }
		    catch (Exception)
		    {
		    }
#endif
#endregion
#if !bigrams
            try
		    {
		        possibleMoves.MaxBy(m => m.GetPriority()).Invoke();
		    }
		    catch (Exception)
		    {
		    }
#endif
        }

	    private void UpdateDwellings()
	    {
	        foreach (var dwelling in memorizedMap.Values.Where(v => v.Dwelling != null).Select(v => v.Dwelling))
	        {
	            dwelling.AvailableToBuyCount = Math.Min(32,
	                dwelling.AvailableToBuyCount + dwelling.UnitType == UnitType.Cavalry ? 8 : 16);
	        }
	        timesDwellingsUpdated++;
	    }
	}
}
