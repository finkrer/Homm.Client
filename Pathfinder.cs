using System.Collections.Generic;
using System.Linq;
using AlgoKit.Collections.Heaps;
using HoMM;
using HoMM.ClientClasses;

namespace Homm.Client
{
	internal class Pathfinder
	{
	    public readonly HommSensorData SensorData;
		public readonly Location HeroLocation;
		public readonly Dictionary<Location, MapObjectData> ObjectMap;
		private readonly Dictionary<Location, Location> previousLocation = new Dictionary<Location, Location>();
	    public readonly Dictionary<Location, double> TravelTimes;
        private static readonly Dictionary<Terrain, double> movementCost = new Dictionary<Terrain, double>
		{
			[Terrain.Road] = 0.75,
			[Terrain.Grass] = 1,
			[Terrain.Desert] = 1.15,
			[Terrain.Snow] = 1.3,
			[Terrain.Marsh] = 1.3
		};
		public readonly MapObjects ReachableObjects = new MapObjects();
        private static readonly Location leftSpawn = Location.Zero;
	    private readonly Location rightSpawn;
	    public readonly Dictionary<Resource, int> OwnedMines = new Dictionary<Resource, int>
	    {
            [Resource.Gold] = 0,
            [Resource.Ebony] = 0,
            [Resource.Glass] = 0,
            [Resource.Iron] = 0
	    };

        public Pathfinder(HommSensorData sensorData, Dictionary<Location, MapObjectData> memorizedMap)
		{
		    var map = sensorData.Map;
			SensorData = sensorData;
			HeroLocation = sensorData.Location.ToLocation();
		    rightSpawn = new Location(map.Height - 1, map.Width - 1);
		    var updatedMap = map.Objects.ToDictionary(x => x.Location.ToLocation(), x => x);
		    ObjectMap = memorizedMap;
		    foreach (var objectData in updatedMap)
		    {
		        memorizedMap[objectData.Key] = objectData.Value;
		    }
		    TravelTimes = new Dictionary<Location, double> { [HeroLocation] = 0 };
            FindPaths();
		}

		private void FindPaths()
		{
			var frontier = new PairingHeap<double, Location>(new DoubleComparer()) {{0, HeroLocation}};
			CategorizeObject(ObjectMap[HeroLocation]);

			while (frontier.Count != 0)
			{
				var current = frontier.Pop().Value;
				for (var d = Direction.Up; d <= Direction.RightDown; d++)
				{
					var next = current.NeighborAt(d);
					if (IsEnemySpawn(next)
                        || !ObjectMap.ContainsKey(next))
						continue;
					var mapObject = ObjectMap[next];
					CategorizeObject(mapObject);
					var nextTime = TravelTimes[current] + 0.5 * movementCost[mapObject.Terrain];
					if (TravelTimes.ContainsKey(next) && !(nextTime < TravelTimes[next])) continue;
					TravelTimes[next] = nextTime;
					if (IsPassable(mapObject))
						frontier.Add(nextTime, next);
					previousLocation[next] = current;
				}
			}
		}

		public List<Direction> FindPathTo(Location location)
		{
			var result = new List<Direction>();
			var locations = new List<Location> {location};
			if (!previousLocation.ContainsKey(location))
				return result;
			var current = location;
			while (current != HeroLocation)
			{
				current = previousLocation[current];
				locations.Add(current);
			}
			locations.Reverse();
			for (var i = 0; i < locations.Count - 1; i++)
				result.Add(locations[i].GetDirectionTo(locations[i+1]));
			return result;
		}

		private void CategorizeObject(MapObjectData mapObject)
		{
		    var isHostileArmy = mapObject.Garrison != null && mapObject.Garrison.Owner != SensorData.MyRespawnSide ||
		                        mapObject.Hero != null && mapObject.Hero.Name != SensorData.MyRespawnSide ||
		                        mapObject.NeutralArmy != null;
		    var isResourcePile = mapObject.ResourcePile != null;
		    var isNonFriendlyMine = mapObject.Mine != null && mapObject.Mine.Owner != SensorData.MyRespawnSide;
            var isFriendlyMine = mapObject.Mine != null && mapObject.Mine.Owner == SensorData.MyRespawnSide;
            var isDwelling = mapObject.Dwelling != null;

            if (isHostileArmy || isResourcePile || isNonFriendlyMine)
                ReachableObjects.MoveTargets.Add(mapObject);
            else if (isDwelling)
                ReachableObjects.MoveAndHireTargets.Add(mapObject);
		    if (isFriendlyMine)
		        OwnedMines[mapObject.Mine.Resource]++;
		}

		private static bool IsPassable(MapObjectData mapObject)
		{
			return mapObject.Garrison == null && mapObject.Hero == null && mapObject.NeutralArmy == null &&
			       mapObject.Wall == null;
		}

	    private bool IsEnemySpawn(Location location)
	    {
	        var player = SensorData.MyRespawnSide;
	        return player == "Left" ? location.IsEqualTo(rightSpawn) : location.IsEqualTo(leftSpawn);
	    }

		private class DoubleComparer : IComparer<double>
		{
			public int Compare(double x, double y)
			{
				return x.CompareTo(y);
			}
		}
	}
}
