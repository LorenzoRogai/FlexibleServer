using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.ComponentModel;
using System.Threading;

namespace FlexibleServer.Networking
{
    class Connection : ConnectionManager
    {
        public int connID;
        public byte[] buffer;
        public Socket socket;
    }

    class ConnectionManager
    {
        private List<Connection> _sockets; //List of client connection
        private Socket _serverSocket;
        private char Delimiter = (char)1; //Delimit Packet header and Packet body values using char(1)
        /// <summary>
        /// Start Server Connection</summary>
        /// <param name="ip">Ip to bind</param> 
        /// <param name="port">Port to bind</param> 
        /// </summary>
        public bool Start(IPAddress ip, int port)
        {
            _sockets = new List<Connection>();
            IPEndPoint serverEndPoint;
            try
            {
                serverEndPoint = new IPEndPoint(ip, port); //Create a new endpoint on ip:port
            }
            catch (System.ArgumentOutOfRangeException e)
            {
                throw new ArgumentOutOfRangeException("Port number entered would seem to be invalid, should be between 1024 and 65000", e);
            }
            try
            {
                _serverSocket = new Socket(serverEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp); //Server socket
            }
            catch (System.Net.Sockets.SocketException e)
            {
                throw new ApplicationException("Could not create socket, check to make sure not duplicating port", e);
            }
            try
            {
                _serverSocket.Bind(serverEndPoint); //Bind endpoint
                _serverSocket.Listen(10); //Start listening
            }
            catch (Exception e)
            {
                Core.GetLogging().WriteLine("Error occured while binding socket, check inner exception" + e, LogLevel.Error);
                Core.GetLogging().WriteLine("Closing...", LogLevel.Error);
                Thread.Sleep(5000);
                Environment.Exit(0);
            }
            try
            {
                //Begins to accept incoming connection attempt
                _serverSocket.BeginAccept(new AsyncCallback(acceptCallback), _serverSocket);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Error occured starting listeners, check inner exception", e);
            }
            return true;
        }

        /// <summary>
        /// Accepted connection callback</summary>
        /// <param name="result">Status of asynchronous operation</param>        
        /// </summary>
        private void acceptCallback(IAsyncResult result)
        {
            Connection conn = new Connection();
            try
            {
                //Finish accepting the connection
                Socket s = (Socket)result.AsyncState;
                conn = new Connection();
                conn.socket = s.EndAccept(result);
                conn.buffer = new byte[512];              
                lock (_sockets)
                {
                    _sockets.Add(conn); //Add client to the list
                }
                conn.connID = _sockets.IndexOf(conn);
                Core.GetLogging().WriteLine("[" + conn.connID + "] New Connection from " + ((IPEndPoint)conn.socket.RemoteEndPoint).Address, LogLevel.Debug);

                OnClientConnect(conn);

                //Queue receiving of data from the connection
                conn.socket.BeginReceive(conn.buffer, 0, conn.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), conn);
                //Queue the accept of the next incomming connection
                _serverSocket.BeginAccept(new AsyncCallback(acceptCallback), _serverSocket);
            }
            catch
            {
                Core.GetLogging().WriteLine("[" + conn.connID + "] Connection lost from " + ((IPEndPoint)conn.socket.RemoteEndPoint).Address, LogLevel.Debug);

                OnClientDisconnect(conn);

                if (conn.socket != null)
                {
                    conn.socket.Close();
                    lock (_sockets)
                    {
                        _sockets.Remove(conn);
                    }
                }
                //Queue the next accept
                _serverSocket.BeginAccept(new AsyncCallback(acceptCallback), _serverSocket);
            }
        }

        /// <summary>
        /// Convert string to byte array</summary>
        /// <param name="str">String value</param>        
        /// </summary>
        public static byte[] StrToByteArray(string str)
        {
            return new UTF8Encoding().GetBytes(str);
        }

