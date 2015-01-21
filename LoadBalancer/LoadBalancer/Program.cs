using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoadBalancer
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length != 2)
            {
                Console.WriteLine("Usage LoadBalancer [IP address] [Port number]");
            }
            else
            {
                new LoadBalancer(args[0], Int32.Parse(args[1]));
            }
        }
    }
}
