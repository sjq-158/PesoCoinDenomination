using System.Drawing;

namespace PesoCoinDenomination
{
    public class Coin
    {
        public int area; 
        public int minX = int.MaxValue, minY = int.MaxValue;
        public int maxX = -1, maxY = -1; 

        public bool hasHole; // hole in the middle = 5 centavo
        public double diameter;

        public int type; // 0 = 5centavo, 1 = 10centavo , 2 = 25centavo, 3 = 1 peso, 4 = 5 peso
        public string name;
        public decimal value;
        public Color color;
    }
}