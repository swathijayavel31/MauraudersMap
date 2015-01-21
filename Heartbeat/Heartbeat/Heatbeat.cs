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

namespace Heartbeat
{

    class Heartbeat
    {
        // Constants
        public const int HEARTBEAT_PORT_NUM = 8003;
        public const int LB_PORT_NUM = 8005;
        public const int SEND_TIMEOUT_MILLISEC = 100;
        public const int MILLISEC_BETWEEN_HEARTBEAT_LOOPS = 100;

        public const double MIN_LAT = 42.439385;
        public const double MAX_LAT = 42.451533;
        public const double MIN_LNG = -76.490995;
        public const double MAX_LNG = -76.474238;

        // Variable declaration and initialization
        private int id;
        private string localIPAddrString;
        private IPAddress localIPAddr;
        private string lbIPAddrString;
        private IPAddress lbAddr;

        private double lat;
        private double lng;

        private ArrayList server_array;     // Arraylist of Tuple<TcpClient, StreamWriter>
        private Mutex server_array_mutex;

        private TcpClient lb;
        private StreamWriter lb_stream;

        // Threads
        private Thread receive_servers_thread;
        private Thread send_heartbeats_thread;

        public enum heartbeat_type { FRONT_BACK = 0, FRONT, BACK, MID, HEARTBEAT }


        public Heartbeat(string ipaddr_str,string lbaddr_str, int id,double lat, double lng)
        {
            localIPAddrString = ipaddr_str;
            localIPAddr = IPAddress.Parse(localIPAddrString);
            lbIPAddrString = lbaddr_str;
            lbAddr = IPAddress.Parse(lbIPAddrString);
            server_array = new ArrayList();
            server_array_mutex = new Mutex();
            receive_servers_thread = new Thread(new ThreadStart(receive_servers));
            send_heartbeats_thread = new Thread(new ThreadStart(send_heartbeats));
            this.id = id;
            this.lat = lat;
            this.lng = lng;
            lb = new TcpClient();
            lb.Connect(lbAddr, LB_PORT_NUM);
            Console.WriteLine("Connected to Load Balancer!");
            lb_stream = new StreamWriter(lb.GetStream(), Encoding.ASCII);
            lb_stream.AutoFlush = true;
        }



        public void start()
        {
            receive_servers_thread.Start();
            send_heartbeats_thread.Start();
        }



        private string h_type_to_string(heartbeat_type t)
        {
            return ((int)t).ToString() + ',';
        }
        private string h_type_to_string(heartbeat_type t, TcpClient client)
        {
            return ((int)t).ToString() + ',' + ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
        }



        private void receive_servers()
        {
            // Initialize connection info and start listening
            TcpListener listener = new TcpListener(localIPAddr, HEARTBEAT_PORT_NUM);
            listener.Start();

            while (true)
            {
                // Receive server connection
                TcpClient client = listener.AcceptTcpClient();
                client.NoDelay = true;
                client.SendTimeout = SEND_TIMEOUT_MILLISEC;
                StreamWriter stream_writer = new StreamWriter(client.GetStream(), Encoding.ASCII);
                stream_writer.AutoFlush = true;
                Console.WriteLine("Connected!");

                server_array_mutex.WaitOne();
                try
                {
                    int count = server_array.Count;
                    if (count == 0)
                    {
                        // Tell server it is in the front of the chain
                        stream_writer.WriteLine(h_type_to_string(heartbeat_type.FRONT_BACK));
                    }
                    else
                    {
                        // Tell server it is in the back of the chain
                        stream_writer.WriteLine(h_type_to_string(heartbeat_type.BACK));
                        // Tell previous last server to backup to this server
                        string send_str = "";
                        int prev_server_index = count - 1;
                        if (prev_server_index == 0) send_str = h_type_to_string(heartbeat_type.FRONT, client);
                        else send_str = h_type_to_string(heartbeat_type.MID, client);
                        StreamWriter prev_serve_stream_writer = ((Tuple<TcpClient, StreamWriter>)(server_array[prev_server_index])).Item2;
                        prev_serve_stream_writer.WriteLine(send_str);
                    }
                    // Store server Connection
                    server_array.Add(new Tuple<TcpClient, StreamWriter>(client,stream_writer));
                    string msg = "h,";
                    msg += id.ToString();
                    msg += ",";
                    msg += lat.ToString();
                    msg += ",";
                    msg += lng.ToString();
                    msg += ",";
                    foreach(var v in server_array)
                    {
                        TcpClient temp = ((Tuple<TcpClient,StreamWriter>)v).Item1;
                        msg += ((IPEndPoint)temp.Client.RemoteEndPoint).Address.ToString();
                        msg += ",";
                    }
                    msg += "h";
                    lb_stream.WriteLine(msg);
                }
                catch (Exception e)
                {
                    Console.Write(e.ToString());
                }
                server_array_mutex.ReleaseMutex();
                Thread.Yield();
            }
        }



