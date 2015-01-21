using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulation
{
    class Util
    {
        public const double MILES_PER_LAT = 69;
        public const double MILES_PER_LNG = 49;

        public static DateTime clone(DateTime d)
        {
            return new DateTime(
                d.Year,
                d.Month,
                d.Day,
                d.Hour,
                d.Minute,
                d.Second,
                d.Millisecond,
                d.Kind);
        }

        public static double mph_to_mps(double mph)
        {
            return mph / 60 / 60;
        }

        public static double distance(double lat1, double lng1, double lat2, double lng2)
        {
            double lat_diff_in_feet = (lat2 - lat1) * Util.MILES_PER_LAT * 5280;
            double lng_diff_in_feet = (lng2 - lng1) * Util.MILES_PER_LNG * 5280;
            return Math.Sqrt(Math.Pow(lat_diff_in_feet, 2.0) + Math.Pow(lng_diff_in_feet, 2.0));
        }
    }
}