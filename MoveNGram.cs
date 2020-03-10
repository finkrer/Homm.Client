using System.Linq;
using HoMM.ClientClasses;

namespace Homm.Client
{
    internal class MoveNGram : IMove
    {
        private readonly IMove[] moves;

        public MoveNGram(params IMove[] moves)
        {
            this.moves = moves;
        }

        public double GetPriority()
        {
            return moves.Sum(x => x.GetPriority());
        }

        public void Invoke()
        {
            moves[0].Invoke();
        }

        public HommSensorData GetNewSensorData()
        {
            return moves[moves.Length - 1].GetNewSensorData();
        }
    }
}
