// algo
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

        public static DateTime RoundDown(this DateTime dt, TimeSpan d)
        {
            var delta = dt.Ticks % d.Ticks;
            return new DateTime(dt.Ticks - delta, dt.Kind);
        }

        public static bool SymbolSeconds(string period, out int value)
        {
            return symbolPeriodCache.TryGetValue(period.ToString(), out value);
            ;
        }
    }
}
