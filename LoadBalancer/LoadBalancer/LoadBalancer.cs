using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using System.IO;

namespace LoadBalancer
{
    class ServerStatus
    {
        private int id;
        private double lat;
        private double lng;
        private string[] addresses;
        private Hashtable players;
        public ServerStatus(int id, double lat, double lng,string[] addresses)
        {
            this.id = id;
            this.lat = lat;
            this.lng = lng;
            this.addresses = addresses;
            players = new Hashtable();
        }

        public void update_addresses(string[] addr)
        {
            this.addresses = addr;
        }

        public void increment_players(int id)
        {
            //if(!players.Contains(id))
            players.Add(id, 1);
        }

        public void decrement_players(int id)
        {
            players.Remove(id);
        }

        public int num_players()
        {
            return players.Count;
        }

        public double get_lat()
        {
            return lat;
        }

        public double get_lng()
        {
            return lng;
        }

        public string[] get_addresses()
        {
            return addresses;
        }

        internal void reset_players()
        {
            players = new Hashtable();
        }
    }
 
    class LoadBalancer
    {
        public const int POOL_SIZE = 15;
        public const int SERVER_SIZE = 5;
        private IPAddress self_address;
        private int port;
        private ServerStatus[] servers;
        private Queue workers;
        private readonly object sync;
        public LoadBalancer(string self_ip, int self_port)
        {
            servers = new ServerStatus[SERVER_SIZE];
            for (int i = 0; i < SERVER_SIZE; i++)
            {
                servers[i] = null;
            }
            self_address = IPAddress.Parse(self_ip);
            port = self_port;
            workers = new Queue();
            sync = new object();
            for(int i = 0; i < POOL_SIZE ; i++)
            {
                lock (sync)
                {
                    workers.Enqueue(new Worker(i, this));
                }
            }

            TcpListener listener = new TcpListener(self_address, port);
            listener.Start();
            while(true)
            {
                TcpClient client= listener.AcceptTcpClient();
                Console.WriteLine("Client connected to Load Balancer. Awaiting delegation!");
                Worker current;

                lock(sync)
                {
                    while (workers.Count == 0)
                        Monitor.Wait(sync);
                    current = (Worker)workers.Dequeue();
                }
                current.wake_up(client);
            }
        }

        public void requeue(Worker w)
        {
            lock(sync)
            {
                workers.Enqueue(w);
                Monitor.PulseAll(sync);
            }
        }

        public ServerStatus get_server(int id)
        {
            return servers[id];
        }

        public void set_server(int id, ServerStatus serv)
        {
            servers[id] = serv;
        }

        public void reset_players()
        {
            for (int i = 0; i < SERVER_SIZE; i++)
            {
                if (servers[i] != null)
                {
                    servers[i].reset_players();
                }
            }
        }

        public int place_player(double lat, double lng, int p_id)
        {
            double min_distance = Double.MaxValue;
            int min_id = 0;
            for(int i = 0 ; i < SERVER_SIZE ; i++)
            {
                if(servers[i] != null)
                {
                    double dist = distance(lat, lng, servers[i].get_lat(), servers[i].get_lng());
                    if(dist < min_distance)
                    {
                        min_distance = dist;
                        min_id = i;
                    }
                }
            }

            servers[min_id].increment_players(p_id);
            return min_id;
        }

        public static double distance(double lat1, double lng1, double lat2, double lng2)
        {
            double MILES_PER_LAT = 69;
            double MILES_PER_LNG = 49;
            double lat_diff_in_feet = (lat2 - lat1) * MILES_PER_LAT * 5280;
            double lng_diff_in_feet = (lng2 - lng1) * MILES_PER_LNG * 5280;
            return Math.Sqrt(Math.Pow(lat_diff_in_feet, 2.0) + Math.Pow(lng_diff_in_feet, 2.0));
        }
    }

    class Worker
    {
        private readonly object sync;
        private int id;
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;
        private LoadBalancer balancer;

        public Worker(int id,LoadBalancer b)
        {
            this.balancer = b;
            sync = new object();
            client = null;
            reader = null;
            writer = null;
            this.id = id;
            Console.WriteLine("Worker " + id.ToString() + " connected.");
            Thread thread = new Thread(new ThreadStart(run));
            thread.Start();
        }

        public void wake_up(TcpClient client)
        {
            lock(sync)
            {
                this.client = client;
                Monitor.PulseAll(sync);
            }
        }

        private void handle_heartbeat(string[] info)
        {
            Console.WriteLine("Twas a heartbeat controller");
            //This is never going to exit from here or re-enter the thread pool
            string[] current = info;
            while(true)
            {
                if(!current[0].Equals("h") || !current[current.Length-1].Equals("h"))
                {
                    //break out! something went wrong
                    Console.WriteLine("Broke.");
                    return;
                }
                int id = Int32.Parse(current[1]);
                double lat = Convert.ToDouble(current[2]);
                double lng = Convert.ToDouble(current[3]);
                string[] addresses = new string[current.Length - 5];
                for(int i = 4; i < current.Length-1; i++)
                {
                    addresses[i - 4] = current[i];
                    Console.WriteLine(addresses[i - 4]);
                }
                ServerStatus curr = balancer.get_server(id);
                if(curr != null)
                {
                    curr.update_addresses(addresses);
                    balancer.set_server(id, curr);
                }
                else
                {
                    curr = new ServerStatus(id, lat, lng, addresses);
                    balancer.set_server(id, curr);
                }
                Console.WriteLine("Set configuration. Now Waiting for new configuration");
                string msg = reader.ReadLine().Trim(); //blocks until new config sent
                Console.WriteLine("Got a new configuration!");
                current = msg.Split(new char[] { ',' });
                for (int i = 0; i < current.Length; i++)
                {
                    Console.WriteLine(current[i]);
                }
            }
        }

        private void handle_player(string[] info)
        {
            Console.WriteLine("Twas a player!");
            int p_id = Int32.Parse(info[1]);
            double lat = Convert.ToDouble(info[2]);
            double lng = Convert.ToDouble(info[3]);
            int serv = balancer.place_player(lat, lng,p_id);
            ServerStatus s = balancer.get_server(serv);
            string[] addr = s.get_addresses();
            string msg = "";
            for(int i = 0 ; i < addr.Length; i++)
            {
                msg += addr[i];
                if(i != addr.Length -1)
                    msg += ",";
            }
            writer.WriteLine(msg);
            string ack = reader.ReadLine().Trim();
            if(!ack.Equals("FIN"))
            {
                Console.WriteLine("Incorrect termination message from client.");
            } 
        }

        public void run()
        {
            while (true)
            {
                lock (sync)
                {
                    while (client == null)
                        Monitor.Wait(sync);
                }
                Console.WriteLine("Got a new connection!");
                reader = new StreamReader(client.GetStream(), Encoding.ASCII);
                writer = new StreamWriter(client.GetStream(), Encoding.ASCII);
                writer.AutoFlush = true;
                string ret = reader.ReadLine();
                Console.WriteLine("Got info from connectee!");
                Console.WriteLine(ret);
                string[] info = ret.Split(new char[] { ',' });
                if (info[0].Equals("h"))
                    handle_heartbeat(info);
                else if (info[0].Equals("p"))
                    handle_player(info);
                else if (info[0].Equals("k"))
                {
                    balancer.reset_players();
                    writer.WriteLine("FIN");
                }
                else
                    Console.WriteLine("Not a Heartbeat or a Player");
                lock(sync)
                {
                    client.Close();
                    reader = null;
                    writer = null;
                    client = null;
                }
                balancer.requeue(this);
            }
        }
    }
}
