using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Security.Permissions;
using System.Net;
using System.IO;
using System.Reflection;
using System.Globalization;
using FlexibleServer.Networking;
using FlexibleServer.Configuration;
using FlexibleServer.Webservice;

namespace FlexibleServer
{
    public class Core
    {
        private static ConnectionManager connMgr;
        private static Logging Logging;
        private static ConfigurationData Configuration;
        public static List<Method> RegisteredMethods;
        public static object dllInstance;
        private static webService WebService;
        
        /// <summary>
        /// Entry point  
        /// </summary>
        static void Main(string[] args)
        {                   
            //Set console height,width,title
            Console.WindowHeight = Console.LargestWindowHeight - 25;
            Console.WindowWidth = Console.LargestWindowWidth / 2;
            Console.Title = "Flexible Server";

            //Load configuration file
            Configuration = new ConfigurationData("flexibleserver-config.conf");

            Logging = new Logging();

            //Set the MinimumLogLevel reading value from configuration file
            Logging.MinimumLogLevel = (LogLevel)int.Parse(GetConfig().data["MinimumLogLevel"]);
            
            Logging.WriteLine("Initializing Flexible Server", LogLevel.Information);                      

            Logging.WriteLine("Configurations Loaded", LogLevel.Information);

            //Get User Created DLL
            string handlerDLL = GetConfig().data["packetHandlerDLL"];

            Assembly packetHandlerDllAssembly = null;
            //Check if User Created DLL exists else close Server
            if (File.Exists(handlerDLL))
            {
                //Load User Created DLL Assembly
                packetHandlerDllAssembly = Assembly.LoadFrom(AppDomain.CurrentDomain.BaseDirectory + handlerDLL); 
                Logging.WriteLine("Loading Packet Handler DLL", LogLevel.Information);

                //Get PacketHandler Class
                Type Class = packetHandlerDllAssembly.GetType("PacketHandler");
                try
                {
                    //Create a instance of PacketHandler Class
                    dllInstance = Activator.CreateInstance(Class);
                }
                catch (Exception e)
                {
                    Logging.WriteLine("User Created DLL must have PacketHandler Class. Closing..", LogLevel.Error);
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }

                int MethodsCount = 0;
                //Create a list of methods        
                RegisteredMethods = new List<Method>();                 

                bool OnClientConnectMethodFound = false;
                bool OnClientDisconnectMethodFound = false;
                //Get methods created by user
                foreach (MethodInfo MethodInfo in Class.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)) 
                {
                    //Check if OnClientConnect and OnClientDisconnect methods exist
                    if (MethodInfo.Name == "OnClientConnect")
                    {
                        OnClientConnectMethodFound = true;
                        continue;
                    }
                    
                    if (MethodInfo.Name == "OnClientDisconnect")
                    {
                        OnClientDisconnectMethodFound = true;
                        continue;
                    }

                    //Only load methods with MethodResponse return type
                    if (MethodInfo.ReturnType != typeof(MethodResponse))
                    {
                        Logging.WriteLine("Method: " + MethodInfo.Name + " must return MethodResponse currently: " + MethodInfo.ReturnType.Name, LogLevel.Error);
                        Logging.WriteLine("Method: " + MethodInfo.Name + " not registered", LogLevel.Error);
                        continue;
                    }
                    string param = "";
                    //Create a new method class. MethodInfo is necessary for future invokes of DLL Methods
                    Method Method = new Method(MethodInfo.Name, MethodInfo);
                    //Method must have connID(int) Param
                    bool connIDParameterFound = false;
                    //Get method parameters
                    foreach (ParameterInfo pParameter in MethodInfo.GetParameters())
                    {
                        //Add Parameter
                        Method.AddParameter(pParameter.Name, pParameter.ParameterType);
                        param += pParameter.Name + " (" + pParameter.ParameterType.Name + ") ";
                        if (pParameter.Name.ToLower() == "connid" && pParameter.ParameterType == typeof(int))
                        {
                            connIDParameterFound = true;
                        }
                    }

                    if (!connIDParameterFound)
                    {
                        Logging.WriteLine("Method: " + MethodInfo.Name + " must have a connID(int) param", LogLevel.Error);
                        Logging.WriteLine("Method: " + MethodInfo.Name + " not registered", LogLevel.Error);
                        continue;
                    }

                    if (param == "")
                        param = "none ";

                    //Add method to the registered methods list
                    RegisteredMethods.Add(Method);

                    Logging.WriteLine("Method name: " + MethodInfo.Name + " parameters: " + param + "registered", LogLevel.Information);
                    MethodsCount++;
                }

                if (!OnClientConnectMethodFound || !OnClientDisconnectMethodFound)
                {
                    Logging.WriteLine("PacketHandler must contain OnClientConnect and OnClientDisconnect methods. Closing..", LogLevel.Error);
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }

                //Close server if there is any registered method
                if (MethodsCount == 0)
                {
                    Logging.WriteLine("Any method loaded. Closing..", LogLevel.Information);
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }
                Logging.WriteLine("Registered " + MethodsCount + " Methods", LogLevel.Information);
                Logging.WriteLine("Loaded Packet Handler DLL", LogLevel.Information);
            }
            else
            {
                Logging.WriteLine("Unable to locate Packet Handler DLL named: " + handlerDLL + ". Closing..", LogLevel.Error);
                Thread.Sleep(5000);
                Environment.Exit(0);
            }

            //Start server on ip:port
            connMgr = new ConnectionManager();
            IPAddress ip = IPAddress.Parse(GetConfig().data["tcp.bindip"]);
            int port = int.Parse(GetConfig().data["tcp.port"]);
            connMgr.Start(ip, port);
            

            if (int.Parse(GetConfig().data["enableWebService"]) != 0)
            {
                //Start webService on ip:port and authentication username password
                WebService = new webService(IPAddress.Parse(GetConfig().data["webservice.bindip"]), int.Parse(GetConfig().data["webservice.port"]), GetConfig().data["webservice.username"], GetConfig().data["webservice.password"]);
                //Set assembly on which there are webService Methods
                WebService.SetAssembly(packetHandlerDllAssembly);
                //Start webService
                WebService.Start();
            }
            else
                WebService = null;

            Logging.WriteLine("Server Listening on: " + ip.ToString() + " port: " + port, LogLevel.Information);

            //Create a new thread to update console title with the active connections count
            Thread Monitor = new Thread(mainMonitor);
            Monitor.Start();
        }

        /// <summary>
        /// Return Configuration List</summary>      
        static void mainMonitor()
        {
            while (true)
            {           
                Console.Title = "Flexible Server - Active Connections: " + connMgr.GetActiveConnectionsCount();
                Thread.Sleep(5000);              
            }
        }

        /// <summary>
        /// Return Configuration List</summary>      
        /// </summary>
        public static ConfigurationData GetConfig()
        {
            return Configuration;           
        }

        /// <summary>
        /// Return Registered Method</summary>
        /// <param name="Name">Registered Method Name</param> 
        /// </summary>
        public static Method GetMethodByName(string Name)
        {
            foreach (Method m in RegisteredMethods)
            {
                if (m.GetName().ToLower() == Name)
                    return m;
            }

            return null;
        }

        /// <summary>
        /// Return Logging
        /// </summary>
        public static Logging GetLogging()
        {
            return Logging;
        }
    }
}
