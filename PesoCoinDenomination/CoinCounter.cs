using System;
using System.Collections.Generic;
using System.Drawing;

namespace PesoCoinDenomination
{
    public class CoinResult
    {
        public Bitmap annotated;
        public List<Coin> coins;
        public int threshold;
        public string details;
    }

    public class CoinCounter
    {
        // type index: 0 = 5centavo, 1 = 10centavo , 2 = 25centavo, 3 = 1 peso, 4 = 5 peso
        static string[] typeNames = { "5 centavo", "10 centavo", "25 centavo", "1 peso", "5 peso" };
        static decimal[] typeValues = { 0.05m, 0.10m, 0.25m, 1m, 5m };
        static Color[] typeColors = { Color.Magenta, Color.Red, Color.Orange, Color.LimeGreen, Color.DeepSkyBlue };

        public static CoinResult process(Bitmap source)
        {
            int width = source.Width, height = source.Height;

            byte[] gray = toGrayScale(source);

            int threshold = findThreshold(gray);
            bool[] isCoin = applyThreshold(gray, threshold);

            List<Coin> coins = findCoins(isCoin, width, height);
            measureCoins(coins, isCoin, width, height);
            classifyCoins(coins);

            Bitmap annotated = drawResult(source, coins);

            // Result
            CoinResult result = new CoinResult();
            result.annotated = annotated;
            result.coins = coins;
            result.threshold = threshold;
            result.details = buildDetails(coins, threshold);
            return result;
        }

        static byte[] toGrayScale(Bitmap source)
        {
            byte[] gray = new byte[source.Width * source.Height];
            for (int x = 0; x < source.Width; x++) {
                for (int y = 0; y < source.Height; y++) {
                    Color pixel = source.GetPixel(x, y);
                    gray[y * source.Width + x] = (byte)((pixel.R + pixel.G + pixel.B) / 3);
                }
            }
            return gray;
        }

        static int findThreshold(byte[] gray)
        {
            long sum = 0;
            for (int i = 0; i < gray.Length; i++) {
                sum += gray[i];
            }
            double t = (double)sum / gray.Length;

            for (int round = 0; round < 50; round++)
            {
                long darkSum = 0, brightSum = 0;
                int darkCount = 0, brightCount = 0;
                for (int i = 0; i < gray.Length; i++){
                    if (gray[i] < t) { 
                        darkSum += gray[i]; darkCount++;
                    }
                    else { 
                        brightSum += gray[i]; brightCount++; 
                    }
                }
                if (darkCount == 0 || brightCount == 0) {
                    break;
                }

                double newT = ((double)darkSum / darkCount + (double)brightSum / brightCount) / 2;
                
                if (Math.Abs(newT - t) < 0.5) { 
                    t = newT; break; 
                }

                t = newT;
            }
            return (int)t;
        }
        static bool[] applyThreshold(byte[] gray, int threshold)
        {
            bool[] isCoin = new bool[gray.Length];
            for (int i = 0; i < gray.Length; i++){
                isCoin[i] = gray[i] < threshold;
            }
            return isCoin;
        }

        static List<Coin> findCoins(bool[] isCoin, int width, int height)
        {
            List<Coin> coins = new List<Coin>();
            bool[] visited = new bool[isCoin.Length];
            int[] stack = new int[isCoin.Length];
            int minArea = width * height / 3000;

            for (int start = 0; start < isCoin.Length; start++)
            {
                if (!isCoin[start] || visited[start]) continue;

                Coin coin = new Coin();
                int top = 0;
                stack[top++] = start;
                visited[start] = true;

                while (top > 0)
                {
                    int p = stack[--top];
                    int x = p % width;
                    int y = p / width;

                    coin.area++;
                    if (x < coin.minX) { 
                        coin.minX = x;
                    }
                    if (x > coin.maxX) { 
                        coin.maxX = x;
                    }
                    if (y < coin.minY){ 
                        coin.minY = y;
                    }
                    if (y > coin.maxY)
                    {
                        coin.maxY = y;
                    }

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                            int q = ny * width + nx;
                            if (isCoin[q] && !visited[q])
                            {
                                visited[q] = true;
                                stack[top++] = q;
                            }
                        }
                    }
                }

