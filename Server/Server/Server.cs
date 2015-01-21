using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Xml.Linq;
using System.Xml; 
using System.IO; 

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;



namespace Server
{

    class Player
    {
        // This is a test comment
        // Public Constants
        public enum states { HUMAN = 0, ZOMBIE, STUNNED, REMOVED };

        // Private Fields
        private int id;
        private string name;
        private states state;
        private double lat;
        private double lng;

        // Public Properties
        public int Id { get { return id; } set { id = value; } }
        public string Name { get { return String.Copy(name); } set { name = String.Copy(value); } }
        public states State { get { return state; } set { state = value; } }
        public double Lat { get { return lat; } set { lat = value; } }
		public double Lng { get { return lng; } set { lng = value; } }

        public Player(int id, string name, states state, double lat, double lng)
        {
            this.id = id;
            this.name = String.Copy(name);
            this.state = state;
            this.lat = lat;
            this.lng = lng;
        }


        public static states char_to_state(char c)
        {
            switch (c)
            {
                case 'h':
                    return states.HUMAN;
                case 'z':
                    return states.ZOMBIE;
                case 's':
                    return states.STUNNED;
                case 'r':
                    return states.REMOVED;
                default:
                    return states.REMOVED;
            }
        }
    }

    class Obstacle
    {
        // Public Constants
        public enum states { ACTIVE = 0, REMOVED };
        
        // Private Fields
        private int id;
        private string name;
        private states state;
        private ArrayList points;

        // Public Properties
        public int Id { get { return id; } set { id = value; } }
        public string Name { get { return String.Copy(name); } set { name = String.Copy(value); } }
        public states State { get { return state; } set { state = value; } }
        public ArrayList Points { get { return points; } set { points = value; } }

        public Obstacle(int id, string name, states state, ArrayList points)
        {
            this.id = id;
            this.name = String.Copy(name);
            this.state = state;
            this.points = points;
        }

        public static states char_to_state(char c)
        {
            switch (c)
            {
                case 'a':
                    return states.ACTIVE;
                case 'r':
                    return states.REMOVED;
                default:
                    return states.REMOVED;
            }
        }
    }


    class Safe_Zone
    {
        // Public Constants
        public enum states { ACTIVE = 0, REMOVED };

        // Private Fields
        private int id;
        private string name;
        private states state;
        private ArrayList points;

        // Public Properties
        public int Id { get { return id; } set { id = value; } }
        public string Name { get { return String.Copy(name); } set { name = String.Copy(value); } }
        public states State { get { return state; } set { state = value; } }
        public ArrayList Points { get { return points; } set { points = value; } }

        public Safe_Zone(int id, string name, states state, ArrayList points)
        {
            this.id = id;
            this.name = String.Copy(name);
            this.state = state;
            this.points = points;
        }



        public static states char_to_state(char c)
        {
            switch (c)
            {
                case 'a':
                    return states.ACTIVE;
                case 'r':
                    return states.REMOVED;
                default:
                    return states.REMOVED;
            }
        }
    }


    class Server
    {
        // IP ADDRESSES
        public const string IP_ADDR_SWATHI = "10.32.6.248";
        public const string IP_ADDR_BRIAN = "10.33.129.27";
        public const string IP_ADDR_LAB = "10.32.6.219";

        // PORT NUMS CONSTANTS
        public const int BACKUP_PORT_NUM = 8002;
        public const int CLIENT_RECEIVE_PORT_NUM = 8001;
        public const int CLIENT_SEND_PORT_NUM = 8004;
        public const int HEARTBEAT_PORT_NUM = 8003;

        // Server and heartbeat states
        public enum heartbeat_type { FRONT_BACK = 0, FRONT, BACK, MID, HEARTBEAT }
        public enum server_state { FRONT_BACK = 0, FRONT, BACK, MID, NOT_RUNNING }
        public server_state current_server_state; 

        // IPAddresses
        private IPAddress this_ipaddr;
        private IPAddress heartbeat_ipaddr;
        private IPAddress backup_ipaddr;

