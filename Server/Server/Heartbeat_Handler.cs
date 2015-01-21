using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Xml.Linq;

using System.Text;
using System.Threading; 
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;



namespace Server
{

    class Heartbeat_Handler
    {
        // Heartbeat Handler Specific Variables
        public enum heartbeat_type { FRONT_BACK = 0, FRONT, BACK, MID, HEARTBEAT };
        public TcpClient heartbeat_client;
        public StreamReader heartbeat_stream_reader; 
        public string localIPAddrString;



        // References to server variables
        public ArrayList client_receive_listener_handlers;
        public ArrayList client_receive_message_handlers;
        public ArrayList lat_lng_handlers;
        public ArrayList event_handlers;
        public Thread xml_handler;
        public Thread xml_parser;
        public Thread client_send_listener_handler;
        public TcpClient next_backup_client;
        public IPAddress backup_ipaddr;
        public Server.server_state current_server_state;
        public ConcurrentQueue<Tuple<int, TcpClient, StreamWriter, StreamReader>> client_send_queue;
        public TcpListener client_send_connection_listener;
        public Server server;



        // Thread Arraylists contain Thread objects
        public Heartbeat_Handler (
            ArrayList client_receive_listener_handlers,
            ArrayList client_receive_message_handlers,
            ArrayList lat_lng_handlers,
            ArrayList event_handlers,
            Thread xml_handler,
            Thread xml_parser,
            Thread client_send_listener_handler,
            TcpClient next_backup_client,
            IPAddress backup_ipaddr,
            Server.server_state current_server_state,
            String heartbeat_ipaddr_str,
            ConcurrentQueue<Tuple<int, TcpClient, StreamWriter, StreamReader>> client_send_queue,
            TcpListener client_send_connection_listener,
            Server server
        )
        {
            this.client_receive_listener_handlers = client_receive_listener_handlers;
            this.client_receive_message_handlers = client_receive_message_handlers;
            this.lat_lng_handlers = lat_lng_handlers;
            this.event_handlers = event_handlers;
            this.xml_handler = xml_handler;
            this.xml_parser = xml_parser;
            this.client_send_listener_handler = client_send_listener_handler;
            this.current_server_state = current_server_state;
            this.next_backup_client = next_backup_client;
            this.backup_ipaddr = backup_ipaddr;
            this.localIPAddrString = heartbeat_ipaddr_str;
            this.client_send_queue = client_send_queue;
            this.client_send_connection_listener = client_send_connection_listener;
            this.server = server;

            IPAddress localIPAddr = IPAddress.Parse(this.localIPAddrString);
            heartbeat_client = new TcpClient(this.localIPAddrString, Server.HEARTBEAT_PORT_NUM);
            heartbeat_stream_reader = new StreamReader(heartbeat_client.GetStream(), Encoding.ASCII);
        }



