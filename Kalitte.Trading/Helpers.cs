// algo
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    public class RandomGenerator
    {

        readonly RNGCryptoServiceProvider csp;

        public RandomGenerator()
        {
            csp = new RNGCryptoServiceProvider();
        }



        public double NextDouble()
        {            
            var bytes = new Byte[8];
            csp.GetBytes(bytes);
            // Step 2: bit-shift 11 and 53 based on double's mantissa bits
            var ul = BitConverter.ToUInt64(bytes, 0) / (1 << 11);
            Double d = ul / (Double)(1UL << 53);
            return d;
        }

        public int Next(int minValue, int maxExclusiveValue)
        {
            if (minValue >= maxExclusiveValue)
                throw new ArgumentOutOfRangeException("minValue must be lower than maxExclusiveValue");

            long diff = (long)maxExclusiveValue - minValue;
            long upperBound = uint.MaxValue / diff * diff;

            uint ui;
            do
            {
                ui = GetRandomUInt();
            } while (ui >= upperBound);
            return (int)(minValue + (ui % diff));
        }

        private uint GetRandomUInt()
        {
            var randomBytes = GenerateRandomBytes(sizeof(uint));
            return BitConverter.ToUInt32(randomBytes, 0);
        }

        private byte[] GenerateRandomBytes(int bytesNumber)
        {
            byte[] buffer = new byte[bytesNumber];
            csp.GetBytes(buffer);
            return buffer;
        }
    }
    public static class Helper
    {
        private static Dictionary<string, int> symbolPeriodCache = new Dictionary<string, int>();
        
        static Helper()
        {
            symbolPeriodCache.Add(BarPeriod.Sec.ToString(), 1);
            symbolPeriodCache.Add(BarPeriod.Sec5.ToString(), 5);
            symbolPeriodCache.Add(BarPeriod.Sec10.ToString(), 10);
            symbolPeriodCache.Add(BarPeriod.Sec15.ToString(), 15);
            symbolPeriodCache.Add(BarPeriod.Sec20.ToString(), 20);
            symbolPeriodCache.Add(BarPeriod.Sec30.ToString(), 30);
            symbolPeriodCache.Add(BarPeriod.Sec45.ToString(), 45);
            symbolPeriodCache.Add(BarPeriod.Min.ToString(), 60);
            symbolPeriodCache.Add(BarPeriod.Min5.ToString(), 5 * 60);
            symbolPeriodCache.Add(BarPeriod.Min10.ToString(), 10 * 60);
            symbolPeriodCache.Add(BarPeriod.Min15.ToString(), 15 * 60);
            symbolPeriodCache.Add(BarPeriod.Min20.ToString(), 20 * 60);
            symbolPeriodCache.Add(BarPeriod.Min30.ToString(), 30 * 60);
            symbolPeriodCache.Add(BarPeriod.Min60.ToString(), 60 * 60);
            symbolPeriodCache.Add(BarPeriod.Min120.ToString(), 120 * 60);
            symbolPeriodCache.Add(BarPeriod.Min180.ToString(), 180 * 60);
            symbolPeriodCache.Add(BarPeriod.Min240.ToString(), 180 * 60);
        }

        public static DateTime RoundUp(DateTime dt, TimeSpan d)
        {
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }

        public static decimal Cross(decimal[] list, decimal baseVal, decimal delta = 0)
        {            
            var i = list.Length;
            var cv = 0M;
            decimal max = Decimal.MinValue;
            decimal min = Decimal.MaxValue;
            while (--i >= 1)
            {
                decimal cdif = list[i] - baseVal;
                decimal pdif = list[i - 1] - baseVal;
                max = cdif > max ? cdif : max;
                min = cdif < min ? cdif : min;
                if (cdif > 0 && pdif < 0) { cv = cdif; break; }
                else if (cdif < 0 && pdif > 0) { cv = cdif; break; }
            }
            if (cv > 0 && max > delta) return cv;
            if (cv < 0 && min < delta) return cv;
            return 0M;
            //return cv;
        }

        //public static CandlePart ToCandle(OHLCType ohlc)
        //{
        //    return (CandlePart)ohlc;
        //}

        public static decimal GetMultiplier(decimal value, decimal [] ranges, decimal [] multipliers)
        {
            for(var i = 0; i < ranges.Length; i++)
            {
                var next = i + 1;
                if (value < ranges[i]) return multipliers[i];
            }
            return multipliers.Last();
        }

        public static void Shuffle<T>(IList<T> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void ShuffleSimple<T>(IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static IList<T>[] SliceList<T>(IList<T> list, int slice)
        {
            var res = new List<IList<T>>();

            var count = list.Count;


            return res.ToArray();
        }

        public static void ShuffleParallel<T>(IList<T> list, int n)
        {
            
        }


        


        public static IEnumerable<object> Cartesian(IEnumerable<IEnumerable<object>> items)
        {
            var slots = items
               // initialize enumerators
               .Select(x => x.GetEnumerator())
               // get only those that could start in case there is an empty collection
               .Where(x => x.MoveNext())
               .ToArray();

            while (true)
            {
                // yield current values
                yield return slots.Select(x => x.Current);

                // increase enumerators
                foreach (var slot in slots)
                {
                    // reset the slot if it couldn't move next
                    if (!slot.MoveNext())
                    {
                        // stop when the last enumerator resets
                        if (slot == slots.Last()) { yield break; }
                        slot.Reset();
                        slot.MoveNext();
                        // move to the next enumerator if this reseted
                        continue;
                    }
                    // we could increase the current enumerator without reset so stop here
                    break;
                }
            }
        }
        public static DateTime RoundDown(this DateTime dt, TimeSpan d)
        {
            var delta = dt.Ticks % d.Ticks;
            return new DateTime(dt.Ticks - delta, dt.Kind);
        }

        public static bool SymbolSeconds(string period, out int value)
        {
            return symbolPeriodCache.TryGetValue(period, out value);
            ;
        }
    }
}