        // Client Info and Listeners
        private ConcurrentQueue<Tuple<int, TcpClient, StreamWriter, StreamReader>> client_receive_queue;
        private ConcurrentQueue<Tuple<int, TcpClient, StreamWriter, StreamReader>> client_send_queue; 
        private int num_send_clients;
        private Mutex num_send_clients_m;
        private TcpListener client_receive_connection_listener;
        private TcpListener client_send_connection_listener;
        
        //Transfer Queues
        private ConcurrentQueue<String> latLongQueue;
        private ConcurrentQueue<String> eventQueue;
        private ConcurrentQueue<String> xmlQueue; 

        // THREAD Arraylists of Thread objects
        ArrayList client_receive_listener_handlers;
        ArrayList client_receive_message_handlers;
        ArrayList lat_lng_handlers;
        ArrayList event_handlers;

        // THREAD Others
        Thread xml_handler;
        Thread xml_parser;
        Thread client_send_listener_handler;
        
        // THREAD Quantities
        public const int NUM_CLIENT_RECEIVE_LISTENERS = 1;
        public const int NUM_CLIENT_MESSAGES_HANDLERS = 1;
        public const int NUM_LAT_LNG_HANDLERS = 1;
        public const int NUM_EVENT_HANDLERS = 1;

        // BACKUP client/listeners
        TcpClient next_backup_client;
        TcpClient prev_backup_client;
        TcpListener backup_listener;

        // HEARTBEAT HANDLER INFO
        private Heartbeat_Handler heartbeat_handler;
        private TcpClient heartbeat_client;

        // STATE saved on server
        private Player[] players = new Player[5];
        private Obstacle[] obstacles = new Obstacle[5];
        private Safe_Zone[] safe_zones = new Safe_Zone[5];



        public Server(String my_ipaddr, String heartbeat_ipaddr)
        {
            current_server_state = server_state.NOT_RUNNING;

            // Store appropriate IP Addresses
            this.this_ipaddr = IPAddress.Parse(my_ipaddr);
            this.heartbeat_ipaddr = IPAddress.Parse(heartbeat_ipaddr); 

            // Initialize keeping client info
            client_receive_queue = new ConcurrentQueue<Tuple<int, TcpClient, StreamWriter, StreamReader>>();
            client_send_queue = new ConcurrentQueue<Tuple<int, TcpClient, StreamWriter, StreamReader>>(); 
            num_send_clients = 0;
            num_send_clients_m = new Mutex(); 

            // Transfer Queues
            latLongQueue = new ConcurrentQueue<String>();
            eventQueue = new ConcurrentQueue<String>();
            xmlQueue = new ConcurrentQueue<String>(); 

            // Threading Arrays
            client_receive_listener_handlers = new ArrayList();
            client_receive_message_handlers = new ArrayList();
            lat_lng_handlers = new ArrayList();
            event_handlers = new ArrayList();

            // Other THREADS
            xml_handler = new Thread(new ThreadStart(handle_XML));
            xml_parser = new Thread(new ThreadStart(update_backup_state));
            Thread client_send_listener_handler = new Thread(new ThreadStart(listen_for_send_client_connections));

            // Heartbeat Handler Thread
            Thread heartbeat_handler_T;

            // Backup client/listeners
            prev_backup_client = null;
            next_backup_client = null;
            backup_listener = new TcpListener(this.this_ipaddr, BACKUP_PORT_NUM);
            client_receive_connection_listener = new TcpListener(this.this_ipaddr, CLIENT_RECEIVE_PORT_NUM);
            client_send_connection_listener = new TcpListener(this.this_ipaddr, CLIENT_SEND_PORT_NUM);

            //Append threads to Arrays
            for (int i=0; i<NUM_CLIENT_RECEIVE_LISTENERS; i++)
            {
                client_receive_listener_handlers.Add(new Thread(new ThreadStart(listen_for_receive_client_connections)));
            }
            for (int i=0; i<NUM_CLIENT_MESSAGES_HANDLERS; i++)
            {
                client_receive_message_handlers.Add(new Thread(new ThreadStart(handleClients)));
            }
            for (int i=0; i<NUM_LAT_LNG_HANDLERS; i++)
            {
                lat_lng_handlers.Add(new Thread(new ThreadStart(process_lat_lng_queue)));
            }
            for (int i=0; i<NUM_EVENT_HANDLERS; i++)
            {
                event_handlers.Add(new Thread(new ThreadStart(process_event_queue)));
            }

            // Initialize Hearbeat Handler
            heartbeat_handler = new Heartbeat_Handler(
                client_receive_listener_handlers,
                client_receive_message_handlers,
                lat_lng_handlers,
                event_handlers,
                xml_handler,
                xml_parser,
                client_send_listener_handler,
                next_backup_client,
                backup_ipaddr,
                current_server_state,
                heartbeat_ipaddr,
                client_send_queue,
                client_send_connection_listener,
                this
                );

            // Initialize and Start heartbeat thread
            heartbeat_handler_T = new Thread(new ThreadStart(heartbeat_handler.receive));
            // Add a safe zone
            const double MIN_LAT = 42.439385;
            const double MAX_LAT = 42.451533;
            const double MIN_LNG = -76.490995;
            const double MAX_LNG = -76.474238;
            ArrayList ps2 = new ArrayList(3);
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
            safe_zones[0] = (new Safe_Zone(0, "Arts Quad", Safe_Zone.states.ACTIVE, ps2));
            heartbeat_handler_T.Start();
        }