                if (coin.area >= minArea){ 
                    coins.Add(coin); 
                }
            }
            return coins;
        }

        // Checks the size of each coin and whether it has a hole in the middle 
        static void measureCoins(List<Coin> coins, bool[] isCoin, int width, int height)
        {
            for (int i = 0; i < coins.Count; i++)
            {
                Coin coin = coins[i];
                int boxWidth = coin.maxX - coin.minX + 1;
                int boxHeight = coin.maxY - coin.minY + 1;
                coin.diameter = (boxWidth + boxHeight) / 2.0;

                // check if there is a hole in the middle of the coin 
                int centerX = (coin.minX + coin.maxX) / 2;
                int centerY = (coin.minY + coin.maxY) / 2;
                coin.hasHole = true;
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        int x = centerX + dx;
                        int y = centerY + dy;

                        if (x < 0 || y < 0 || x >= width || y >= height){ 
                            continue;
                        }

                        if (isCoin[y * width + x]){ 
                            coin.hasHole = false; 
                        }
                    }
                }
            }
        }

        static void classifyCoins(List<Coin> coins)
        {
            int solidCount = 0;
            for (int i = 0; i < coins.Count; i++)
                if (!coins[i].hasHole) solidCount++;

            double[] sizes = new double[solidCount];
            int index = 0;
            for (int i = 0; i < coins.Count; i++) {
                if (!coins[i].hasHole) sizes[index++] = coins[i].diameter;
            }

            // Sort the sizes using insertion sort
            for (int i = 1; i < solidCount; i++) {
                double key = sizes[i];
                int j = i - 1;
                while (j >= 0 && sizes[j] > key)
                {
                    sizes[j + 1] = sizes[j];
                    j--;
                }
                sizes[j + 1] = key;
            }

            // Find the 3 largest gaps between sizes to determine the cutoffs for coin types
            double[] cutoffs = new double[3];
            bool[] gapUsed = new bool[solidCount];
            for (int k = 0; k < 3; k++) {
                double bestGap = -1;
                int bestIndex = -1;

                for (int i = 0; i < solidCount - 1; i++) {
                    double gap = sizes[i + 1] - sizes[i];
                    if (!gapUsed[i] && gap > bestGap){
                        bestGap = gap;
                        bestIndex = i;
                    }
                }

                if (bestIndex >= 0) {
                    gapUsed[bestIndex] = true;
                    cutoffs[k] = (sizes[bestIndex] + sizes[bestIndex + 1]) / 2;  
                }else{ 
                    cutoffs[k] = double.MaxValue; 
                }
            }

            for (int a = 0; a < 3; a++) {
                for (int b = 0; b < 2 - a; b++) {
                    if (cutoffs[b] > cutoffs[b + 1])
                    {
                        double temp = cutoffs[b];
                        cutoffs[b] = cutoffs[b + 1];

                        cutoffs[b + 1] = temp;
                    }
                }
            }

            // Assign coint types based on size and if there is a hole in the middle
            for (int i = 0; i < coins.Count; i++)
            {
                Coin coin = coins[i];
                if (coin.hasHole)
                {
                    coin.type = 0;
                }
                else
                {
                    int rank = 0;
                    for (int k = 0; k < 3; k++){
                        if (coin.diameter > cutoffs[k]) {
                            rank++;
                        }
                    }
                    coin.type = 1 + rank;
                }

                coin.name = typeNames[coin.type];
                coin.value = typeValues[coin.type];
                coin.color = typeColors[coin.type];
            }
        }

        static Bitmap drawResult(Bitmap source, List<Coin> coins)
        {
            Bitmap result = new Bitmap(source.Width, source.Height);
            for (int x = 0; x < source.Width; x++) {
                for (int y = 0; y < source.Height; y++) {
                    result.SetPixel(x, y, source.GetPixel(x, y));
                }
            }
            for (int i = 0; i < coins.Count; i++) {
                Coin coin = coins[i];
                for (int thickness = 0; thickness < 3; thickness++)
                {
                    int left = coin.minX - thickness, right = coin.maxX + thickness;
                    int top = coin.minY - thickness, bottom = coin.maxY + thickness;

                    for (int x = left; x <= right; x++)
                    {
                        setSafe(result, x, top, coin.color);
                        setSafe(result, x, bottom, coin.color);
                    }
                    for (int y = top; y <= bottom; y++)
                    {
                        setSafe(result, left, y, coin.color);
                        setSafe(result, right, y, coin.color);
                    }
                }
            }
            return result;
        }

        static void setSafe(Bitmap bitmap, int x, int y, Color color)
        {
            if (x >= 0 && y >= 0 && x < bitmap.Width && y < bitmap.Height) {
                bitmap.SetPixel(x, y, color);
            }
        }

        static string buildDetails(List<Coin> coins, int threshold)
        {
            string newline = Environment.NewLine;
            string text = "PESO COIN COUNTER" + newline;

            text += "Threshold: " + threshold + newline;
            text += "Coins detected: " + coins.Count + newline;
            text += "(box color = denomination)" + newline + newline;

            decimal total = 0;
            for (int t = 0; t < 5; t++)
            {
                int count = 0;
                for (int i = 0; i < coins.Count; i++)
                    if (coins[i].type == t) count++;

                decimal subtotal = count * typeValues[t];
                total += subtotal;
                text += typeNames[t] + " (" + typeColors[t].Name + ")" + newline;
                text += "   " + count + " coins = P" + subtotal.ToString("0.00") + newline;
            }

            text += newline + "TOTAL: P" + total.ToString("0.00");
            return text;
        }
    }
}