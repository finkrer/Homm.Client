using System;
using HoMM;
using HoMM.ClientClasses;

namespace Homm.Client
{
	internal static class Extensions
	{
		public static void MoveTowards(this HommClient client, Location location, Pathfinder pathfinder)
		{
		    if (location.IsEqualTo(pathfinder.HeroLocation))
		    {
		        client.Wait(0.2);
		        return;
		    }
            var path = pathfinder.FindPathTo(location);
            if (path.Count != 0)
				client.Move(path[0]);
			else
				throw new Exception("Невозможно найти путь");
		}

	    public static bool IsEqualTo(this Location location, Location other)
	    {
	        return location.X == other.X && location.Y == other.Y;
	    }

	    public static bool IsEqualTo(this LocationInfo location, LocationInfo other)
	    {
	        return location.X == other.X && location.Y == other.Y;
	    }
    }
}
