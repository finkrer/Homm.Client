using HoMM.ClientClasses;

namespace Homm.Client
{
	internal interface IMove
	{
		double GetPriority();
		void Invoke();
	    HommSensorData GetNewSensorData();
	}
}
