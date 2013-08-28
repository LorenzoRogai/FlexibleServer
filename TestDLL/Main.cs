//--------------------------------------------
//   Flexible Server - Example of User DLL
//              Chat Server
//--------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Data;
using MysqlConnector;


public class WebserviceHandler : IWebservice
{
    public string[] GetUserList()
    {
        List<string> Names = new List<string>();
        foreach (User User in PacketHandler.Users)
            Names.Add(User.Name + "\n");
        return Names.ToArray();
    }

    public int GetUserCount()
    {
        return PacketHandler.Users.Count;
    }
}


[ServiceContract]
public interface IWebservice
{
    [OperationContract]
    string[] GetUserList();
    [OperationContract]
    int GetUserCount();
}

public class User
{
    public int connID;
    public string Name;

    public User(int connID, string Name)
    {
        this.connID = connID;
        this.Name = Name;
    }
}

//PacketHandler class must be public to let server reads methods
public class PacketHandler
{
    public static List<User> Users;
    private Logging Logging;
    //public Mysql MysqlConn;
    /// <summary>
    /// Initialize variables  
    ///  </summary>
    public PacketHandler()
    {
        Users = new List<User>();
        Logging = new Logging();
        Logging.MinimumLogLevel = 0;

        //MysqlConn = new Mysql();
        //MysqlConn.Connect("127.0.0.1", 3306, "root", "123456", "unity");

        //MysqlConn.GetClient();
    }

    /// <summary>
    /// Return Chat User by Connection ID
    /// <param name="connID">Connection ID</param>     
    /// </summary>

    //Prevent server to load this methods using private flag
    private User GetUserByConnID(int connID)
    {
        foreach (User u in Users)
        {
            if (u.connID == connID)
                return u;
        }
        return null;
    }

    /// <summary>
    /// Return Chat User by Name
    /// <param name="Name">User Name</param>     
    /// </summary>
    private User GetUserByName(string Name)
    {
        foreach (User u in Users)
        {
            if (u.Name == Name)
                return u;
        }
        return null;
    }

    /// <summary>
    /// Handle Chat User Login
    /// <param name="username">Username given by Chat User</param>     
    /// <param name="password">Password given by Chat User</param>     
    /// <param name="connID">Connection ID provided by server</param>     
    /// </summary>

    //Public method must return a result of type MethodResponse 
    public MethodResponse Login(object username, object password, int connID)
    {
        //Create a new MethodResponse
        MethodResponse MethodResponse = new MethodResponse();

        //Check if user exists from mysql
        //DataRow Row = MysqlConn.ReadDataRow("SELECT * FROM users where username = '" + username + "' AND password = '" + password + "'");

        bool loginFailed = true;

        if (password.ToString() == "password")
            loginFailed = false;
        //if (Row != null)
        //{
            //loginFailed = false;
        //}

        if (loginFailed)
        {
            //Create a new Packet LOGIN_RESPONSE and send Packet to the sender
            Packet LoginResponse = new Packet("LOGIN_RESPONSE");
            //Add a boolean value to Packet. It means login failed
            LoginResponse.AddBoolean(false);
            //Add Packet to MethodResponse
            MethodResponse.AddPacket(LoginResponse);
        }
        else
        {
            Packet LoginResponse = new Packet("LOGIN_RESPONSE");
            LoginResponse.AddBoolean(true);//It means successful login
            //Add a int value to Packet. It provides client the connection ID for future use
            LoginResponse.AddInt32(connID);

            //Announce to all clients a new user joined
            //Set sendToAll parameter to true (default false) if you want to send Packet to all clients
            Packet UserJoin = new Packet("USER_JOIN", true);
            //Add the name of the Chat User joined
            UserJoin.AddString(username.ToString());

            //Add Packets to MethodResponse
            MethodResponse.AddPacket(LoginResponse);
            MethodResponse.AddPacket(UserJoin);

            Users.Add(new User(connID, username.ToString())); //Add the Chat User to a List

            //Write on server console from dll
            Logging.WriteLine("User: " + username + " has joined the chat", LogLevel.Information);
        }

        return MethodResponse; //Return MethodResponse to Server
    }

    /// <summary>
    /// Handle Chat User New Message
    /// <param name="Message">The message written by Chat User</param>        
    /// <param name="connID">Connection ID provided by server</param>     
    /// </summary>
    public MethodResponse Message(string Message, int connID)
    {
        MethodResponse MethodResponse = new MethodResponse();

        Packet MessageResponse = new Packet("MESSAGE_RESPONSE", true); //Create a new Packet MESSAGE_RESPONSE and set sendToAll to true
        MessageResponse.AddString(GetUserByConnID(connID).Name); //Add the name of the Chat User using Connection ID
        MessageResponse.AddString(Message); //Add the message written by Chat User
        MethodResponse.AddPacket(MessageResponse); //Add Packet to MethodResponse

        return MethodResponse; //Return MethodResponse to Server
    }

    /// <summary>
    /// Handle Chat User Whisper
    /// <param name="toUser">The whisper target</param>    
    /// <param name="Message">The message written by Chat User</param>    
    /// <param name="connID">Connection ID provided by server</param>     
    /// </summary>
    public MethodResponse Whisper(string toUser, string Message, int connID)
    {
        MethodResponse MethodResponse = new MethodResponse();

        if (GetUserByName(toUser) != null) //Check if whisper target is online
        {
            //Create a list with Connection ID to which the server will send the Packet
            List<int> toUsers = new List<int>();
            toUsers.Add(GetUserByName(toUser).connID);

            Packet MessageResponse = new Packet("WHISPER_RESPONSE", false, toUsers); //Create a new Packet WHISPER_RESPONSE providing the list with Connections ID
            MessageResponse.AddString(GetUserByConnID(connID).Name); //Add the whisper sender name
            MessageResponse.AddString(Message); //Add the message written by sender
            MethodResponse.AddPacket(MessageResponse); //Add Packet to MethodResponse
        }
        else
        {
            Packet MessageResponse = new Packet("WHISPER_RESPONSE"); //Create a new Packet WHISPER_RESPONSE
            MessageResponse.AddString(toUser); //Add the name of whisper target
            MessageResponse.AddString("currently offline");
            MethodResponse.AddPacket(MessageResponse); //Add Packet to MethodResponse
        }

        return MethodResponse; //Return MethodResponse to Server
    }

    /// <summary>
    /// Must always be declared. it will be called when a client disconnect  
    /// <param name="connID">Connection ID provided by server</param>     
    /// </summary>
    public void OnClientDisconnect(int connID)
    {
        if (GetUserByConnID(connID) != null)
        {
            Logging.WriteLine("User: " + GetUserByConnID(connID).Name + " has left the chat", LogLevel.Information);
            Users.Remove(GetUserByConnID(connID));
        }
    }

    /// <summary>
    /// Must always be declared. it will be called when a client connect  
    /// <param name="connID">Connection ID provided by server</param>     
    /// </summary>
    public void OnClientConnect(int connID)
    {

    }
}