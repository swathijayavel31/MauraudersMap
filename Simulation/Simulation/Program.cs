using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Simulation
{
    public class Info
    {
        public int user { get; set; }
        public double AverageOutstandingRequests { get; set; }
        public int LastOutstandingRequest { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new simulation_gui());
        }
    }
}