        public void process_heartbeat_string(string cur_message)
        {
            string[] cur_message_arr = cur_message.Split(new char[1] { ',' }, 2);
            string received_ipaddr_str = cur_message_arr[1];
            bool received_ipaddr = (received_ipaddr_str != "");
            int heartbeat_type = Convert.ToInt32(cur_message_arr[0]);
            Server.server_state prev_server_state = current_server_state;
            switch (heartbeat_type)
            {
                case (0):
                    // FRONT_BACK
                    Console.WriteLine("FRONT_BACK received from heartbeat\n");
                    this.current_server_state = Server.server_state.FRONT_BACK;
                    if (next_backup_client != null)
                    {
                        next_backup_client.Close();
                        next_backup_client = new TcpClient();
                    }
                    this.backup_ipaddr = null;
                    if (prev_server_state != Server.server_state.FRONT && prev_server_state != Server.server_state.FRONT_BACK)
                    {
                        for (int j = 0; j < event_handlers.Count; j++) ((Thread)event_handlers[j]).Start();
                        for (int j = 0; j < lat_lng_handlers.Count; j++) ((Thread)lat_lng_handlers[j]).Start();
                        for (int j = 0; j < client_receive_message_handlers.Count; j++) ((Thread)client_receive_message_handlers[j]).Start();
                        for (int j = 0; j < client_receive_listener_handlers.Count; j++) ((Thread)client_receive_listener_handlers[j]).Start();
                        xml_parser.Abort();
                        xml_parser = new Thread(new ThreadStart(server.update_backup_state));
                    }
                    if (prev_server_state != Server.server_state.BACK && prev_server_state != Server.server_state.FRONT_BACK)
                    {
                        client_send_listener_handler = new Thread(new ThreadStart(server.listen_for_send_client_connections));
                        client_send_listener_handler.Start();
                    }
                    break;

                case (1):
                    // FRONT
                    Console.WriteLine("FRONT received from heartbeat\n");
                    current_server_state = Server.server_state.FRONT;
                    if (received_ipaddr)
                    {
                        this.backup_ipaddr = IPAddress.Parse(received_ipaddr_str);
                        if (next_backup_client!=null && next_backup_client.Connected) next_backup_client.Close();
                    }
                    if (prev_server_state != Server.server_state.FRONT && prev_server_state != Server.server_state.FRONT_BACK)
                    {
                        for (int j = 0; j < event_handlers.Count; j++) ((Thread)event_handlers[j]).Start();
                        for (int j = 0; j < lat_lng_handlers.Count; j++) ((Thread)lat_lng_handlers[j]).Start();
                        for (int j = 0; j < client_receive_message_handlers.Count; j++) ((Thread)client_receive_message_handlers[j]).Start();
                        for (int j = 0; j < client_receive_listener_handlers.Count; j++) ((Thread)client_receive_listener_handlers[j]).Start();
                    }

                    if (prev_server_state == Server.server_state.FRONT_BACK)
                    {
                        client_send_connection_listener.Stop();
                        client_send_listener_handler.Abort();
                        client_send_listener_handler = new Thread(new ThreadStart(server.listen_for_send_client_connections));
                        // Close all connections to clients
                        Tuple<int, TcpClient, StreamWriter, StreamReader> client_send_connection;
                        while (client_send_queue.Count > 0)
                        {
                            if (client_send_queue.TryDequeue(out client_send_connection))
                            {
                                client_send_connection.Item2.Close();
                                client_send_connection.Item4.Dispose();
                                client_send_connection.Item3.Dispose();
                            }
                        }
                    }

                    // THIS CASE SHOULD NEVER OCCUR
                    if (prev_server_state == Server.server_state.BACK)
                    {
                        client_send_connection_listener.Stop();
                        client_send_listener_handler.Abort();
                        client_send_listener_handler = new Thread(new ThreadStart(server.listen_for_send_client_connections));
                        // Close all connections to clients
                        Tuple<int, TcpClient, StreamWriter, StreamReader> client_send_connection;
                        while (client_send_queue.Count > 0)
                        {
                            if (client_send_queue.TryDequeue(out client_send_connection))
                            {
                                client_send_connection.Item2.Close();
                                client_send_connection.Item4.Dispose();
                                client_send_connection.Item3.Dispose();
                            }
                        }
                    }
                    break;

                case (2):
                    // BACK
                    Console.WriteLine("BACK received from heartbeat\n");
                    current_server_state = Server.server_state.BACK;
                    if (next_backup_client != null && next_backup_client.Connected) next_backup_client.Close();
                    next_backup_client = new TcpClient();
                    this.backup_ipaddr = null;
                    if (prev_server_state==Server.server_state.NOT_RUNNING) xml_parser.Start();
                    if (prev_server_state != Server.server_state.BACK && prev_server_state != Server.server_state.FRONT_BACK)
                    {
                        client_send_listener_handler.Start();
                    }
                    break;

                case (3):
                    // MID
                    Console.WriteLine("MID received from heartbeat\n");
                    current_server_state = Server.server_state.MID;
                    if (received_ipaddr)
                    {
                        this.backup_ipaddr = IPAddress.Parse(received_ipaddr_str);
                        if (next_backup_client != null && next_backup_client.Connected) next_backup_client.Close();
                    }

                    if (prev_server_state == Server.server_state.BACK)
                    {
                        client_send_connection_listener.Stop();
                        client_send_listener_handler.Abort();
                        client_send_listener_handler = new Thread(new ThreadStart(server.listen_for_send_client_connections));
                        // Close all connections to clients
                        Tuple<int, TcpClient, StreamWriter, StreamReader> client_send_connection;
                        while (client_send_queue.Count > 0)
                        {
                            if (client_send_queue.TryDequeue(out client_send_connection))
                            {
                                client_send_connection.Item2.Close();
                                client_send_connection.Item4.Dispose();
                                client_send_connection.Item3.Dispose();
                            }
                        }
                    }
                    break;

                case (4):
                    // HEARTBEAT
                    break;

                default:
                    Console.WriteLine("ERROR:  Invalid heartbeat type!");
                    break;
            }

            if (prev_server_state == Server.server_state.NOT_RUNNING)
            {
                xml_handler.Start();
            }
        }
        


        public void receive()
        {
            while (true)
            {
                String received_string = heartbeat_stream_reader.ReadLine().Trim();
                process_heartbeat_string(received_string);
            }
        }
    }
}