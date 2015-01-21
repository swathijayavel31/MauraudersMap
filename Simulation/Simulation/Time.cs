using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulation
{
    class Time
    {
        public const double MIN_PER_SIM_DAY = 60.0;
        public const double HR_PER_SIM_DAY = MIN_PER_SIM_DAY / 60;
        public const double DAY_PER_SIM_DAY = HR_PER_SIM_DAY / 24;
        public const double SEC_PER_SIM_DAY = MIN_PER_SIM_DAY * 60;
        public const double MILLISEC_PER_SIM_DAY = SEC_PER_SIM_DAY * 1000;

        public const double MIN_PER_SIM_MIN = MIN_PER_SIM_DAY / 24 / 60;
        public const double HR_PER_SIM_HR = HR_PER_SIM_DAY / 24;
        public const double SEC_PER_SIM_SEC = SEC_PER_SIM_DAY / 24 / 60 / 60;
        public const double MILLISEC_PER_SIM_MILLISEC = MILLISEC_PER_SIM_DAY / 24 / 60 / 60 / 1000;

        public static double get_real_days(double sim_days)
        {
            return sim_days * DAY_PER_SIM_DAY;
        }

        public static double get_real_hrs(double sim_hrs)
        {
            return sim_hrs * HR_PER_SIM_HR;
        }

        public static double get_real_mins(double sim_mins)
        {
            return sim_mins * MIN_PER_SIM_MIN;
        }

        public static double get_real_secs(double sim_secs)
        {
            return sim_secs * SEC_PER_SIM_SEC;
        }

        public static double get_real_millisecs(double sim_millisecs)
        {
            return sim_millisecs * MILLISEC_PER_SIM_MILLISEC;
        }

        public static double get_sim_days(double real_days)
        {
            return real_days / DAY_PER_SIM_DAY;
        }

        public static double get_sim_hrs(double real_hrs)
        {
            return real_hrs / HR_PER_SIM_HR;
        }

        public static double get_sim_mins(double real_mins)
        {
            return real_mins / MIN_PER_SIM_MIN;
        }

        public static double get_sim_secs(double real_secs)
        {
            return real_secs / SEC_PER_SIM_SEC;
        }

        public static double get_sim_millisecs(double real_millisecs)
        {
            return real_millisecs / MILLISEC_PER_SIM_MILLISEC;
        }
    }
}