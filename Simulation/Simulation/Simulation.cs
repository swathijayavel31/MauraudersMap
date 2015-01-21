using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Simulation
{
    class Simulation
    {
        public int NUM_PLAYERS; 

        // IP ADDRESSES
        public const string IP_ADDR_SWATHI = "10.32.6.248";
        public const string IP_ADDR_BRIAN = "10.33.129.27";
        public const string IP_ADDR_LAB_0 = "128.84.72.26";
        public const string IP_ADDR_LAB_1 = "10.32.6.23";
        public const string IP_ADDR_LAB_2 = "128.84.72.24";

        public int LB_PORT_NUM;
        public int offset;
        
        //For sending safe-zone and obstacle updates
        public const int MAX_SERVER_CONNECTION_TRIES = 100; // In milliseconds
        private TcpClient send_tcpclnt;
        private Info[] stats;
        private simulation_gui sg;
    

        // PRIVATE FIELDS
        private ArrayList players;
        private ArrayList obstacles_in_play;
        private ArrayList obstacles_available;
        private ArrayList safe_zones_in_play;
        private ArrayList safe_zones_available;
        private Mutex tag_stun_lock = new Mutex();
        private Random rand_gen = new Random();

        // PUBLIC FIELDS
        public IPAddress LB_IPADDR;

        // PUBLIC FIELDS
        public Mutex Tag_Stun_Lock { get { return tag_stun_lock; } }

        // METHODS
		public Simulation(int humans, int zombies,simulation_gui sg,string addr,int port, bool is_south)
        {
            LB_IPADDR = IPAddress.Parse(addr);
            LB_PORT_NUM = port;
            this.sg = sg;
            // Create obstacles
            obstacles_available = new ArrayList(2);

            List<Tuple<double, double>> pb0 = new List<Tuple<double, double>>(3);
            pb0.Add(new Tuple<double,double>(42.443143, -76.485124));
            pb0.Add(new Tuple<double,double>(42.443128, -76.484008));
            pb0.Add(new Tuple<double,double>(42.442130, -76.479974));
            obstacles_available.Add(new Obstacle(0, "River", pb0));

            List<Tuple<double, double>> pb1 = new List<Tuple<double, double>>(3);
            pb1.Add(new Tuple<double,double>(42.446245, -76.483820));
            pb1.Add(new Tuple<double,double>(42.446245, -76.482779));
            pb1.Add(new Tuple<double,double>(42.445572, -76.482736));
            pb1.Add(new Tuple<double,double>(42.445588, -76.483782));
            pb1.Add(new Tuple<double,double>(42.446245, -76.483820));
            obstacles_available.Add(new Obstacle(1, "Sage_Hall", pb1));

            obstacles_in_play = new ArrayList(2);
            //obstacles_in_play.Add(obstacles_available[0]);
            //obstacles_in_play.Add(obstacles_available[1]);

            // Create safe zones
            safe_zones_available = new ArrayList(2);

            List<Tuple<double, double>> ps0 = new List<Tuple<double, double>>(3);
            ps0.Add(new Tuple<double, double>(42.443904, -76.482352));
            ps0.Add(new Tuple<double, double>(42.443916, -76.482162));
            ps0.Add(new Tuple<double, double>(42.443557, -76.482140));
            ps0.Add(new Tuple<double, double>(42.443553, -76.482342));
            safe_zones_available.Add(new Safe_Zone(0, "CSUG", ps0));

            List<Tuple<double, double>> ps1 = new List<Tuple<double, double>>(3);
            ps1.Add(new Tuple<double, double>(42.445204, -76.481577));
            ps1.Add(new Tuple<double, double>(42.445240, -76.480413));
            ps1.Add(new Tuple<double, double>(42.444812, -76.480387));
            ps1.Add(new Tuple<double, double>(42.444856, -76.481325));
            ps1.Add(new Tuple<double, double>(42.444682, -76.481341));
            ps1.Add(new Tuple<double, double>(42.444662, -76.481502));
            safe_zones_available.Add(new Safe_Zone(1, "Gates", ps1));

            List<Tuple<double, double>> ps2 = new List<Tuple<double, double>>(15);
            ps2.Add(new Tuple<double, double>(42.451284, -76.485617));
            ps2.Add(new Tuple<double, double>(42.451293, -76.485140));
            ps2.Add(new Tuple<double, double>(42.451376, -76.483917));
            ps2.Add(new Tuple<double, double>(42.451372, -76.482678));
            ps2.Add(new Tuple<double, double>(42.451317, -76.482136));
            ps2.Add(new Tuple<double, double>(42.451158, -76.482469));
            ps2.Add(new Tuple<double, double>(42.450747, -76.482860));
            ps2.Add(new Tuple<double, double>(42.450537, -76.482941));
            ps2.Add(new Tuple<double, double>(42.447564, -76.482801));
            ps2.Add(new Tuple<double, double>(42.447473, -76.485865));
            ps2.Add(new Tuple<double, double>(42.447774, -76.485929));
            ps2.Add(new Tuple<double, double>(42.448439, -76.485602));
            ps2.Add(new Tuple<double, double>(42.449452, -76.485827));
            ps2.Add(new Tuple<double, double>(42.451301, -76.485940));

            safe_zones_in_play = new ArrayList(2);
            safe_zones_in_play.Add(new Safe_Zone(0,"Arts Quad",ps2));
            NUM_PLAYERS = humans + zombies;

            // Add players
            players = new ArrayList(NUM_PLAYERS);
            stats = new Info[NUM_PLAYERS];

            // Add first zombie
            for (int i = 0; i < zombies ; i++)
            {
                if(i == 0)
                    players.Add(new Zombie(this, 0, true, is_south));
                else
                    players.Add(new Zombie(this, i, false, is_south));
                stats[i] = new Info();
                stats[i].user = i;
                stats[i].AverageOutstandingRequests = 0.0;
                stats[i].LastOutstandingRequest = 0;
            }
                

            // Add humans
            for (int i = zombies; i < NUM_PLAYERS ; i++)
            {
                players.Add(new Human(this, i, is_south));
                stats[i] = new Info();
                stats[i].user = i;
                stats[i].AverageOutstandingRequests = 0.0;
                stats[i].LastOutstandingRequest = 0;
            }
            send_tcpclnt = new TcpClient();
        }

        public void update_stats(int id, int outstanding, int completed)
        {
            double prev = stats[id].AverageOutstandingRequests;
            stats[id].AverageOutstandingRequests = (prev * (completed - 1) + outstanding) / completed;
            stats[id].LastOutstandingRequest = outstanding;
            if (sg.get_stats_displayed() && rand_gen.NextDouble() < 0.0005)
            {
                sg.redraw_table();
            }
                
        }

        public Info[] get_stats()
        {
            return stats;
        }

        public void finish()
        {
            for (int i = 0; i < NUM_PLAYERS; i++)
            {
                ((Player)players[i]).terminate();
            }
            TcpClient lb = new TcpClient();
            lb.Connect(LB_IPADDR, LB_PORT_NUM);
            StreamWriter w = new StreamWriter(lb.GetStream(), Encoding.ASCII);
            StreamReader r = new StreamReader(lb.GetStream(), Encoding.ASCII);
            w.AutoFlush = true;
            w.WriteLine("k");
            r.ReadLine();
            lb.Close();
        }

        public void start()
        {
            for (int i = 0; i < NUM_PLAYERS; i++)
            {
                if (players[i] is Human) ((Human)players[i]).start();
                else ((Zombie)players[i]).start();
            }
        }



        // returns null if no internsection, otherwise a tuple of the intersection point
        public Tuple<double,double> find_obstacle_intersection(double lat0, double lng0, double lat1, double lng1)
        {
            Tuple<double, double> intersect_point = null;
            int i = 0;
            while (intersect_point == null && i < obstacles_in_play.Count)
            {
                intersect_point = ((Obstacle)obstacles_in_play[i]).find_intersection(lat0, lng0, lat1, lng1);
                i++;
            }
            return intersect_point;
        }


        // Returns -1 if in safe zone; the number of the safe zone otherwise
        public int get_safe_zone(double lat, double lng)
        {
            int safe_zone = -1;
            int i = 0;
            while (safe_zone == -1 && i < safe_zones_in_play.Count)
            {
                if (((Safe_Zone)safe_zones_in_play[i]).is_inside(lat, lng))
                    safe_zone = (((Safe_Zone)safe_zones_in_play[i])).Id;
                i++;
            }
      //      Console.WriteLine("Safe_Zone = " + safe_zone.ToString());
            return safe_zone;
        }

        // Returns array of Zombie ids chasing Human; if none returns null
        public int[] get_zombies_chasing(int human_id)
        {
            int[] zombies_chasing_arr = null;
            List<int> zombies_chasing_list = new List<int>();
            for (int i = 0; i < NUM_PLAYERS; i++)
            {
                if ((players[i] is Zombie) && ((Zombie)players[i]).Human_Chasing == human_id)
                {
                    zombies_chasing_list.Add(((Zombie)players[i]).Id);
                }
            }
            if (zombies_chasing_list.Count > 0)
            {
                zombies_chasing_arr = new int[zombies_chasing_list.Count];
                zombies_chasing_list.CopyTo(zombies_chasing_arr);
            }
            return zombies_chasing_arr;
        }



        public double get_lat(int player_id)
        {
            Player p = (Player) players[player_id];
            if (p is Zombie) return ((Zombie)p).Lat;
            else return ((Human)p).Lat;
        }



        public double get_lng(int player_id)
        {
            Player p = (Player) players[player_id];
            if (p is Zombie) return ((Zombie)p).Lng;
            else return ((Human)p).Lng;
        }



        public double get_lat_mph(int player_id)
        {
            Player p = (Player) players[player_id];
            if (p is Zombie) return ((Zombie)p).Lat_Mph;
            else return ((Human)p).Lat_Mph;
        }



        public double get_lng_mph(int player_id)
        {
            Player p = (Player) players[player_id];
            if (p is Zombie) return ((Zombie)p).Lng_Mph;
            else return ((Human)p).Lng_Mph;
        }



        public Tuple<double,double> get_cur_lat_lng(DateTime curr_play_time, int player_id)
        {
            Player player2 = (Player) players[player_id];
            if (player2 is Human)
            {
                Human h = (Human)player2;
                double player_lat = h.Lat + Util.mph_to_mps(h.Lat_Mph) * Time.get_sim_secs((curr_play_time - h.Prev_Play_Time).TotalSeconds) / Util.MILES_PER_LAT;
                double player_lng = h.Lng + Util.mph_to_mps(h.Lng_Mph) * Time.get_sim_secs((curr_play_time - h.Prev_Play_Time).TotalSeconds) / Util.MILES_PER_LNG;
                return new Tuple<double, double>(player_lat, player_lng);
            }
            else
            {
                Zombie z = (Zombie)player2;
                double player_lat = z.Lat + Util.mph_to_mps(z.Lat_Mph) * (curr_play_time - z.Prev_Play_Time).TotalSeconds / Util.MILES_PER_LAT;
                double player_lng = z.Lng + Util.mph_to_mps(z.Lng_Mph) * (curr_play_time - z.Prev_Play_Time).TotalSeconds / Util.MILES_PER_LNG;
                return new Tuple<double, double>(player_lat, player_lng);
            }
        }



        // Returns true if successful in stunning; else false
        public void stun(int player_id)
        {
            Player p = (Player) players[player_id];
            if (p is Zombie)
            {
                Zombie z = (Zombie)p;
                if (z.State == Zombie.states.ZOMBIE)
                {
                    z.stun_receive();
                }
            }
        }


        
        // Returns true if successful in tagging; else false
        public void tag(Zombie tagging_zombie, int player_id)
        {
            if (tagging_zombie.State == Zombie.states.ZOMBIE)
            {
                Player p = (Player)players[player_id];
                if (p is Human)
                {
                    Human h = (Human)p;
                    if (h.State == Human.states.HUMAN)
                    {
                        players[player_id] = ((Human)p).to_zombie();
                    }
                }
            }
        }



        public int get_closest_human(double lat, double lng)
        {
            double min_dist = Double.MaxValue;
            int min_player = -1;
            for (int i = 0; i < NUM_PLAYERS; i++)
            {
                if (players[i] is Human)
                {
                    Human h = (Human)players[i];
                    double lat_dist = (h.Lat - lat) * Util.MILES_PER_LAT * 5280;
                    double lng_dist = (h.Lng - lng) * Util.MILES_PER_LNG * 5280;
                    double tot_dist = Math.Sqrt(Math.Pow(lat_dist, 2.0) + Math.Pow(lng_dist, 2.0));
                    if (tot_dist < min_dist)
                    {
                        min_dist = tot_dist;
                        min_player = h.Id;
                    }
                }
            }
            return min_player;
        }



        // Write XML Doc

        public String convert_state(int state, Object o)
        {
            String s = "";
            if (o is Human)
            {
                switch (state)
                {
                    case 0:
                        s = "h";
                        break;
                    case 1:
                        s = "r";
                        break;
                    default:
                        Console.WriteLine("Error- Invalid State: " + s);
                        break;
                }
            }
            else if (o is Zombie)
            {
                switch (state)
                {
                    case 0:
                        s = "z";
                        break;
                    case 1:
                        s = "s";
                        break;
                    case 2:
                        s = "r";
                        break;
                    default:
                        Console.WriteLine("Error- Invalid State: " + s);
                        break;
                }
            }
            else if (o is Safe_Zone || o is Obstacle)
            {
                switch (state)
                {
                    case 0:
                        s = "a";
                        break;
                    case 1:
                        s = "r";
                        break;
                    default:
                        Console.WriteLine("Error- Invalid State: " + s);
                        break;
                }
            }

            else
            {
                Console.WriteLine("Error- Invalid object");
            }

            return s;
        }




        public void create_and_send_XML()
        {
            XElement playersElem = new XElement("players");
            for (int i=0; i<players.Capacity; i++)
            {
                Player player = (Player)players[i];
                if (player != null)
                {
                    if (player is Human)
                    {
                        Human h = (Human)player;
                        playersElem.Add(new XElement("player",
                            new XAttribute("id", h.Id.ToString()),
                            new XAttribute("name", h.Name),
                            new XAttribute("state", convert_state((int)h.State, player)),
                            new XAttribute("lat", h.Lat.ToString()),
                            new XAttribute("lng", h.Lng.ToString())));

                    }
                    else if (player is Zombie)
                    {
                        Zombie z = (Zombie)player;
                        playersElem.Add(new XElement("player",
                                new XAttribute("id", z.Id.ToString()),
                                new XAttribute("name", z.Name),
                                new XAttribute("state", convert_state((int)z.State, player)),
                                new XAttribute("lat", z.Lat.ToString()),
                                new XAttribute("lng", z.Lng.ToString())));
                    }
                }
            }

            XElement zonesElem = new XElement("safeZones");
            foreach (Safe_Zone safe_zone in safe_zones_in_play)
            {
                if (safe_zone != null)
                {
                    XElement safeZoneElem = new XElement("safeZone",
                        new XAttribute("id", safe_zone.Id),
                        new XAttribute("name", safe_zone.Name),
                        new XAttribute("state", convert_state((int)safe_zone.State, safe_zone)));

                    for (int i = 0; i < safe_zone.Points.Count; i++)
                    {
                        double lat = ((Tuple<double, double>)safe_zone.Points[i]).Item1;
                        double lng = ((Tuple<double, double>)safe_zone.Points[i]).Item2;
                        safeZoneElem.Add(new XElement("point",
                            new XAttribute("lat", lat.ToString()),
                            new XAttribute("lng", lng.ToString())));
                    }
                    zonesElem.Add(safeZoneElem);
                }
            }

            XElement obstaclesElem = new XElement("obstacles");
            foreach (Obstacle obstacle in obstacles_in_play)
            {
                if (obstacle != null)
                {
                    XElement obstacleElem = new XElement("obstacle",
                        new XAttribute("id", obstacle.Id),
                        new XAttribute("name", obstacle.Name),
                        new XAttribute("state", convert_state((int)obstacle.State, obstacle)));
                    for (int i = 0; i < obstacle.Points.Count; i++)
                    {
                        double lat = ((Tuple<double, double>)obstacle.Points[i]).Item1;
                        double lng = ((Tuple<double, double>)obstacle.Points[i]).Item2;
                        obstacleElem.Add(new XElement("point",
                            new XAttribute("lat", lat.ToString()),
                            new XAttribute("lng", lng.ToString())));
                    }
                    obstaclesElem.Add(obstacleElem);
                }
            }

            XDocument XMLdoc = new XDocument(new XElement("mapItems",
                    playersElem, zonesElem, obstaclesElem));
            try
            {
                XMLdoc.Save("map.xml");
            }
            catch (Exception e)
            {
               Console.WriteLine("Error: " +  e.StackTrace); 
            }
        }

 /*       private void connect_to_server()
        {
            int num_rounds = 0;
            int connection_index = 0;
            while (!send_tcpclnt.Connected && num_rounds < MAX_SERVER_CONNECTION_TRIES)
            {
                int i = 0;
                while (!send_tcpclnt.Connected && i < server_addresses.Length)
                {
                    int server_try_index = (cur_server_index + i) % server_addresses.Length;
                    IPAddress ipaddr = IPAddress.Parse(server_addresses[server_try_index]);
                    try
                    {
                        send_tcpclnt.Connect(ipaddr, CLIENT_SEND_PORT_NUM);
                        stream_writer = new StreamWriter(send_tcpclnt.GetStream(), Encoding.ASCII);
                        stream_writer.AutoFlush = true;
                        connection_index = i;
                    }
                    catch (Exception e)
                    {
                        send_tcpclnt.Close();
                        send_tcpclnt = new TcpClient();
                    }
                    i++;
                }
            }
            cur_server_index = connection_index;
            if (!send_tcpclnt.Connected) Console.WriteLine("Sim failed to connect to any servers\n");
        }

        private void send_str(String str)
        {
            try
            {
                if (!send_tcpclnt.Connected) connect_to_server();
                stream_writer.WriteLine(str);
            }
            catch (Exception e)
            {
                Console.WriteLine("Sim failed to send message\n");
            }
        }*/
    }
}