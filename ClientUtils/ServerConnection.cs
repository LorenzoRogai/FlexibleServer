using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;

namespace ClientUtils.Net
{
    public class Packet
    {
        public string Header;
        public List<object> bodyValues;
        private char Delimiter = (char)1;
        public Packet(string Header)
        {
            this.Header = Header;
            bodyValues = new List<object>();
        }

        public void AddInt32(int Value)
        {
            bodyValues.Add(Value);
        }

        public void AddString(string Value)
        {
            bodyValues.Add(Value);
        }

        public void AddBoolean(bool Value)
        {
            bodyValues.Add(Value);
        }

        public string GetPacketString()
        {
            string PacketString = Header;
            foreach (object o in bodyValues)
            {
                PacketString += Delimiter.ToString() + o.ToString();
            }
            return PacketString;
        }
    }

    // State object for receiving data from remote device.
    class StateObject
    {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 256;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    public class ServerConnection
    {
        // ManualResetEvent instances signal completion.
        private ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        public delegate void onPacketReceiveHandler(object sender, Packet Packet);
        public event onPacketReceiveHandler onPacketReceive;

        private IPAddress ip;
        private int port;

        private char Delimiter = (char)1;

        // The response from the remote device.
        private String response = String.Empty;
        Socket client;
        public void Connect(IPAddress ip, int port)
        {
            this.ip = ip;
            this.port = port;
            // Connect to a remote device.
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(ip, port);

                // Create a TCP/IP socket.
                client = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();

                Receive();
            }
            catch
            {
                client.Close();
                Console.WriteLine("Can't connect to server on: " + ip + ":" + port);
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                Console.WriteLine("Client connected to server: {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch
            {
                client.Close();
                Console.WriteLine("Can't connect to server on: " + ip + ":" + port);
            }
        }

        private void Receive()
        {
            try
            {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {              
                Console.WriteLine("An error occurred while receiving message" + e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.                  
                    string Message = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);
                    string PacketHeader = Message.Split(Delimiter)[0];
                    Message = Message.Substring(Message.IndexOf(Delimiter) + 1);

                    Packet Packet = new Packet(PacketHeader);
                    foreach (string bodyValue in Message.Split(Delimiter))
                    {
                        Packet.AddString(bodyValue);
                    }

                    onPacketReceive(this, Packet);
                    // Get the rest of the data.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {          
                Console.WriteLine("An error occurred while receiving message" + e.ToString());
            }
        }

        public void Send(Packet Packet)
        {
            try
            {
                // Convert the string data to byte data using ASCII encoding.
                byte[] byteData = Encoding.ASCII.GetBytes(Packet.GetPacketString());

                // Begin sending the data to the remote device.
                client.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), client);
            }
            catch (Exception e)
            {                
                Console.WriteLine("An error occurred while sending message" + e.ToString());
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                //Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred while sending message" + e.ToString());
            }
        }
    }
}
