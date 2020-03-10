using DeepCopyExtensions;
using HoMM;
using HoMM.ClientClasses;

namespace Homm.Client
{
	internal class Wait : IMove
	{
		private readonly HommClient client;
	    private readonly HommSensorData sensorData;

		public Wait(HommClient client, HommSensorData sensorData)
		{
		    this.client = client;
		    this.sensorData = sensorData;
		}

		public double GetPriority() => 0;

	    public HommSensorData GetNewSensorData()
	    {
	        return sensorData.DeepCopyByExpressionTree();
	    }

		public void Invoke()
		{
			client.Wait(1);
		}
	}
}