        /// <summary>
        /// On Packet received callback</summary>
        /// <param name="result">Status of asynchronous operation</param>           
        /// </summary>
        private void ReceiveCallback(IAsyncResult result)
        {
            //get our connection from the callback
            Connection conn = (Connection)result.AsyncState;

            try
            {
                //Grab our buffer and count the number of bytes receives
                int bytesRead = conn.socket.EndReceive(result);

                if (bytesRead > 0)
                {
                    HandlePacket(ParseMessage(Encoding.ASCII.GetString(conn.buffer, 0, bytesRead), conn), conn);
                  
                    //Queue the next receive
                    conn.socket.BeginReceive(conn.buffer, 0, conn.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), conn);
                }
                else //Client disconnected
                {
                    Core.GetLogging().WriteLine("[" + conn.connID + "] Connection lost from " + ((IPEndPoint)conn.socket.RemoteEndPoint).Address, LogLevel.Debug);

                    OnClientDisconnect(conn);

                    conn.socket.Close();
                    lock (_sockets)
                    {
                        _sockets.Remove(conn);
                    }
                }
            }
            catch (SocketException e)
            {
                Core.GetLogging().WriteLine("[" + conn.connID + "] Connection lost from " + ((IPEndPoint)conn.socket.RemoteEndPoint).Address, LogLevel.Debug);

                OnClientDisconnect(conn);

                if (conn.socket != null)
                {
                    conn.socket.Close();
                    lock (_sockets)
                    {
                        _sockets.Remove(conn);
                    }
                }
            }
        }

        /// <summary>
        /// Invoke OnClientConnect on User Created Dll</summary>      
        /// <param name="conn">Client connection</param>
        /// </summary>     
        private void OnClientConnect(Connection conn)
        {
            Core.dllInstance.GetType().GetMethod("OnClientConnect").Invoke(Core.dllInstance, new object[] { conn.connID });
        }

        /// <summary>
        /// Invoke OnClientDisconnect on User Created Dll</summary>      
        /// <param name="conn">Client connection</param>
        /// </summary>     
        private void OnClientDisconnect(Connection conn)
        {
            Core.dllInstance.GetType().GetMethod("OnClientDisconnect").Invoke(Core.dllInstance, new object[] { conn.connID });
        }

        /// <summary>
        /// Send message to all clients</summary>
        /// <param name="message">Byte array of the message</param>           
        /// </summary>     
        public bool sendToAll(byte[] message)
        {
            foreach (Connection conn in _sockets)
            {
                if (conn != null && conn.socket.Connected)
                {
                    lock (conn.socket)
                    {
                        conn.socket.Send(message, message.Length, SocketFlags.None);
                    }
                }
                else
                    continue;
            }
            return true;
        }

        /// <summary>
        /// Send message to specific client connection</summary>
        /// <param name="message">Byte array of the message</param>  
        /// <param name="conn">Client connection</param>
        /// </summary>
        public bool Send(byte[] message, Connection conn)
        {
            if (conn != null && conn.socket.Connected)
            {
                lock (conn.socket)
                {
                    conn.socket.Send(message, message.Length, SocketFlags.None);
                }
            }
            else
                return false;
            return true;
        }

        /// <summary>
        /// Return currently connected clients</summary>    
        /// </summary>
        public int GetActiveConnectionsCount()
        {
            return _sockets.Count;
        }

        /// <summary>
        /// Parse message string to Packet class</summary>
        /// <param name="message">Packet string</param>   
        /// <param name="conn">Client connection</param>
        /// </summary>
        private Packet ParseMessage(string Message, Connection conn)
        {
            string PacketHeader = Message.Split(Delimiter)[0];

            Packet Packet = new Packet(PacketHeader);

            Message = Message.Substring(Message.IndexOf(Delimiter) + 1); //Only Packet Body

            //Parse type from incoming packet body values
            foreach (string Parameter in Message.Split(Delimiter))
            {
                //TO-DO more type parsing
                int intN;
                bool boolN;
                if (int.TryParse(Parameter, out intN))
                {
                    Packet.AddInt32(intN);
                }
                else if (Boolean.TryParse(Parameter, out boolN))
                {
                    Packet.AddBoolean(boolN);
                }
                else
                {
                    Packet.AddString(Parameter);
                }
            }

            //Always add connID to Packet to get client id on User Created DLL
            Packet.AddInt32(conn.connID);

            return Packet;
        }

        /// <summary>
        /// Invoke the packet-associated method and send response packets contained in MethodResponse</summary>    
        /// <param name="Packet">The incoming packet</param>
        /// <param name="conn">The parsed packet</param>
        /// </summary>
        private void HandlePacket(Packet Packet, Connection conn)
        {
            Core.GetLogging().WriteLine("Received Packet: " + Packet.GetPacketString(), LogLevel.Debug);
            //Get associated Packet method using packet header/name
            Method Method = Core.GetMethodByName(Packet.Header.ToLower());
            if (Method != null)
            {
                //Packet body values count must match with method parameters count
                if (Method.GetParametersCount() != Packet.bodyValues.Count)
                {
                    Core.GetLogging().WriteLine("Method: " + Method.GetName() + " has " + Method.GetParametersCount() + " params but client request has " + Packet.bodyValues.Count + " params", LogLevel.Error);
                }
                else
                {
                    MethodResponse result = null;
                    try
                    {
                        //Try invoke associated method given packet body values as parameters
                        result = (MethodResponse)Method.GetMethodInfo().Invoke(Core.dllInstance, Packet.bodyValues.ToArray());
                    }
                    catch (Exception e)
                    {
                        Core.GetLogging().WriteLine("Error handling Method: " + Method.GetName() + " Exception Message: " + e.Message, LogLevel.Error);
                    }
                    if (result != null)
                    {                      
                        Core.GetLogging().WriteLine("Handled Method: " + Method.GetName() + ". Sending response..", LogLevel.Information);

                        //Invoke succeed! now read Packets contained in MethodResponse and send them to the specified clients
                        foreach (Packet PacketToSend in result.Packets)
                        {
                            string PacketString = PacketToSend.GetPacketString();
                            if (PacketToSend.sendToAll) //Send to all clients
                            {
                                sendToAll(StrToByteArray(PacketString));
                                Core.GetLogging().WriteLine("Sent response: " + PacketString + " to all clients", LogLevel.Debug);
                            }
                            else if (PacketToSend.sendTo != null) //Only send to clients specified in a list
                            {
                                foreach (int connID in PacketToSend.sendTo)
                                {
                                    Send(StrToByteArray(PacketString), _sockets[connID]);
                                    Core.GetLogging().WriteLine("Sent response: " + PacketString + " to client id: " + connID, LogLevel.Debug);
                                }
                            }
                            else //Send to sender
                            {
                                Send(StrToByteArray(PacketString), conn);
                                Core.GetLogging().WriteLine("Sent response: " + PacketString + " to client id: " + conn.connID, LogLevel.Debug);
                            }
                        }
                    }
                }
            }
            else Core.GetLogging().WriteLine("Invoked Method: " + Packet.Header + " does not exist", LogLevel.Error);
        }
    }
}