        public void listen_for_receive_client_connections()
        {
            bool not_success = true;
            while (not_success)
            {
                try
                {
                    client_receive_connection_listener.Start();
                    not_success = false; 
                }
                catch (SocketException e)
                {

                }

            }
            String id_String = "not filled";
            while (true)
            {
                try
                {
                    Console.WriteLine("Listening for client connection to receive updates, on port: " + CLIENT_RECEIVE_PORT_NUM);
                    TcpClient client = client_receive_connection_listener.AcceptTcpClient();
                    Console.WriteLine("Successfully connected to client from whom we receive updates");
                    client.NoDelay = true;
                    NetworkStream stream = client.GetStream();
                    StreamWriter client_stream_writer = new StreamWriter(stream, Encoding.UTF8);
                    client_stream_writer.AutoFlush = true;
                    StreamReader client_stream_reader = new StreamReader(stream, Encoding.ASCII);
                    String msg = client_stream_reader.ReadLine();

                    String[] ltr = msg.Split(new char[] { ',' });
                    id_String = ltr[1];

                    int id = Convert.ToInt32(id_String);

                    client_receive_queue.Enqueue(new Tuple<int, TcpClient, StreamWriter, StreamReader>(id, client, client_stream_writer, client_stream_reader));

                    add_updates(msg);
                }
                catch (IOException e)
                {

                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.StackTrace);
                    Console.WriteLine(e.ToString());
                    Console.WriteLine("id_String: " + id_String);
                }
            }
        }



