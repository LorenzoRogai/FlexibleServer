using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Packet
{
    public string Header; //Name of the Packet
    public List<object> bodyValues; //List of Values
    public bool sendToAll; //Send to all Clients?
    public List<int> sendTo; //List of Connection ID to which the server will send the Packet
    private char Delimiter = (char)1; //Delimit Packet header and Packet body values using char(1)

    /// <summary>
    /// New Packet</summary>
    /// <param name="Header">The Packet header/name</param>     
    /// <param name="sendToAll">Send to all Clients?</param> 
    /// <param name="sendTo">List of Connection ID to which the server will send the Packet</param> 
    /// </summary>
    public Packet(string Header, bool sendToAll = false, List<int> sendTo = null)
    {       
        this.Header = Header;
        this.bodyValues = new List<object>();
        this.sendToAll = sendToAll;
        this.sendTo = sendTo;
    }

    /// <summary>
    /// Add integer value to Packet body</summary>
    /// <param name="Value">Integer value</param>     
    /// </summary>
    public void AddInt32(int Value) //Add a integer to Packet body
    {
        bodyValues.Add(Value);
    }

    /// <summary>
    /// Add string value to Packet body</summary>
    /// <param name="Value">String value</param>    
    /// </summary>
    public void AddString(string Value) //Add a string to Packet body
    {
        bodyValues.Add(Value);
    }

    /// <summary>
    /// Add boolean value to Packet body</summary>
    /// <param name="Value">Boolean value</param>     
    /// </summary>
    public void AddBoolean(bool Value) //Add a boolean to Packet body
    {
        bodyValues.Add(Value);
    }

    /// <summary>
    /// Return the final string value to be sent to client
    /// </summary>  
    public string GetPacketString()
    {
        string PacketString = Header;
        foreach (object o in bodyValues)
        {
            PacketString += Delimiter.ToString() + o.ToString(); //Add delimiter to each Packet body value
        }
        return PacketString;
    }
}

public class MethodResponse
{    
    public List<Packet> Packets; //List of Packets

    public MethodResponse()
    {        
        Packets = new List<Packet>();
    }

    /// <summary>
    /// Add new Packet to MethodResponse</summary>
    /// <param name="Packet">Packet</param>     
    public void AddPacket(Packet Packet)
    {
        Packets.Add(Packet);
    }
}

