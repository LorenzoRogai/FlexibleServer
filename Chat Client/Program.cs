using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using ClientUtils.Net;
namespace Chat_Client
{
    class Program
    {
        static int id = 0;
        static ServerConnection conn;
        static bool Logged = false;
        static string username = "";

        /// <summary>
        /// Entry point  
        /// </summary>
        static void Main(string[] args)
        {
            conn = new ServerConnection();
            //Connect to the server on 127.0.0.1:8888
            conn.Connect(IPAddress.Parse("127.0.0.1"), 8888);
            //Handle received Packet
            conn.onPacketReceive += new ServerConnection.onPacketReceiveHandler(HandlePacket);

            Login(); //Ask login
            while (true)
            {
                if (Logged) //Read message to send only when user is logged in
                {
                    string textToSend = Console.ReadLine(); //Read message

                    if (textToSend.Split(' ')[0] == "whisper") //Check if user want to send a whisper to someone
                    {
                        string toUser = textToSend.Split(' ')[1]; //Whisper target name

                        Packet Whisper = new Packet("WHISPER"); //Create a new Packet WHISPER
                        Whisper.AddString(toUser); //Add whisper target name

                        //Add the message  
                        Whisper.AddString(textToSend.Substring(textToSend.IndexOf(textToSend.Split(' ')[2])));  
                        conn.Send(Whisper); //Send the Packet to the server

                        //Clear last console line
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                        ClearCurrentConsoleLine();

                        Console.WriteLine("Sent whisper to:" + toUser);
                    }
                    else
                    {
                        Packet Message = new Packet("MESSAGE"); //Create a new Packet MESSAGE
                        Message.AddString(textToSend); //The message that all Chat Users will read
                        conn.Send(Message); //Send the Packet to the server
                    }
                }
            }
        }

        /// <summary>
        /// Ask Login
        /// </summary>
        static void Login()
        {
            Console.WriteLine("Write username");
            username = Console.ReadLine(); //Read user input
            Console.WriteLine("Write password");
            string password = Console.ReadLine(); //Read user input

            Packet Login = new Packet("LOGIN"); //Create a new Packet LOGIN
            Login.AddString(username); //Add the username to Packet
            Login.AddString(password); //Add the password to Packet

            conn.Send(Login); //Send the Packet to the server
        }

        /// <summary>
        /// Handle the received Packet
        /// <param name="sender">Class on which the event has been fired</param>     
        /// <param name="Packet">The received packet</param>     
        /// </summary>
        static void HandlePacket(object sender, Packet Packet)
        {
            switch (Packet.Header)
            {
                case "LOGIN_RESPONSE": //Received LOGIN_RESPONSE Packet
                    {
                        bool loginResponse = Convert.ToBoolean(Packet.bodyValues[0]); //Get Login Response from Packet Body
                        if (!loginResponse)
                        {
                            Console.WriteLine("Login failed");
                            Login(); //Ask login until logged
                        }
                        else
                        {
                            id = int.Parse(Packet.bodyValues[1].ToString()); //Get Connection ID from Packet Body
                            Console.WriteLine("Login Successful");
                            Logged = true; //User has logged in
                        }
                    }
                    break;

                case "USER_JOIN": //Received USER_JOIN Packet
                    {
                        string Name = Packet.bodyValues[0].ToString(); //Get the name of the Chat User that has logged in from Packet Body
                        Console.WriteLine("User: " + Name + " has joined the chat");
                    }
                    break;

                case "MESSAGE_RESPONSE": //Received WHISPER_RESPONSE Packet
                    {
                        string SenderName = Packet.bodyValues[0].ToString(); //Get the name of the sender
                        string Message = Packet.bodyValues[1].ToString(); //Get the message of the sender
                        if (SenderName == username) //Clear current line only if sender is a third person
                        {
                            Console.SetCursorPosition(0, Console.CursorTop - 1);
                            ClearCurrentConsoleLine();
                        }
                        Console.WriteLine(SenderName + ": " + Message);
                    }
                    break;

                case "WHISPER_RESPONSE": //Received WHISPER_RESPONSE Packet
                    {
                        string SenderName = Packet.bodyValues[0].ToString(); //Get the name of the sender
                        string Message = Packet.bodyValues[1].ToString(); //Get the message of the sender
                        Console.WriteLine("Whisper from " + SenderName + ": " + Message);
                    }
                    break;
            }
        }

        /// <summary>
        /// Clear the current console line
        /// </summary>
        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int i = 0; i < Console.WindowWidth; i++)
                Console.Write(" ");
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}