        public void listen_for_send_client_connections()
        {
            bool not_success = true;
            while (not_success)
            {
                try
                {
                    client_send_connection_listener.Start();
                    not_success = false;
                }
                catch (SocketException e)
                {

                }
            }
            int id= -1;
            while (true)
            {
                try
                {
                    Console.WriteLine("Listening for client connections to whom we send XML, on port: " + CLIENT_SEND_PORT_NUM);
                    TcpClient client = client_send_connection_listener.AcceptTcpClient();
                    Console.WriteLine("Successfully connected to client to whom we send XML");
                    client.NoDelay = true;
                    NetworkStream stream = client.GetStream();
                    StreamWriter client_stream_writer = new StreamWriter(stream, Encoding.UTF8);
                    client_stream_writer.AutoFlush = true;
                    StreamReader client_stream_reader = new StreamReader(stream, Encoding.ASCII);

                    String msg = client_stream_reader.ReadLine();

                    id = Convert.ToInt32(msg);

                    client_send_queue.Enqueue(new Tuple<int, TcpClient, StreamWriter, StreamReader>(id, client, client_stream_writer, client_stream_reader));

                    num_send_clients_m.WaitOne();
                    num_send_clients++;
                    num_send_clients_m.ReleaseMutex();
                }
                catch (IOException e)
                {

                }
                catch (ThreadAbortException e)
                {
                    //do nothingsssss
                }
                catch (SocketException e)
                {
                    //do nothing
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.StackTrace);
                    Console.WriteLine(e.ToString());
                    Console.WriteLine("id: " + id);
                }
            }
        }



        public void handleClients()
        {
            ASCIIEncoding asciEn = new ASCIIEncoding();

            while (true)
            {
                Tuple<int, TcpClient, StreamWriter, StreamReader> client_tuple;
                if (client_receive_queue.TryDequeue(out client_tuple))
                {
                    TcpClient client = client_tuple.Item2;
                    try
                    {
                        if (client.Connected)
                        {
                            int bytes_present = client.Client.Available;
                            if (bytes_present > 0)
                            {
                                String updates = client_tuple.Item4.ReadLine();
                                add_updates(updates);
                            }
                            client_receive_queue.Enqueue(client_tuple);
                        }
                    }
                    catch (IOException e)
                    {
                        //Do nothing
                    }
                }
            }
        }



        public void add_updates(String update)
        {
            if (update != "")
            {
                if (update[0] == 's' || update[0] == 't') eventQueue.Enqueue(update);
                else if (update[0] == 'p' || update[0] == 'o' || update[0] == 'z')
                {
                    latLongQueue.Enqueue(update);
                }
            }
        }



        public String convert_state(int state, Object o)
        {
            String s = "";
            if (o is Player)
            {
                switch (state)
                {
                    case 0:
                        s = "h";
                        break;
                    case 1:
                        s = "z";
                        break;
                    case 2:
                        s = "s";
                        break;
                    case 3:
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
                }
            }

            else
            {
                Console.WriteLine("Error- Invalid object");
            }

            return s; 
        }



        public String create_XML()
        {
            XElement playersElem = new XElement("players");
            foreach (Player player in players)
            {
                if (player != null)
                {
                    playersElem.Add(new XElement("player",
                            new XAttribute("id", player.Id.ToString()),
                            new XAttribute("name", player.Name),
                            new XAttribute("state", convert_state((int)player.State, player)),
                            new XAttribute("lat", player.Lat.ToString()),
                            new XAttribute("lng", player.Lng.ToString())));
                }
            }

            XElement zonesElem = new XElement("safeZones");
            foreach (Safe_Zone safe_zone in safe_zones)
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
            foreach (Obstacle obstacle in obstacles)
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

            String update = XMLdoc.ToString(SaveOptions.DisableFormatting);

            
            return update; 

        }



        public void send_XML_clients(String update_message)
        {
            try
            {
                // update_message = create_XML();

                Tuple<int, TcpClient, StreamWriter, StreamReader> current_client;

                //Console.WriteLine("SEND 01");
                num_send_clients_m.WaitOne();
                //Console.WriteLine("SEND 02");
                int num_clients_to_send = num_send_clients;
                //Console.WriteLine("SEND 03");
                num_send_clients_m.ReleaseMutex();
                //Console.WriteLine("SEND 04");

                int num_tries = 0; 
                while (num_clients_to_send > 0 && num_tries < 10)
                {
                    //Console.WriteLine("SEND 05");

                    if (client_send_queue.TryDequeue(out current_client))
                    {
                        num_tries = 0; 
                        //Console.WriteLine("SEND 06");
                        current_client.Item2.NoDelay = true;
                        current_client.Item3.WriteLineAsync(update_message);
                        client_send_queue.Enqueue(current_client);
                        num_clients_to_send--;
                    }
                    else num_tries++; 
                    //Console.WriteLine("SEND 07");
                }
                //Console.WriteLine("SEND 08");
            }
            catch (InvalidOperationException e)
            {

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.StackTrace);
                Console.WriteLine(ex.ToString());
            }
            }



        public void handle_XML()
        {
            String xml_file = null;
            StreamReader prev_backup_client_reader = null;
            StreamWriter next_backup_client_writer = null;
            DateTime backup_last_recieve_time = DateTime.Now;

            while (true)
            {
                try
                {
                    // XML GET/CREATION
                    //Console.WriteLine("HERE 01");
                    if (heartbeat_handler.current_server_state == server_state.FRONT || heartbeat_handler.current_server_state == server_state.FRONT_BACK)
                    {
                        //create XML
                        //Console.WriteLine("HERE 02");
                        Thread.Sleep(200);
                        xml_file = create_XML();
                        //Console.WriteLine("Exited create_xml"); 
                    }
                    else if (heartbeat_handler.current_server_state != server_state.NOT_RUNNING || heartbeat_handler.current_server_state != server_state.FRONT_BACK || heartbeat_handler.current_server_state != server_state.FRONT)
                    {
                        //If no incoming backup connection, listen and recieve xml
                        //Console.WriteLine("HERE 03");
                        if (prev_backup_client == null || !prev_backup_client.Connected)
                        {
                            //Console.WriteLine("HERE 04");
                            backup_listener.Start();
                            IAsyncResult ar = backup_listener.BeginAcceptTcpClient(null, null);
                            System.Threading.WaitHandle wh = ar.AsyncWaitHandle;
                            bool connected = ar.AsyncWaitHandle.WaitOne(500);
                            if (connected)
                            {
                                //Console.WriteLine("HERE 05");
                                prev_backup_client = backup_listener.EndAcceptTcpClient(ar);
                                prev_backup_client_reader = new StreamReader(prev_backup_client.GetStream(), Encoding.UTF8);
                                backup_listener.Stop();
                            }
                            else
                            {
                                //Console.WriteLine("HERE 06");
                                backup_listener.Stop();
                            }
                        }
                        //Console.WriteLine("HERE 07");
                        if (prev_backup_client_reader != null)
                        {
                            //Console.WriteLine("HERE 08");
                            string last_message = "";
                            double millisec = (DateTime.Now - backup_last_recieve_time).TotalMilliseconds;
                            while (last_message == "" || (millisec < 100 && !prev_backup_client_reader.EndOfStream))
                            {
                                //Console.WriteLine("HERE 09");
                                last_message = prev_backup_client_reader.ReadLine();
                                millisec = (DateTime.Now - backup_last_recieve_time).TotalMilliseconds;
                            }
                            xml_file = last_message;
                            //Console.WriteLine("HERE 10");
                            backup_last_recieve_time = DateTime.Now;
                            xmlQueue.Enqueue(last_message);
                        }
                        //Console.WriteLine("HERE 11");
                    }

                    // SEND XML TO BACKUP or CLIENTS
                    //Console.WriteLine("HERE 12");
                    if (heartbeat_handler.current_server_state == server_state.FRONT || heartbeat_handler.current_server_state == server_state.MID)
                    {
                        //send xml over backup connection
                        //Console.WriteLine("HERE 13");
                        if (heartbeat_handler.next_backup_client == null || !heartbeat_handler.next_backup_client.Connected)
                        {
                            //Console.WriteLine("HERE 14");
                            heartbeat_handler.next_backup_client = new TcpClient();
                            IAsyncResult ar = heartbeat_handler.next_backup_client.BeginConnect(heartbeat_handler.backup_ipaddr.ToString(), BACKUP_PORT_NUM, null, null);
                            System.Threading.WaitHandle wh = ar.AsyncWaitHandle;
                            bool connected = ar.AsyncWaitHandle.WaitOne(5000);
                            if (connected)
                            {
                                //Console.WriteLine("HERE 15");
                                next_backup_client_writer = new StreamWriter(heartbeat_handler.next_backup_client.GetStream(), Encoding.UTF8);
                                next_backup_client_writer.AutoFlush = true;
                                next_backup_client_writer.WriteLine(xml_file);
                                //      Console.WriteLine(xml_file);
                            }
                            else
                            {
                                //Console.WriteLine("HERE 16");
                                heartbeat_handler.next_backup_client.Close();
                                heartbeat_handler.next_backup_client = new TcpClient();
                            }
                        }
                        else
                        {
                            next_backup_client_writer.WriteLine(xml_file);
                            //                      Console.WriteLine(xml_file); 
                        }
                        //Console.WriteLine("HERE 17");
                    }
                    else if (heartbeat_handler.current_server_state != server_state.NOT_RUNNING)
                    {
                        //send xml_to_client
                        //Console.WriteLine("HERE 18");
                        send_XML_clients(xml_file);
                        //if (xml_file == null) Console.WriteLine("NULL XML!");
                        Console.WriteLine("xml_file sent");
                    }
                    //Console.WriteLine("HERE 19");
                }
                catch (InvalidOperationException e)
                {

                }
                catch (IOException c)
                {
                    // Do nothing
                }
                catch (SocketException s)
                {
                    // Do nothing
                }
            }
        }



        public void update_backup_state()
        {
            while (true)
            {
                String xml_file;
                if (xmlQueue.TryDequeue(out xml_file))
                {
                    // Console.WriteLine("dequeued xml file");
                    XDocument xdoc = XDocument.Parse(xml_file);
                    IEnumerable<XElement> elements = xdoc.Elements();
                    foreach (XElement mapItem_elem in elements)
                    {
                        if (mapItem_elem.Name.LocalName == "mapItems")
                        {
                            IEnumerable<XElement> mapItem_elements = mapItem_elem.Elements();
                            foreach (XElement map_elem in mapItem_elements)
                            {
                                if (map_elem.Name.LocalName == "players")
                                {
                                    IEnumerable<XElement> player_elems = map_elem.Elements();
                                    foreach (XElement player_elem in player_elems)
                                    {
                                        if (player_elem.Name.LocalName == "player")
                                        {
                                            int id = Convert.ToInt32(player_elem.Attribute("id").Value);
                                            String name = player_elem.Attribute("name").Value;
                                            Player.states state = Player.char_to_state((char)(player_elem.Attribute("state").Value[0]));
                                            double lat = Convert.ToDouble(player_elem.Attribute("lat").Value);
                                            double lng = Convert.ToDouble(player_elem.Attribute("lng").Value);

                                            if (id >= players.Length)
                                            {
                                                Player player = new Player(id, name, state, lat, lng);
                                                players = add_to_array(players, id, player);
                                            }
                                            else
                                            {
                                            Player player = players[id];
                                                if (player == null)
                                                {
                                                    player = new Player(id, name, state, lat, lng);
                                                    players = add_to_array(players, id, player);
                                                }
                                                else
                                                {
                                            player.Name = name;
                                            player.State = state;
                                            player.Lat = lat;
                                            player.Lng = lng;
                                        }
                                    }
                                }
                                    }
                                }

                                else if (map_elem.Name.LocalName == "safeZones")
                                {
                                    IEnumerable<XElement> zone_elems = map_elem.Elements();
                                    foreach (XElement zone_elem in zone_elems)
                                    {
                                        if (zone_elem.Name.LocalName == "safeZone")
                                        {
                                            int id = Convert.ToInt32(zone_elem.Attribute("id").Value);
                                            String name = zone_elem.Attribute("name").Value;
                                            Safe_Zone.states state = Safe_Zone.char_to_state((char)(zone_elem.Attribute("state").Value[0]));

                                            ArrayList points_list;

                                            if (id >= safe_zones.Length)
                                            {
                                                points_list = new ArrayList();
                                                Safe_Zone sz = new Safe_Zone(id, name, state, points_list);
                                                safe_zones = add_to_array(safe_zones, id, sz);
                                            }

                                            else
                                            {
                                                Safe_Zone zone = safe_zones[id];
                                                if (zone == null)
                                                {
                                                    points_list = new ArrayList();
                                                    Safe_Zone sz = new Safe_Zone(id, name, state, points_list);
                                                    safe_zones = add_to_array(safe_zones, id, sz);
                                                }
                                                else
                                                {
                                                zone.Name = name;
                                                zone.State = state;
                                                zone.Points.Clear();
                                                points_list = zone.Points;
                                            }
                                            }

                                            IEnumerable<XElement> points = zone_elem.Elements();
                                            foreach (XElement point_elem in points)
                                            {
                                                if (point_elem.Name.LocalName == "point")
                                                {
                                                    double lat = Convert.ToDouble(point_elem.Attribute("lat").Value);
                                                    double lng = Convert.ToDouble(point_elem.Attribute("lng").Value);
                                                    Tuple<double, double> new_point = new Tuple<double, double>(lat, lng);
                                                    points_list.Add(new_point);
                                                }
                                            }
                                        }
                                    }
                                }

                                else if (map_elem.Name.LocalName == "obstacles")
                                {
                                    IEnumerable<XElement> obstacle_elems = map_elem.Elements();
                                        foreach (XElement obstacle_elem in obstacle_elems)
                                        {
                                            int id = Convert.ToInt32(obstacle_elem.Attribute("id").Value);
                                            String name = obstacle_elem.Attribute("name").Value;
                                            Obstacle.states state = Obstacle.char_to_state((char)(obstacle_elem.Attribute("state").Value[0]));

                                            ArrayList points_list;

                                            if (id >= safe_zones.Length)
                                            {
                                                points_list = new ArrayList();
                                                Obstacle obs = new Obstacle(id, name, state, points_list);
                                                obstacles = add_to_array(obstacles, id, obs);
                                            }

                                            else
                                            {
                                                Obstacle obs = obstacles[id];

                                            if (obs == null)
                                            {
                                                points_list = new ArrayList();
                                                obs = new Obstacle(id, name, state, points_list);
                                                obstacles = add_to_array(obstacles, id, obs);
                                            }

                                                obs.Name = name;
                                                obs.State = state;
                                                obs.Points.Clear();
                                                points_list = obs.Points;
                                            }

                                            IEnumerable<XElement> points = obstacle_elem.Elements();
                                            foreach (XElement point_elem in points)
                                            {
                                                if (point_elem.Name.LocalName == "point")
                                                {
                                                    double lat = Convert.ToDouble(point_elem.Attribute("lat").Value);
                                                    double lng = Convert.ToDouble(point_elem.Attribute("lng").Value);
                                                    Tuple<double, double> new_point = new Tuple<double, double>(lat, lng);
                                                    points_list.Add(new_point);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
            }
        }


                  
        public void process_lat_lng_queue()
        {
            while (true)
            {
                String str;
                if (latLongQueue.TryDequeue(out str))
                {
                    string[] str_arr = str.Split(new char[] { ',' });
                    char type = str_arr[0][0];

                   // Console.WriteLine("String: " + str); 
                    int id = Convert.ToInt32(str_arr[1]);
                    string name = str_arr[2];
                    char state = str_arr[3][0];
                    DateTime time = DateTime.Now;

                    switch (type)
                    {
                        case 'p':
                            // Get lat lng
                            double lat = Convert.ToDouble(str_arr[4]);
                            double lng = Convert.ToDouble(str_arr[5]);
                            // Create player if not in player list
                            if (id >= players.Length || players[id] == null)
                            {
                                Player p = new Player(id, name, Player.char_to_state(state), lat, lng);
                                players =  add_to_array(players, id, p);
                            }
                            // Update current safe zone if time is more recent
                            else
                            {
                                Player p = players[id];
                                p.Name = name;
                                p.State = Player.char_to_state(state);
                                p.Lat = lat;
                                p.Lng = lng;
                            }
                            break;

                        case 'z':
                            // Get lat lng list
                            ArrayList points = new ArrayList();
                            for (int i = 4; i < str_arr.Length; i += 2)
                            {
                                points.Add(new Tuple<double, double>(Convert.ToDouble(str_arr[i]), Convert.ToDouble(str_arr[i + 1])));
                            }
                            // Create safe_zone if not in player list
                            if (id >= safe_zones.Length || safe_zones[id] == null)
                            {
                                Safe_Zone s = new Safe_Zone(id, name, Safe_Zone.char_to_state(state), points);
                                safe_zones =  add_to_array(safe_zones, id, s);
                            }
                            // Update current safe zone if time is more recent
                            else
                            {
                                Safe_Zone s = safe_zones[id];
                                s.Name = name;
                                s.State = Safe_Zone.char_to_state(state);
                                s.Points = points;
                            }
                            break;

                        case 'o':
                            // Get lat lng list
                            points = new ArrayList();
                            for (int i = 4; i < str_arr.Length; i += 2)
                            {
                                points.Add(new Tuple<double, double>(Convert.ToDouble(str_arr[i]), Convert.ToDouble(str_arr[i + 1])));
                            }
                            // Create player if not in player list
                            if (id >= obstacles.Length || obstacles[id] == null)
                            {

                                Obstacle o = new Obstacle(id, name, Obstacle.char_to_state(state), points);
                                obstacles =  add_to_array(obstacles, id, o);
                            }
                            // Update current safe zone if time is more recent
                            else
                            {
                                Obstacle o = obstacles[id];
                                o.Name = name;
                                o.State = Obstacle.char_to_state(state);
                                o.Points = points;
                            }
                            break;
                    }
                }
            }
        }



        public void process_event_queue()
        {
            while (true)
            {
                String str;
                if (eventQueue.TryDequeue(out str))
                {
                    string[] str_arr = str.Split(new char[] { ',' });
                    char type = str_arr[0][0];
                    DateTime time = DateTime.Now;
                    int attacker_id = Convert.ToInt32(str_arr[1]);
                    int attacked_id = Convert.ToInt32(str_arr[2]);

                    // If player exists, set its state
                    if ((attacker_id < players.Length && players[attacker_id] != null)
                        && (attacked_id < players.Length && players[attacked_id] != null))
                    {
                        Player attacker = (Player)players[attacker_id];
                        Player attacked = (Player)players[attacked_id];
                        switch (type)
                        {
                            case 's':
                                if (attacked.State == Player.states.ZOMBIE)
                                {
                                    attacked.State = Player.states.STUNNED;
                                }
                                break;

                            case 't':
                                if (attacked.State == Player.states.HUMAN)
                                {
                                    attacked.State = Player.states.ZOMBIE;
                                }
                                break;
                        }
                    }
                }
            }
        }



        private Player[] add_to_array(Player[] arr, int index, Player obj)
        {
            int len = arr.Length;
            if (index < len)
            {
                arr[index] = obj;
                return arr;
            }
            else
            {
                Player[] new_arr = new Player[2 * len];
                while (index >= new_arr.Length)
                {
                    new_arr = new Player[2 * len];
                }
                new_arr[index] = obj;
                arr.CopyTo(new_arr, 0); 

                return new_arr;
            }
        }



        private Safe_Zone[] add_to_array(Safe_Zone[] arr, int index, Safe_Zone obj)
        {
            int len = arr.Length;
            if (index < len)
            {
                arr[index] = obj;
                return arr;
            }
            else
            {
                Safe_Zone[] new_arr = new Safe_Zone[2 * len];
                while (index >= new_arr.Length)
                {
                    new_arr = new Safe_Zone[2 * len];
                }
                new_arr[index] = obj;
                arr.CopyTo(new_arr, 0); 

                return new_arr;
            }
        }



        private Obstacle[] add_to_array(Obstacle[] arr, int index, Obstacle obj)
        {
            int len = arr.Length;
            if (index < len)
            {
                arr[index] = obj;
                return arr;
            }
            else
            {
                Obstacle[] new_arr = new Obstacle[2 * len];
                while (index >= new_arr.Length)
                {
                    new_arr = new Obstacle[2 * len];
                }
                new_arr[index] = obj;
                arr.CopyTo(new_arr, 0); 

                return new_arr;
            }
        }



        static void Main(String[] args)
        {
            Server server = new Server(args[0], args[1]);
        } 
    }
}
