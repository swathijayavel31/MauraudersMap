using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Xml; 
using System.Xml.Linq; 
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Simulation
{

    class Player
    {
        #region fields
        // PUBLIC CONSTANTS
        public string[] server_addresses = null; //= new string[3] { Simulation.IP_ADDR_BRIAN, Simulation.IP_ADDR_BRIAN, Simulation.IP_ADDR_SWATHI };
        public const int MAX_SERVER_CONNECTION_TRIES = 100; // In milliseconds
        public const int MAX_SERVER_CONNECTION_TIME = 5000; // In milliseconds
        public const int SEND_TIMEOUT = 100;   // In milliseconds
        public const double MAX_MPH = 6.0;    // in sim MPH
        public const double MAX_MPH_DELTA = 1.0;  // in sim MPH
        public const double MAX_SECONDS_BETWEEN_SENDS = 0.2; // in real seconds
        public const double MIN_SECONDS_BETWEEN_SENDS = 0.2;  // in real seconds
        public const double MIN_SECONDS_BETWEEN_PLAYS = 0.1;    // in real seconds
        public const double MAX_TIME_OUTSIDE_GAME_BOUNDS = 0.0;
        public const double MAX_FEET_BETWEEN_UPDATES = 5.0; // in feet
        public const double MAX_FEET_TO_STUN = 15.0;    // in feet
        public const double MAX_FEET_TO_TAG = 15.0; // in feet
        public const double MIN_LAT = 42.439385;
        public const double MAX_LAT = 42.451533;
        public const double MIN_LNG = -76.490995;
        public const double MAX_LNG = -76.474238;
		public const int CLIENT_PORT_NUM = 8001;
        public const double KEEP_EVENT_SECONDS = 60.0; // in seconds
        public Thread player_thread;
        public Thread receiver_thread;
        private bool quit = false;
        public Thread get_play() { return player_thread; }
        public Thread get_recv() { return receiver_thread; }

        public bool should_quit()
        {
            return quit;
        }

        public void terminate()
        {
            quit = true;
        }

        #endregion fields
    }

    class Human : Player
    {
        #region human_declaration
        // PUBLIC STATIC CONSTANTS
        public enum states { HUMAN = 0, REMOVED }
        public enum modes { NORMAL = 0, ESCAPE, NONE };

        public const int CLIENT_RECEIVE_PORT_NUM = 8004;
        public const int CLIENT_SEND_PORT_NUM = 8001;
    
        // Normal, chase, and escape max player mode times in sim minutes
        public double[] MAX_MODE_DURATION_MINUTES = new double[2] { 60.0, 10.0 };   // in sim minutes
        // Normase, chase, and exscape max player mode speeds in sim MPH
        public double[] MAX_MODE_MPH = new double[2] { MAX_MPH / 2.0, MAX_MPH };     // in sim MPH

        private static Mutex file_m;

        // PLAYER PRIVATE FIELDS
        private Simulation simulation;
        private Random rand_gen;
        private string name;
        private int id;
        private states state;
        private modes mode;
        private DateTime mode_end_time;
        private double correct_decision_prob;
        private double accuracy;
        private double max_mph;
        private double unreponsive_prob;
        private bool unresponsive;
        private bool updating;
        private double lat_mph;
        private double lng_mph;
        private double lat;
        private double lng;
        private double prev_sent_lat;
        private double prev_sent_lng;
        private DateTime prev_sent_time;
        private DateTime curr_play_time;
        private DateTime prev_play_time;
        private int[] zombies_chasing;

		private TcpClient send_tcpclnt;
        private StreamWriter front_stream_writer;

        private TcpClient receive_tcpclnt;
        private StreamWriter back_stream_writer;
        private StreamReader back_stream_reader;

        private int cur_server_index = 0;
        private Queue<Tuple<int, DateTime>> stun_log = new Queue<Tuple<int,DateTime>>();
        private Queue<Tuple<int, DateTime>> tag_log = new Queue<Tuple<int,DateTime>>();

        private int outstanding;
        private int completed_cycles;


        // PLAYER PUBLIC PROPERTIES
        public string Name { get { return name; } set { name = value; } }
        public int Id { get { return id; } }
        public states State { get { return state; } set { state = value; } }
        public modes Mode { get { return mode; } }
        public string Mode_End_Time { get { return mode_end_time.ToLongDateString(); } }
        public double Correct_Decision_Prob { get { return correct_decision_prob; } }
        public double Accuracy { get { return accuracy; } }
        public double Max_Mph { get { return max_mph; } }
        public double Unreponsive_Prob { get { return unreponsive_prob; } }
        public bool Unresponsive { get { return unresponsive; } }
        public bool Updating { get { return updating; } }
        public double Lat_Mph { get { return lat_mph; } }
        public double Lng_Mph { get { return lng_mph; } }
        public double Lat { get { return lat; } }
        public double Lng { get { return lng; } }
        public double Prev_Sent_Lat { get { return prev_sent_lat; } }
        public double Prev_Sent_Lng { get { return prev_sent_lng; } }
        public DateTime Prev_Sent_Time { get { return prev_sent_time; } }
        public DateTime Prev_Play_Time { get { return prev_play_time; } }
        public TcpClient Send_Tcpclnt { get { return send_tcpclnt; } }
        public TcpClient Receive_Tcpclnt { get { return receive_tcpclnt;  } }
        public int Cur_Server_Index { get { return cur_server_index; } }
        public StreamWriter Front_Stream_Writer { get { return front_stream_writer; } }
        public StreamWriter Back_Stream_Writer { get { return back_stream_writer; } }
        public StreamReader Back_Stream_Reader { get { return back_stream_reader; } }

        #endregion human_declaration

        public Human(Simulation s, int id, bool is_south)
        {
            file_m = new Mutex();

            simulation = s;
            player_thread = new Thread(new ThreadStart(play));
            receiver_thread = new Thread(new ThreadStart(receive_from_server));

            this.rand_gen = new Random(((DateTime.Now.Millisecond + 123332539) * id) );
            name = "";
            this.id = id;
            state = states.HUMAN;
            mode = modes.NONE;
            correct_decision_prob = rand(1.0);
            accuracy = rand(1.0);
            max_mph = rand(MAX_MPH);
            unreponsive_prob = rand(1.0);
            unresponsive = false;
            updating = false;
            lat_mph = rand(2.0 * max_mph) - max_mph;
            lng_mph = rand(2.0 * max_mph) - max_mph;

            if (is_south)
            {
                lat = rand((MAX_LAT - MIN_LAT)/3) + MIN_LAT;
                lng = rand(MAX_LNG - MIN_LNG) + MIN_LNG;
            }
            else
            {
                lat = rand((MAX_LAT - MIN_LAT)/3) + MIN_LAT + (MAX_LAT - MIN_LAT)/1.75;
                lng = rand(MAX_LNG - MIN_LNG) + MIN_LNG;
            }
            

            

			send_tcpclnt = new TcpClient();
            send_tcpclnt.NoDelay = true;
            send_tcpclnt.SendTimeout = SEND_TIMEOUT;

            receive_tcpclnt = new TcpClient();
            receive_tcpclnt.NoDelay = true;
            receive_tcpclnt.SendTimeout = SEND_TIMEOUT;
            receive_tcpclnt.ReceiveTimeout = SEND_TIMEOUT; 

            prev_sent_time = DateTime.Now;
            prev_play_time = DateTime.Now;

            outstanding = 0;
            completed_cycles = 0;
            
        }



        public void add_and_update_stun_log(int player_id, DateTime stun_time)
        {
            while (stun_log.Count > 0 && (stun_time - stun_log.Peek().Item2).TotalSeconds > KEEP_EVENT_SECONDS)
            {
                tag_log.Dequeue();
            }
            tag_log.Enqueue(new Tuple<int, DateTime>(player_id, stun_time));
        }



        public bool stun_in_time_range(DateTime min_time, DateTime max_time)
        {
            foreach (Tuple<int, DateTime> t in stun_log)
            {
                if (min_time.CompareTo(t.Item2) < 0 && min_time.CompareTo(t.Item2) > 0)
                {
                    return true;
                }
            }
            return false;
        }



        public bool tag_in_time_range(DateTime min_time, DateTime max_time)
        {
            foreach (Tuple<int, DateTime> t in tag_log)
            {
                if (min_time.CompareTo(t.Item2) < 0 && min_time.CompareTo(t.Item2) > 0)
                {
                    return true;
                }
            }
            return false;
        }



        public double rand(double max_val)
        {
            return rand_gen.NextDouble() * max_val;
        }

		public string get_state_string()
		{
			return "p," + id.ToString() + "," + name + ",h," + lat.ToString() + "," + lng.ToString();
		}


        private void connect_to_server()
        {
            int num_rounds = 0;
            int connection_index = 0;
            while (!send_tcpclnt.Connected && num_rounds < MAX_SERVER_CONNECTION_TRIES)
            {
                int i = 0;
                while (!send_tcpclnt.Connected && i < server_addresses.Length)
                {
                    send_tcpclnt.Close();
                    send_tcpclnt = new TcpClient();
                    int server_try_index = (cur_server_index + i) % server_addresses.Length;
                    IPAddress ipaddr = IPAddress.Parse(server_addresses[server_try_index]);
                    System.Threading.WaitHandle wh = null; 
                    try
                    {
                        IAsyncResult ar = send_tcpclnt.BeginConnect(server_addresses[server_try_index], CLIENT_SEND_PORT_NUM, null, null);
                        wh = ar.AsyncWaitHandle;
                        bool connected = ar.AsyncWaitHandle.WaitOne(MAX_SERVER_CONNECTION_TIME);
                        if (!connected)
                        {
                            send_tcpclnt.Close();
                            send_tcpclnt = new TcpClient();
                        }
                        else
                        {
                            if (send_tcpclnt != null || ar != null)
                            {
                                send_tcpclnt.EndConnect(ar);
                                Console.WriteLine(id + " Human connected to send server: " + server_addresses[server_try_index]);
                                front_stream_writer = new StreamWriter(send_tcpclnt.GetStream(), Encoding.ASCII);
                                front_stream_writer.AutoFlush = true;
                                connection_index = i;
                            }
                        }
                    }
                    catch (NullReferenceException e)
                    {

                    }
                    catch (SocketException e)
                    {
    
                    }
                    if (wh != null) wh.Dispose(); 
                    i++;
                }
            }
            cur_server_index = connection_index;
            if (!send_tcpclnt.Connected) Console.WriteLine(id + " Client " + id.ToString() + " failed to connect to any servers\n");
        }



        private void send_str(String str)
        {
            try
            {
                if (!send_tcpclnt.Connected) connect_to_server();
                front_stream_writer.WriteLine(str);
                outstanding = outstanding + 1;
            }
            catch (SocketException e)
            {

            }
            catch (ObjectDisposedException e)
            {

            }
            catch (IOException e)
            {

            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.ToString());
                Console.WriteLine("Client " + id.ToString() + "failed to send message\n");
            }
        }



        public void send_state()
        {
            prev_sent_lat = lat;
            prev_sent_lng = lng;
            DateTime send_time = DateTime.Now;
            prev_sent_time = Util.clone(send_time);
            send_str(get_state_string());
        }


        public void receive_from_server()
        {
            DateTime last_receive_time = DateTime.Now;
            int corrupt_count = 0;
            while (!should_quit())
            {
                int num_rounds = 0;
                int connection_index = 0;
                try
                {
                    while (!receive_tcpclnt.Connected && num_rounds < MAX_SERVER_CONNECTION_TRIES)
                    {
                        int i = 0;
                        while (!receive_tcpclnt.Connected && i < server_addresses.Length)
                        {
                            receive_tcpclnt.Close();
                            receive_tcpclnt = new TcpClient();
                            int server_try_index = (cur_server_index + i) % server_addresses.Length;
                            IPAddress ipaddr = IPAddress.Parse(server_addresses[server_try_index]);
                            System.Threading.WaitHandle wh = null;
                            try
                            {
                                IAsyncResult ar = receive_tcpclnt.BeginConnect(server_addresses[server_try_index], CLIENT_RECEIVE_PORT_NUM, null, null);
                                wh = ar.AsyncWaitHandle;
                                bool connected = ar.AsyncWaitHandle.WaitOne(MAX_SERVER_CONNECTION_TIME);
                                if (!connected)
                                {
                                    receive_tcpclnt.Close();
                                    receive_tcpclnt = new TcpClient();
                                }
                                else
                                {
                                    // TODO:  WHY IS receive_tcpclnt NULL SOMETIMES?!?!?!?
                                    if (receive_tcpclnt != null || ar != null)
                                    {
                                        receive_tcpclnt.EndConnect(ar);
                                        Console.WriteLine(id + " Zombie connected to receive server: " + server_addresses[server_try_index]);
                                        back_stream_writer = new StreamWriter(receive_tcpclnt.GetStream(), Encoding.ASCII);
                                        back_stream_writer.AutoFlush = true;
                                        back_stream_reader = new StreamReader(receive_tcpclnt.GetStream(), Encoding.UTF8);
                                        back_stream_writer.WriteLine(id.ToString());
                                        connection_index = i;
                                    }
                                }
                            }
                            catch (NullReferenceException e)
                            {

                            }
                            catch (IOException e)
                            {

                            }
                            catch (SocketException e)
                            {

                            }
                            if (wh != null) wh.Dispose();
                            i++;
                        }
                    }
                    cur_server_index = connection_index;
                    if (!receive_tcpclnt.Connected)
                    {
                        Console.WriteLine("Client " + id.ToString() + " failed to connect to any servers\n");
                        return;
                    }

                    String update_msg = "";
                    try
                    {
                        // String update_msg = back_stream_reader.ReadLine();
                        //update_msg = "";
                        //double millisec = (DateTime.Now - last_receive_time).TotalMilliseconds;
                        //while (update_msg == "" || (millisec < 100 && !back_stream_reader.EndOfStream))
                        //{
                        //    update_msg = back_stream_reader.ReadLine();
                        //    millisec = (DateTime.Now - last_receive_time).TotalMilliseconds;
                        //}
                        //last_receive_time = DateTime.Now;
                        //back_stream_reader.DiscardBufferedData();

                        update_msg = back_stream_reader.ReadLine();
                        completed_cycles = completed_cycles + 1;
                        simulation.update_stats(id, outstanding, completed_cycles);
                        outstanding = 0;
                        //XDocument xDoc = XDocument.Parse(update_msg);
                        if (id == 1)
                        {
                            XDocument xDoc = XDocument.Parse(update_msg);

                            System.IO.StreamWriter sw;
                            using (sw = new System.IO.StreamWriter("map1.xml", false, Encoding.Unicode))
                            {
                                xDoc.Save(sw, SaveOptions.None);
                            }
                        }
                        
                        if(id == 2)
                        {
                            XDocument xDoc = XDocument.Parse(update_msg);

                            System.IO.StreamWriter sw;
                            using (sw = new System.IO.StreamWriter("map2.xml", false, Encoding.Unicode))
                            {
                                xDoc.Save(sw, SaveOptions.None);
                            }
                        }
                    }
                    catch (ObjectDisposedException e)
                    {

                    }
                    catch (XmlException e)
                    {
                        // Console.WriteLine("Error"); 
                        // Console.WriteLine("update_msg: " + update_msg);
                        corrupt_count++;
                        Console.WriteLine("Corrupted files: " + corrupt_count);
                        receive_tcpclnt.Close();
                        receive_tcpclnt = new TcpClient();

                    }
                    catch (ArgumentException e)
                    {
                        back_stream_reader.DiscardBufferedData();

                    }
                    catch (IOException e)
                    {

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e.StackTrace);
                        Console.WriteLine(e.ToString());
                    }
                }
                catch (NullReferenceException e)
                {

                }
            }
            if (receive_tcpclnt != null && should_quit())
                receive_tcpclnt.Close();
        }
     

        public void start()
        {
            player_thread.Start();
        }

        public double get_lat_change(DateTime cur_time)
        {
            double secs = Time.get_sim_secs((cur_time - prev_play_time).TotalSeconds);
            double lat_change = Util.mph_to_mps(lat_mph) / Util.MILES_PER_LAT * secs;
            return lat_change;
        }

        public double get_lng_change(DateTime cur_time)
        {
            double secs = Time.get_sim_secs((cur_time - prev_play_time).TotalSeconds);
            return Util.mph_to_mps(lng_mph) / Util.MILES_PER_LNG * secs;
        }



        public Zombie to_zombie()
        {
            state = states.REMOVED;
            Zombie z = new Zombie(simulation, this, prev_sent_time, prev_play_time);
            z.start();
            return z;
        }



        private void set_escape_speeds()
        {
            double sum_lat = 0.0;
            double sum_lng = 0.0;
            for (int i = 0; i < zombies_chasing.Length; i++)
            {
                sum_lat += simulation.get_lat_mph(zombies_chasing[i]);
                sum_lat += simulation.get_lng_mph(zombies_chasing[i]);
            }
            double d_lat_mph = rand(2 * MAX_MPH_DELTA);
            double d_lng_mph = rand(2 * MAX_MPH_DELTA);
            double lat_max_mph = max_mph;
            double lng_max_mph = max_mph;
            if (sum_lat < 0)
            {
                d_lat_mph = -d_lat_mph;
                lat_max_mph = -lat_max_mph;
            }
            if (sum_lng < 0)
            {
                d_lng_mph = -d_lng_mph;
                lng_max_mph = -lng_max_mph;
            }

            double new_lat_mph = lat_mph + d_lat_mph;
            double new_lng_mph = lng_mph + d_lng_mph;

            lat_mph = ((Math.Abs(new_lat_mph) < Math.Abs(lat_max_mph)) ? new_lat_mph : lat_max_mph);
            lng_mph = ((Math.Abs(new_lng_mph) < Math.Abs(lng_max_mph)) ? new_lng_mph : lng_max_mph);
        }



        private void stun(int zombie_chasing_id)
        {
            Console.WriteLine("STUNNED!!!!!!!!!!!!!!!");
            simulation.Tag_Stun_Lock.WaitOne();
            simulation.stun(zombie_chasing_id);
            DateTime stun_time = DateTime.Now;
            //add_and_update_stun_log(zombie_chasing_id, stun_time);
            simulation.Tag_Stun_Lock.ReleaseMutex();
            String str = "s," + id.ToString() + "," + zombie_chasing_id.ToString() + "," + stun_time.ToString();
            send_str(str);
        }



        private void set_speeds()
        {
            // Determine random new speed
            double new_lat_mph = lat_mph + (rand(2 * MAX_MPH_DELTA) - MAX_MPH_DELTA);
            double new_lng_mph = lng_mph + (rand(2 * MAX_MPH_DELTA) - MAX_MPH_DELTA);

            // Determine new speeds considering max
            if (Math.Abs(new_lat_mph) > MAX_MODE_MPH[(int)mode])
            {
                lat_mph = (new_lat_mph >= 0 ? MAX_MODE_MPH[(int)mode] : -MAX_MODE_MPH[(int)mode]);
                lng_mph = (new_lng_mph >= 0 ? MAX_MODE_MPH[(int)mode] : -MAX_MODE_MPH[(int)mode]);
            }
            else
            {
                lat_mph = new_lat_mph;
                lng_mph = new_lng_mph;
            }
        }

        private void balance_load()
        {
            bool connected = false;
            TcpClient lb = null;
            while (!connected)
            {
                lb = new TcpClient();
                IAsyncResult ar = lb.BeginConnect(simulation.LB_IPADDR, simulation.LB_PORT_NUM, null, null);
                WaitHandle wh = ar.AsyncWaitHandle;
                connected = ar.AsyncWaitHandle.WaitOne(MAX_SERVER_CONNECTION_TIME);
                if (!connected)
                {
                    lb.Close();
                    lb = new TcpClient();
                }
            }
            Console.WriteLine("Player " + id.ToString() + " Connected to Load Balancer!");
            StreamWriter lb_write_stream = new StreamWriter(lb.GetStream(), Encoding.ASCII);
            lb_write_stream.AutoFlush = true;
            StreamReader lb_read_stream = new StreamReader(lb.GetStream(), Encoding.ASCII);
            string msg = "p,";
            msg += id.ToString() + ",";
            msg += lat.ToString() + ",";
            msg += lng.ToString() + ",p";
            lb_write_stream.WriteLine(msg);
            Console.WriteLine("Sent message to LB. Waiting on configuration!");
            string ret = lb_read_stream.ReadLine().Trim();
            Console.WriteLine("Read Server Configurations from LB!");
            server_addresses = ret.Split(new char[] { ',' });
            send_state();
            lb_write_stream.WriteLine("FIN");
            Console.WriteLine("Going to close connection to LB. SWAG");
            lb.Close();
            send_state();
            receiver_thread.Start(); // ---------------------- START RECEIVE THREAD -------- 
        }



        private void play()
        {
            balance_load();
            while (state != states.REMOVED && !should_quit())
            {
                //Console.WriteLine("ABNORMAL 1");
                // Wait minimum amount of time between plays
                Thread.Sleep((int)Math.Ceiling(MIN_SECONDS_BETWEEN_PLAYS * 1000));
                // Get date time for current set of updates
                curr_play_time = DateTime.Now;
                // Determine new position based on previous speeds
                double new_lat = lat + get_lat_change(curr_play_time);
                double new_lng = lng + get_lng_change(curr_play_time);
                // Determine if might have crossed obstacle
                Tuple<double,double> intersect_point = simulation.find_obstacle_intersection(lat, lng, new_lat, new_lng);
                // If might have crossed
                if (intersect_point != null)
                {
                    //Console.WriteLine("ABNORMAL 2");
                    // Decide if it did
                    bool crossed = (rand_gen.NextDouble() >= correct_decision_prob);
                    if (crossed)
                    {
                        // Expel from game
                        //Console.WriteLine("ABNORMAL 3");
                        state = states.REMOVED;
                        send_state();
                    }
                    else
                    {
                        //Console.WriteLine("ABNORMAL 4");
                        // Move halfway between prev position and obstacle
                        lat = (lat + intersect_point.Item2) / 2.0;
                        lng = (lng + intersect_point.Item1) / 2.0;
                        // Reverse direction
                        lat_mph = -lat_mph;
                        lng_mph = -lng_mph;
                    }
                }
                // Determine if crossed game border and place within bounds
                else if (new_lat>MAX_LAT||new_lat<MIN_LAT||new_lng>MAX_LNG||new_lng<MIN_LNG)
                {
                    //Console.WriteLine("ABNORMAL 5");
                    if (new_lat > MAX_LAT)
                    {
                        lat = MAX_LAT;
                        lat_mph = -lat_mph;
                    }
                    else if (new_lat < MIN_LAT)
                    {
                        lat = MIN_LAT;
                        lat_mph = -lat_mph;
                    }
                    if (new_lng > MAX_LNG)
                    {
                        lng = MAX_LNG;
                        lng_mph = -lng_mph;
                    }
                    else if (new_lng < MIN_LNG)
                    {
                        lng = MIN_LNG;
                        lng_mph = -lng_mph;
                    }
                }
                else
                {
                    // Update lat and lng
                    //Console.WriteLine("ABNORMAL 6");
                    lat = new_lat;
                    lng = new_lng;
                }

                if (mode == modes.NORMAL)
                {
                    //Console.WriteLine("ABNORMAL 7");
                    // Normal Mode
                    if (state != states.REMOVED)
                    {
                        // Determine if being chased
                        zombies_chasing = simulation.get_zombies_chasing(id);
                        bool enters_escape = false;
                        if (zombies_chasing != null)
                        {
                            //Console.WriteLine("ABNORMAL 8");
                            // Being chased
                            // Determine if enters escape mode
                            enters_escape = (rand_gen.NextDouble() < correct_decision_prob);
                        }
                        if (enters_escape)
                        {
                            //Console.WriteLine("ABNORMAL 9");
                            // Enter escape mode
                            mode = modes.ESCAPE;
                            mode_end_time = curr_play_time.AddMinutes(rand(Time.get_real_mins(MAX_MODE_DURATION_MINUTES[(int)modes.ESCAPE])));
                            set_escape_speeds();
                        }
                        else if (curr_play_time.CompareTo(mode_end_time) >= 0)
                        {
                            //Console.WriteLine("ABNORMAL 10");
                            // Previous mode expired
                            // Enter normal mode
                            mode = modes.NORMAL;
                            mode_end_time = curr_play_time.AddMinutes(rand(Time.get_real_mins(MAX_MODE_DURATION_MINUTES[(int)modes.NORMAL])));
                            zombies_chasing = null;
                        }
                        else
                        {
                            //Console.WriteLine("ABNORMAL 11");
                            // Determine new speeds
                            set_speeds();
                        }
                    }
                }
                else
                {
                    // Escape Mode
                    // Determine if escape mode expired
                    if (curr_play_time.CompareTo(mode_end_time) >= 0)
                    {
                        //Console.WriteLine("ABNORMAL 12");
                        // Escape mode expired
                        // Return to normal mode
                        //Console.WriteLine("NORMAL MODE EXPIRED");
                        mode = modes.NORMAL;
                        mode_end_time = curr_play_time.AddMinutes(rand(Time.get_real_mins(MAX_MODE_DURATION_MINUTES[(int)modes.NORMAL])));
                    }
                    else
                    {
                        //Console.WriteLine("ABNORMAL 13");
                        // Determine if Zombies close enough to be stunned
                        if (zombies_chasing != null)
                        {
                            //Console.WriteLine("ABNORMAL 14");
                            for (int i = 0; i < zombies_chasing.Length; i++)
                            {
                                //Console.WriteLine("ABNORMAL 15");
                                Tuple<double,double> lat_lng = simulation.get_cur_lat_lng(curr_play_time, zombies_chasing[i]);
                                double dist = Util.distance(lat, lng, lat_lng.Item1, lat_lng.Item2);
                                if (dist <= MAX_FEET_TO_STUN)
                                {
                                    //Console.WriteLine("ABNORMAL 16");
                                    if (simulation.get_safe_zone(lat_lng.Item1, lat_lng.Item2) < 0)
                                    {
                                        //Console.WriteLine("ABNORMAL 17");
                                        // Zombie close enough and not in safe zone
                                        // Determine if it gets stunned
                                        bool gets_stunned = (rand(1.0) < accuracy);
                                        if (gets_stunned)
                                        {
                                            //Console.WriteLine("ABNORMAL 18");
                                            // Stun zombie
                                            stun(zombies_chasing[i]);
                                        }
                                    }
                                }
                            }
                        }
                        set_escape_speeds();
                    }
                }

                // Update server if needed
                if ((curr_play_time - prev_sent_time).TotalSeconds >= MAX_SECONDS_BETWEEN_SENDS) send_state();
                //else
                //{
                //    //Console.WriteLine("ABNORMAL 19");
                //    double lat_feet_change = (lat - prev_sent_lat) * Util.MILES_PER_LAT * 5280;
                //    double lng_feet_change = (lng - prev_sent_lng) * Util.MILES_PER_LNG * 5280;
                //    double tot_feet_change = Math.Sqrt(Math.Pow(lat_feet_change, 2.0) + Math.Pow(lng_feet_change, 2.0));
                //    if (tot_feet_change > MAX_FEET_BETWEEN_UPDATES) send_state();
                //}
                prev_play_time = Util.clone(curr_play_time);
            }
        }
    }

    class Zombie : Player
    {
        #region declarations
        // PUBLIC CONSTANTS
        public enum states { ZOMBIE = 0, STUNNED, REMOVED }
        public enum modes { NORMAL = 0, CHASE, NONE };

        public const int CLIENT_RECEIVE_PORT_NUM = 8004;
        public const int CLIENT_SEND_PORT_NUM = 8001;

        // Normal, chase, and escape max player mode times in sim minutes
        public  double[] MAX_MODE_DURATION_MINUTES = new double[2] { 60.0, 10.0 };   // in sim minutes
        // Normase, chase, and exscape max player mode speeds in sim MPH
        public  double[] MAX_MODE_MPH = new double[2] { MAX_MPH / 2.0, MAX_MPH };     // in sim MPH
        public const double MAX_STARVE_HOURS = 48.0;    // in hours
        public const double STUN_MIN = 15.0;

        // Player private fields
        private Simulation simulation;
        private Random rand_gen; 
        private int id;
		private String name; 
        private states state;
        private modes mode;
        private DateTime mode_end_time;
        private double correct_decision_prob;
        private double accuracy;
        private double max_mph;
        private double unreponsive_prob;
        private bool unresponsive;
        private bool updating;
        private double lat_mph;
        private double lng_mph;
        private double lat;
        private double lng;
        private double prev_sent_lat;
        private double prev_sent_lng;
        private DateTime prev_sent_time;
        private DateTime prev_play_time;
        private DateTime curr_play_time;
        private DateTime prev_time_human_eaten;
        private DateTime prev_stun_time;
        private int human_chasing;
        private bool converted;

        private int outstanding;
        private int completed_cycles;

        private TcpClient send_tcpclnt;
        private StreamWriter front_stream_writer;

        private TcpClient receive_tcpclnt;
        private StreamReader back_stream_reader;
        StreamWriter back_stream_writer;


        private int cur_server_index = 0;
        private Queue<Tuple<int, DateTime>> stun_log = new Queue<Tuple<int, DateTime>>();
        private Queue<Tuple<int, DateTime>> tag_log = new Queue<Tuple<int, DateTime>>();

        private static Mutex file_m;

        private readonly object sync = new object();
        private bool balanced = false;

        // Player public properties
        public int Id { get { return id; } }
        public states State { get { return state; } set { state = value; } } 
        public modes Mode { get { return mode; } }
        public string Name { get { return name; } set { name = value; } }
        public string Mode_End_Time { get { return mode_end_time.ToLongDateString(); } }
        public double Correct_Decision_Prob { get { return correct_decision_prob; } }
        public double Accuracy { get { return accuracy; } }
        public double Max_Mph { get { return max_mph; } }
        public double Unreponsive_Prob { get { return unreponsive_prob; } }
        public bool Unresponsive { get { return unresponsive; } }
        public bool Updating { get { return updating; } }
        public double Lat_Mph { get { return lat_mph; } }
        public double Lng_Mph { get { return lng_mph; } }
        public double Lat { get { return lat; } }
        public double Lng { get { return lng; } }
        public double Prev_Sent_Lat { get { return prev_sent_lat; } }
        public double Prev_Sent_Lng { get { return prev_sent_lng; } }
        public DateTime Prev_Sent_Time { get { return prev_sent_time; } }
        public DateTime Prev_Play_Time { get { return prev_play_time; } }
        public DateTime Prev_Time_Human_Eaten { get { return prev_time_human_eaten; } }
        public int Human_Chasing { get { return human_chasing; } }
        public TcpClient Send_Tcpclnt { get { return send_tcpclnt; }}
        public TcpClient Receive_Tcpclnt {get { return receive_tcpclnt; }}
        public StreamWriter Front_Stream_Writer { get { return front_stream_writer; } }
        public StreamWriter Back_Stream_Writer { get { return back_stream_writer; } }
        public StreamReader Back_Stream_Reader { get { return back_stream_reader; } }

        #endregion declarations

        public Zombie(Simulation s, int id, bool first_zombie, bool is_south)
        {
            if (first_zombie)
            {
                file_m = new Mutex();

                simulation = s;
                this.rand_gen = new Random(((DateTime.Now.Millisecond + 24344) * id));
                player_thread = new Thread(new ThreadStart(play));
                receiver_thread = new Thread(new ThreadStart(receive_from_server)); 
                this.id = id;
                name = "";
                state = states.ZOMBIE;
                mode = modes.CHASE;
                mode_end_time = DateTime.Now.AddMinutes(Time.get_real_mins(MAX_MODE_DURATION_MINUTES[(int)modes.CHASE]));
                correct_decision_prob = 1; // 0.5;
                accuracy = 0.5;
                max_mph = 0.5 * MAX_MPH;
                unreponsive_prob = 0;
                unresponsive = false;
                updating = false;
                lat_mph = 2 * rand_gen.NextDouble() * max_mph - max_mph;
                lng_mph = 2 * rand_gen.NextDouble() * max_mph - max_mph;
                prev_sent_lat = lat;
                prev_sent_lng = lng;
                prev_sent_time = DateTime.Now;
                prev_play_time = DateTime.Now;
                prev_time_human_eaten = DateTime.Now;
                human_chasing = -1;

				send_tcpclnt = new TcpClient();
                send_tcpclnt.NoDelay = true;

                receive_tcpclnt = new TcpClient();
                receive_tcpclnt.NoDelay = true;
            }
            else
            {
                file_m = new Mutex();

                simulation = s;
                this.rand_gen = new Random(((DateTime.Now.Millisecond + 15423579) * id));
                player_thread = new Thread(new ThreadStart(play));
                receiver_thread = new Thread(new ThreadStart(receive_from_server)); 
                this.id = id;
                state = states.ZOMBIE;
                name = "";
                mode = modes.NORMAL;
                mode_end_time = DateTime.Now.AddMinutes(rand(Time.get_real_mins(MAX_MODE_DURATION_MINUTES[(int)modes.NORMAL])));
                correct_decision_prob = rand_gen.NextDouble();
                accuracy = rand_gen.NextDouble();
                max_mph = rand_gen.NextDouble() * MAX_MPH;
                unreponsive_prob = rand_gen.NextDouble();
                unresponsive = false;
                updating = false;
                lat_mph = 2 * rand_gen.NextDouble() * max_mph - max_mph;
                lng_mph = 2 * rand_gen.NextDouble() * max_mph - max_mph;
                prev_sent_lat = lat;
                prev_sent_lng = lng;
                prev_sent_time = DateTime.Now;
                prev_play_time = DateTime.Now;
                prev_time_human_eaten = DateTime.Now;
                human_chasing = -1;

                send_tcpclnt = new TcpClient();
                send_tcpclnt.NoDelay = true;

                receive_tcpclnt = new TcpClient();
                receive_tcpclnt.NoDelay = true;
            }
            if (is_south)
            {
                lat = rand((MAX_LAT - MIN_LAT) / 3) + MIN_LAT;
                lng = rand(MAX_LNG - MIN_LNG) + MIN_LNG;
            }
            else
            {
                lat = rand((MAX_LAT - MIN_LAT) / 3) + MIN_LAT + (MAX_LAT - MIN_LAT) / 1.75;
                lng = rand(MAX_LNG - MIN_LNG) + MIN_LNG;
            }
            outstanding = 0;
            completed_cycles = 0;
            converted = false;
        }

        public Zombie(Simulation s, Human h, DateTime prev_sent_time, DateTime prev_play_time)
        {
            file_m = new Mutex();

            simulation = s;
            this.rand_gen = new Random(((DateTime.Now.Millisecond + 765765) * id));
            player_thread = new Thread(new ThreadStart(play));
            receiver_thread = new Thread(new ThreadStart(receive_from_server)); 
            id = h.Id;
            state = states.ZOMBIE;
            name = "";
            mode = modes.NORMAL;
            mode_end_time = DateTime.Now.AddMinutes(rand(Time.get_real_mins(MAX_MODE_DURATION_MINUTES[(int)modes.NORMAL])));
            correct_decision_prob = h.Correct_Decision_Prob;
            accuracy = h.Accuracy;
            max_mph = h.Max_Mph;
            unreponsive_prob = h.Unreponsive_Prob;
            unresponsive = false;
            updating = false;
            lat_mph = h.Lat_Mph;
            lng_mph = h.Lng_Mph;
            lat = h.Lat;
            lng = h.Lng;
            prev_sent_lat = h.Prev_Sent_Lat;
            prev_sent_lng = h.Prev_Sent_Lng;
            this.prev_sent_time = prev_sent_time;
            this.prev_play_time = prev_play_time;
            prev_time_human_eaten = DateTime.Now;
            human_chasing = -1;
            send_tcpclnt = h.Send_Tcpclnt;
            receive_tcpclnt = h.Receive_Tcpclnt;
            front_stream_writer = h.Front_Stream_Writer;
            back_stream_writer = h.Back_Stream_Writer; 
            back_stream_reader = h.Back_Stream_Reader;
            outstanding = 0;
            completed_cycles = 0;
            converted = true;
        }



        public bool stun_in_time_range(DateTime min_time, DateTime max_time)
        {
            foreach (Tuple<int, DateTime> t in stun_log)
            {
                if (min_time.CompareTo(t.Item2) < 0 && min_time.CompareTo(t.Item2) > 0)
                {
                    return true;
                }
            }
            return false;
        }



        public bool tag_in_time_range(DateTime min_time, DateTime max_time)
        {
            foreach (Tuple<int, DateTime> t in tag_log)
            {
                if (min_time.CompareTo(t.Item2) < 0 && min_time.CompareTo(t.Item2) > 0)
                {
                    return true;
                }
            }
            return false;
        }



        public void add_and_update_tag_log(int player_id, DateTime tag_time)
        {
            while (tag_log.Count > 0 && (tag_time - tag_log.Peek().Item2).TotalSeconds > KEEP_EVENT_SECONDS)
            {
                tag_log.Dequeue();
            }
            tag_log.Enqueue(new Tuple<int, DateTime>(player_id, tag_time));
        }


        private void connect_to_server()
        {
            int num_rounds = 0;
            int connection_index = 0;
            while (!send_tcpclnt.Connected && num_rounds < MAX_SERVER_CONNECTION_TRIES)
            {
                int i = 0;
                while (!send_tcpclnt.Connected && i < server_addresses.Length)
                {
                    send_tcpclnt.Close();
                    send_tcpclnt = new TcpClient();
                    int server_try_index = (cur_server_index + i) % server_addresses.Length;
                    IPAddress ipaddr = IPAddress.Parse(server_addresses[server_try_index]);
                    System.Threading.WaitHandle wh = null;
                    try
                    {
                        IAsyncResult ar = send_tcpclnt.BeginConnect(server_addresses[server_try_index], CLIENT_SEND_PORT_NUM, null, null);
                        wh = ar.AsyncWaitHandle;
                        bool connected = ar.AsyncWaitHandle.WaitOne(MAX_SERVER_CONNECTION_TIME);
                        if (!connected)
                        {
                            send_tcpclnt.Close();
                            send_tcpclnt = new TcpClient();
                        }
                        else
                        {
                            if (send_tcpclnt != null || ar != null)
                            {
                                send_tcpclnt.EndConnect(ar);
                                Console.WriteLine(id + " Zombie connected to send server: " + server_addresses[server_try_index]);
                                front_stream_writer = new StreamWriter(send_tcpclnt.GetStream(), Encoding.ASCII);
                                front_stream_writer.AutoFlush = true;
                                connection_index = i;
                            }
                        }
                    }
                    catch (NullReferenceException e)
                    {

                    }
                    catch (SocketException e)
                    {

                    }
                    if (wh != null) wh.Dispose();
                    i++;
                }
            }
            cur_server_index = connection_index;
            if (!send_tcpclnt.Connected) Console.WriteLine(id + " Client " + id.ToString() + " failed to connect to any servers\n");
        }


        private void send_str(String str)
        {
            bool debug = false;
            try
            {
                if (!send_tcpclnt.Connected)
                {
                    connect_to_server();
                    debug = true;
                }
                front_stream_writer.WriteLine(str);
                outstanding = outstanding + 1;
            }
            catch (SocketException e)
            {

            }
            catch (ObjectDisposedException e)
            {

            }
            catch (IOException e)
            {

            }
            catch (Exception e)
            {
                Console.WriteLine("String: " + str);
                if (front_stream_writer == null)
                    Console.WriteLine("Stream writer is NULL");
                if (debug)
                {
                    Console.WriteLine("RECONNECTED");
                }
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.ToString());
                Console.WriteLine("Client " + id.ToString() + "failed to send message\n");
            }
        }



        public void receive_from_server()
        {
            DateTime last_receive_time = DateTime.Now;
            int corrupt_count = 0;
            lock(sync)
            {
                while (!balanced)
                    Monitor.Wait(sync);
            }
            while (!should_quit())
            {
                int num_rounds = 0;
                int connection_index = 0;
                try
                {
                    while (!receive_tcpclnt.Connected && num_rounds < MAX_SERVER_CONNECTION_TRIES)
                    {
                        int i = 0;
                        while (!receive_tcpclnt.Connected && i < server_addresses.Length)
                        {
                            receive_tcpclnt.Close();
                            receive_tcpclnt = new TcpClient();
                            int server_try_index = (cur_server_index + i) % server_addresses.Length;
                            IPAddress ipaddr = IPAddress.Parse(server_addresses[server_try_index]);
                            System.Threading.WaitHandle wh = null;
                            try
                            {
                                IAsyncResult ar = receive_tcpclnt.BeginConnect(server_addresses[server_try_index], CLIENT_RECEIVE_PORT_NUM, null, null);
                                wh = ar.AsyncWaitHandle;
                                bool connected = ar.AsyncWaitHandle.WaitOne(MAX_SERVER_CONNECTION_TIME);
                                if (!connected)
                                {
                                    receive_tcpclnt.Close();
                                    receive_tcpclnt = new TcpClient();
                                }
                                else
                                {
                                    // TODO:  WHY IS receive_tcpclnt NULL SOMETIMES?!?!?!?
                                    if (receive_tcpclnt != null || ar != null)
                                    {
                                        receive_tcpclnt.EndConnect(ar);
                                        Console.WriteLine(id + " Zombie connected to receive server: " + server_addresses[server_try_index]);
                                        back_stream_writer = new StreamWriter(receive_tcpclnt.GetStream(), Encoding.ASCII);
                                        back_stream_writer.AutoFlush = true;
                                        back_stream_reader = new StreamReader(receive_tcpclnt.GetStream(), Encoding.UTF8);
                                        back_stream_writer.WriteLine(id.ToString());
                                        connection_index = i;
                                    }
                                }
                            }
                            catch (NullReferenceException e)
                            {

                            }
                            catch (IOException e)
                            {

                            }
                            catch (SocketException e)
                            {

                            }
                            if (wh != null) wh.Dispose();
                            i++;
                        }
                    }
                    cur_server_index = connection_index;
                    if (!receive_tcpclnt.Connected)
                    {
                        Console.WriteLine("Client " + id.ToString() + " failed to connect to any servers\n");
                        return;
                    }

                    String update_msg = "";
                    try
                    {
                        // String update_msg = back_stream_reader.ReadLine();
                        //update_msg = "";
                        //double millisec = (DateTime.Now - last_receive_time).TotalMilliseconds;
                        //while (update_msg == "" || (millisec < 100 && !back_stream_reader.EndOfStream))
                        //{
                        //    update_msg = back_stream_reader.ReadLine();
                        //    millisec = (DateTime.Now - last_receive_time).TotalMilliseconds;
                        //}
                        //last_receive_time = DateTime.Now;
                        //back_stream_reader.DiscardBufferedData();

                        update_msg = back_stream_reader.ReadLine();
                        completed_cycles = completed_cycles + 1;
                        simulation.update_stats(id, outstanding, completed_cycles);
                        outstanding = 0;
                        //XDocument xDoc = XDocument.Parse(update_msg);
                        if (id == 1)
                        {
                            XDocument xDoc = XDocument.Parse(update_msg);

                            System.IO.StreamWriter sw;
                            using (sw = new System.IO.StreamWriter("map1.xml", false, Encoding.Unicode))
                            {
                                xDoc.Save(sw, SaveOptions.None);
                            }
                        }
                        if (id == 1+simulation.offset)
                        {
                            XDocument xDoc = XDocument.Parse(update_msg);

                            System.IO.StreamWriter sw;
                            using (sw = new System.IO.StreamWriter("map2.xml", false, Encoding.Unicode))
                            {
                                xDoc.Save(sw, SaveOptions.None);
                            }
                        }
                    }
                    catch (ObjectDisposedException e)
                    {

                    }
                    catch (XmlException e)
                    {
                        // Console.WriteLine("Error"); 
                        // Console.WriteLine("update_msg: " + update_msg);
                        corrupt_count++;
                        Console.WriteLine("Corrupted files: " + corrupt_count);
                        receive_tcpclnt.Close();
                        receive_tcpclnt = new TcpClient();

                    }
                    catch (ArgumentException e)
                    {
                        back_stream_reader.DiscardBufferedData();

                    }
                    catch (IOException e)
                    {

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e.StackTrace);
                        Console.WriteLine(e.ToString());
                    }
                }
                catch (NullReferenceException e)
                {

                }
            }
            if (receive_tcpclnt != null && should_quit())
                receive_tcpclnt.Close();
        }



		public string get_state_string()
		{
			if (state == states.STUNNED)
			{
				return "p," + id.ToString() + "," + name + ",s," + lat.ToString() + "," + lng.ToString();
			}
			else
			{
				return "p," + id.ToString() + "," + name + ",z," + lat.ToString() + "," + lng.ToString();
			}
		}

        public void send_state()
        {
            prev_sent_lat = lat;
            prev_sent_lng = lng;
            DateTime send_time = DateTime.Now;
            prev_sent_time = Util.clone(send_time);
            send_str(get_state_string());
        }

        public void start()
        {
            bool success = false;
            while (!success)
            {
                try
                {
                    
                    player_thread.Start();
                    receiver_thread.Start();
                    
                    success = true;
                }
                catch (ThreadStateException e)
                {
             
                }
            }
        }

        public double rand(double max_val)
        {
            return rand_gen.NextDouble() * max_val;
        }

        public double get_lat_change(DateTime cur_time)
        {
            double secs = Time.get_sim_secs((cur_time - prev_play_time).TotalSeconds);
            double lat_change = Util.mph_to_mps(lat_mph) / Util.MILES_PER_LAT * secs;
            return lat_change;
        }

        public double get_lng_change(DateTime cur_time)
        {
            double secs = Time.get_sim_secs((cur_time - prev_play_time).TotalSeconds);
            return Util.mph_to_mps(lng_mph) / Util.MILES_PER_LNG * secs;
        }



        private void set_chase_speeds()
        {
            // Calculate future position of human chasing after 100 ms (real time)
            double h_lat = simulation.get_lat(human_chasing);
            double h_lng = simulation.get_lng(human_chasing);
            double h_lat_mph = simulation.get_lat_mph(human_chasing);
            double h_lng_mph = simulation.get_lng_mph(human_chasing);
            double future_h_lat = h_lat + Util.mph_to_mps(h_lat_mph) / Util.MILES_PER_LAT * MIN_SECONDS_BETWEEN_PLAYS;
            double future_h_lng = h_lng + Util.mph_to_mps(h_lng_mph) / Util.MILES_PER_LNG * MIN_SECONDS_BETWEEN_PLAYS;

            // Calculate speed needed to get to human over there in next 100 ms (real time)
            double lat_miles_between = (future_h_lat - lat) * Util.MILES_PER_LAT;
            double lng_miles_between = (future_h_lng - lng) * Util.MILES_PER_LNG;
            double lat_mph_needed = lat_miles_between / MIN_SECONDS_BETWEEN_PLAYS * 60 * 60;
            double lng_mph_needed = lng_miles_between / MIN_SECONDS_BETWEEN_PLAYS * 60 * 60;

            // Determine ideal speeds considering max deltas
            double lat_mph_after_change;
            double lng_mph_after_change;
            if (Math.Abs(lat_mph_needed) > Math.Abs(lat_mph))
            {
                if (Math.Abs(lat_mph_needed - lat_mph) <= 2 * MAX_MPH_DELTA) lat_mph_after_change = lat_mph_needed;
                else if (lat_mph_needed>=0) lat_mph_after_change = lat_mph + 2 * MAX_MPH_DELTA;
                else lat_mph_after_change = lat_mph - 2 * MAX_MPH_DELTA;

                if (Math.Abs(lng_mph_needed - lng_mph) <= 2 * MAX_MPH_DELTA) lng_mph_after_change = lng_mph_needed;
                else if (lng_mph_needed >= 0) lng_mph_after_change = lng_mph + 2 * MAX_MPH_DELTA;
                else lng_mph_after_change = lng_mph - 2 * MAX_MPH_DELTA;
            }
            else
            {
                lat_mph_after_change = lat_mph_needed;
                lng_mph_after_change = lng_mph_needed;
            }

            // Determine speeds considering max speeds
            // Do for lat
            if (Math.Abs(lat_mph_after_change) > max_mph)
            {
                if (lat_mph_after_change >= 0) lat_mph = max_mph;
                else lat_mph = -max_mph;
            }
            else lat_mph = lat_mph_after_change;
            // Do for lng
            if (Math.Abs(lng_mph_after_change) > max_mph)
            {
                if (lng_mph_after_change >= 0) lng_mph = max_mph;
                else lng_mph = -max_mph;
            }
            else lng_mph = lng_mph_after_change;
        }



        private void set_next_mode()
        {
            if (rand(1.0) < correct_decision_prob)
            {
                mode = modes.CHASE;
                mode_end_time = curr_play_time.AddMinutes(Time.get_real_mins(MAX_MODE_DURATION_MINUTES[(int)modes.CHASE]));
                // Determine Human to Chase
                if (human_chasing < 0)
                {
                    human_chasing = simulation.get_closest_human(lat, lng);
                }
                // Determine new speeds
                if (human_chasing!=-1) set_chase_speeds();
            }
            else
            {
                mode = modes.NORMAL;
                mode_end_time = curr_play_time.AddMinutes(rand(Time.get_real_mins(MAX_MODE_DURATION_MINUTES[(int)modes.NORMAL])));
                // Reset human chasing
                human_chasing = -1;
                // Determine new speeds
                lat_mph = lat_mph + rand(MAX_MPH_DELTA);
                lng_mph = lng_mph + rand(MAX_MPH_DELTA);
            }
        }



        private void tag(int human_chasing_id)
        {
            simulation.Tag_Stun_Lock.WaitOne();
            simulation.tag(this, human_chasing_id);
            DateTime tag_time = DateTime.Now;
            //add_and_update_tag_log(human_chasing_id, tag_time);
            simulation.Tag_Stun_Lock.ReleaseMutex();
            String str = "t," + id.ToString() + "," + human_chasing_id.ToString() + "," + tag_time.ToString();
            send_str(str);
        }



        private void set_speeds()
        {
            // Determine random new speed
            double new_lat_mph = lat_mph + (rand(2 * MAX_MPH_DELTA) - MAX_MPH_DELTA);
            double new_lng_mph = lng_mph + (rand(2 * MAX_MPH_DELTA) - MAX_MPH_DELTA);

            // Determine new speeds considering max
            if (Math.Abs(new_lat_mph) > MAX_MODE_MPH[(int)mode])
            {
                lat_mph = (new_lat_mph >= 0 ? MAX_MODE_MPH[(int)mode] : -MAX_MODE_MPH[(int)mode]);
                lng_mph = (new_lng_mph >= 0 ? MAX_MODE_MPH[(int)mode] : -MAX_MODE_MPH[(int)mode]);
            }
            else
            {
                lat_mph = new_lat_mph;
                lng_mph = new_lng_mph;
            }
        }



        public void stun_receive()
        {
            prev_stun_time = DateTime.Now;
            state = states.STUNNED;
        }

        private void balance_load()
        {
            bool connected = false;
            TcpClient lb = null;
            while (!connected)
            {
                lb = new TcpClient();
                IAsyncResult ar = lb.BeginConnect(simulation.LB_IPADDR, simulation.LB_PORT_NUM, null, null);
                WaitHandle wh = ar.AsyncWaitHandle;
                connected = ar.AsyncWaitHandle.WaitOne(MAX_SERVER_CONNECTION_TIME);
                if (!connected)
                {
                    lb.Close();
                    lb = new TcpClient();
                }
            }
            Console.WriteLine("Player " + id.ToString() + " Connected to Load Balancer!");
            StreamWriter lb_write_stream = new StreamWriter(lb.GetStream(), Encoding.ASCII);
            lb_write_stream.AutoFlush = true;
            StreamReader lb_read_stream = new StreamReader(lb.GetStream(), Encoding.ASCII);
            string msg = "p,";
            msg += id.ToString() + ",";
            msg += lat.ToString() + ",";
            msg += lng.ToString() + ",p";
            lb_write_stream.WriteLine(msg);
            string ret = lb_read_stream.ReadLine().Trim();
            server_addresses = ret.Split(new char[] { ',' });
            send_state();
            lb_write_stream.WriteLine("FIN");
            lb.Close();
            send_state();
            lock(sync)
            {
                balanced = true;
                Monitor.PulseAll(sync);
            }
        }


        private void play()
        {
            if(!converted)
            {
                balance_load();
            }
            while (state != states.REMOVED && !should_quit())
            {
                // Wait minimum amount of time between plays
                Thread.Sleep((int)Math.Ceiling(MIN_SECONDS_BETWEEN_PLAYS * 1000));
                // Get date time for current set of updates
                curr_play_time = DateTime.Now;
                // Remove stun if time up
                if ((state == states.STUNNED) && (Time.get_sim_mins((curr_play_time-prev_stun_time).TotalMinutes) > STUN_MIN))
                {
                    state = states.ZOMBIE;
                    Console.WriteLine("ZOMBIE NOT STUNNED ANYMORE");
                }
                // Determine if starved
                if (Time.get_sim_hrs((curr_play_time - prev_time_human_eaten).TotalHours) > MAX_STARVE_HOURS)
                {
                    state = states.REMOVED;
                }
                else
                {
                    // Determine new position based on previous speeds
                    double new_lat = lat + get_lat_change(curr_play_time);
                    double new_lng = lng + get_lng_change(curr_play_time);
                    // Determine if might have crossed obstacle
                    Tuple<double, double> intersect_point = simulation.find_obstacle_intersection(lat, lng, new_lat, new_lng);
                    // If might have crossed
                    if (intersect_point != null)
                    {
                        // Decide if it did
                        bool crossed = (rand_gen.NextDouble() >= correct_decision_prob);
                        if (crossed)
                        {
                            // Expel from game
                            state = states.REMOVED;
                            send_state();
                        }
                        else
                        {
                            // Move halfway between prev position and obstacle
                            lat = (lat + intersect_point.Item2) / 2.0;
                            lng = (lng + intersect_point.Item1) / 2.0;
                            // Reverse direction
                            lat_mph = -lat_mph;
                            lng_mph = -lng_mph;
                        }
                    }
                    // Determine if crossed game border and place within bounds
                    else if (new_lat > MAX_LAT || new_lat < MIN_LAT || new_lng > MAX_LNG || new_lng < MIN_LNG)
                    {
                        if (new_lat > MAX_LAT)
                        {
                            lat = MAX_LAT;
                            lat_mph = -lat_mph;
                        }
                        else if (new_lat < MIN_LAT)
                        {
                            lat = MIN_LAT;
                            lat_mph = -lat_mph;
                        }
                        if (new_lng > MAX_LNG)
                        {
                            lng = MAX_LNG;
                            lng_mph = -lng_mph;
                        }
                        else if (new_lng < MIN_LNG)
                        {
                            lng = MIN_LNG;
                            lng_mph = -lng_mph;
                        }
                    }
                    else
                    {
                        // Update lat and lng
                        lat = new_lat;
                        lng = new_lng;
                    }
                }
                
                if (mode == modes.NORMAL)
                {
                    // Normal Mode
                    if (state != states.REMOVED)
                    {
                        if (curr_play_time.CompareTo(mode_end_time) >= 0)
                        {
                            // Previous mode expired
                            // Decide which mode to enter
                            set_next_mode();
                        }
                        else
                        {
                            // Determine new speeds
                            set_speeds();
                        }
                    }
                }
                else
                {
                    if (state != states.REMOVED)
                    {
                        // Chase Mode
                        // Determine if chase mode expired
                        if (human_chasing == -1) human_chasing = simulation.get_closest_human(lat, lng);
                        if (curr_play_time.CompareTo(mode_end_time) >= 0)
                        {
                            // Chase mode expired
                            // Set next mode
                            set_next_mode();
                        }
                        else
                        {
                            // Determine if Human close enough to be tagged
                            if (human_chasing != -1)
                            {
                                Tuple<double,double> lat_lng = simulation.get_cur_lat_lng(curr_play_time, human_chasing);
                                double dist = Util.distance(lat, lng, lat_lng.Item1, lat_lng.Item2);
                                if (dist <= MAX_FEET_TO_TAG)
                                {
                                    if (simulation.get_safe_zone(lat_lng.Item1, lat_lng.Item2) < 0)
                                    {
                                        // Human close enough and not in safe zone
                                        // Determine if it gets tagged
                                        bool gets_tagged = (rand(1.0) < accuracy);
                                        if (gets_tagged)
                                        {
                                            // Tag human
                                            tag(human_chasing);
                                            human_chasing = simulation.get_closest_human(lat, lng);                                        }
                                    }
                                }
                            }
                            if (human_chasing >= 0) set_chase_speeds();
                        }
                    }
                }

                // Update server if needed
                if ((curr_play_time - prev_sent_time).TotalSeconds >= MAX_SECONDS_BETWEEN_SENDS) send_state();
                //else
                //{
                //    double lat_feet_change = (lat - prev_sent_lat) * Util.MILES_PER_LAT * 5280;
                //    double lng_feet_change = (lng - prev_sent_lng) * Util.MILES_PER_LNG * 5280;
                //    double tot_feet_change = Math.Sqrt(Math.Pow(lat_feet_change, 2.0) + Math.Pow(lng_feet_change, 2.0));
                //    if (tot_feet_change > MAX_FEET_BETWEEN_UPDATES) send_state();
                //}
                prev_play_time = Util.clone(curr_play_time);
            }
        }
    }
}