        private void send_recovery(int first_alive_index)
        {
            string send_string = null;
            bool backup_found = false;
            int i = first_alive_index;
            int send_server_index = -1;
            while (!backup_found && i < server_array.Count)
            {
                try
                {
                    // Check if next alive
                    StreamWriter stream_writer = ((Tuple<TcpClient, StreamWriter>)(server_array[i])).Item2;
                    stream_writer.WriteLine(h_type_to_string(heartbeat_type.HEARTBEAT));
                    backup_found = true;
                }
                catch (Exception e)
                {
                    // Remove unresponsive server
                    ((Tuple<TcpClient, StreamWriter>)(server_array[i])).Item2.Close();
                    ((Tuple<TcpClient, StreamWriter>)(server_array[i])).Item1.Close();
                    server_array.RemoveAt(i);
                }
            }
            try
            {
                if (backup_found)
                {
                    if (i==0)
                    {
                        if (server_array.Count==1)
                        {
                            send_string = h_type_to_string(heartbeat_type.FRONT_BACK);
                            send_server_index = 0;
                        }
                        else if (server_array.Count==2)
                        {
                            send_string = h_type_to_string(heartbeat_type.FRONT);
                            send_server_index = 0;
                        }
                    }
                    else if (i==1)
                    {
                        send_string = h_type_to_string(heartbeat_type.FRONT, ((Tuple<TcpClient, StreamWriter>)(server_array[i])).Item1);
                        send_server_index = 0;
                    }
                    else
                    {
                        send_string = h_type_to_string(heartbeat_type.MID, ((Tuple<TcpClient, StreamWriter>)(server_array[i])).Item1);
                        send_server_index = i-1;
                    }
                }
                else
                {
                    if (i == 0)
                    {
                        Console.WriteLine("Full system failure:  all servers down.");
                    }
                    else if (i == 1)
                    {
                        send_string = h_type_to_string(heartbeat_type.FRONT_BACK);
                        send_server_index = 0;
                    }
                    else
                    {
                        send_string = h_type_to_string(heartbeat_type.BACK);
                        send_server_index = i-1;
                    }
                }
                StreamWriter stream_writer = ((Tuple<TcpClient, StreamWriter>)(server_array[send_server_index])).Item2;
                stream_writer.WriteLine(send_string);
                string msg = "h,";
                msg += id.ToString();
                msg += ",";
                msg += lat.ToString();
                msg += ",";
                msg += lng.ToString();
                msg += ",";
                foreach (var v in server_array)
                {
                    TcpClient temp = ((Tuple<TcpClient, StreamWriter>)(server_array[i])).Item1;
                    msg += ((IPEndPoint)temp.Client.RemoteEndPoint).Address.ToString();
                    msg += ",";
                }
                msg += "h";
                lb_stream.WriteLine(msg);
            }
            catch (Exception e)
            {
                // Do nothing
                Console.WriteLine(e.Message);
            }

        }



        private void send_heartbeats()
        {
            while (true)
            {
                int i = 0;
                bool sentToAll = false;
                while (!sentToAll)
                {
                    server_array_mutex.WaitOne();
                    if (i >= server_array.Count) sentToAll = true;
                    else
                    {
                        try
                        {
                            // Send hearbeat
                            StreamWriter stream_writer = ((Tuple<TcpClient, StreamWriter>)(server_array[i])).Item2;
                            stream_writer.WriteLine(h_type_to_string(heartbeat_type.HEARTBEAT));
                        }
                        catch (IOException e)
                        {
                            // Remove current connection
                            ((Tuple<TcpClient, StreamWriter>)(server_array[i])).Item2.Close();
                            ((Tuple<TcpClient, StreamWriter>)(server_array[i])).Item1.Close();
                            server_array.RemoveAt(i);
                            send_recovery(i);

                        }
                    }
                    server_array_mutex.ReleaseMutex();
                    i++;
                }
                Thread.Sleep(MILLISEC_BETWEEN_HEARTBEAT_LOOPS);
            }
        }
        // NORTH:  42.449877, -76.482906
        // SOUTH:  42.445048, -76.482048

        

        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: Heartbeat [local ip addr] [load balancer ip addr] [Heartbeat id]");
            }
            else
            {
                try
                {
                    int id = Int32.Parse(args[2]);
                    double lat;
                    double lng;
                    if(id == 0)
                    {
                        lat = MAX_LAT;
                        lng = (MAX_LNG - MIN_LNG) / 2 + MIN_LNG;
                    }
                    else
                    {
                        lat = MIN_LAT;
                        lng = (MAX_LNG - MIN_LNG) / 2 + MIN_LNG;
                    }
                    Heartbeat h = new Heartbeat(args[0], args[1], id,lat,lng);   // String is local ipaddr
                    h.start();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}
