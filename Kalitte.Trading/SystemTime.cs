// algo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    public class SystemTime
    {
        private static SystemTime Instance { get; set; }
        private DateTime? Time { get; set; } = null;

        internal static DateTime Set(DateTime t)
        {
            return (DateTime)(Instance.Time = t);
        }

        private SystemTime()
        {

        }

        static SystemTime()
        {
            Instance = new SystemTime();
        }

        public static DateTime Now
        {
            get
            {
                return Instance.Time ?? DateTime.Now;
            }
        }

    }
}
