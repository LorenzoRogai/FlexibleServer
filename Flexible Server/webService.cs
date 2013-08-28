using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Reflection;

namespace FlexibleServer.Webservice
{
    class LoginValidator : UserNamePasswordValidator
    {
        /// <summary>
        /// Handle webService Login Event</summary>      
        /// <param name="userName">Username</param>   
        /// <param name="password">Password</param>   
        /// </summary>
        [System.Diagnostics.DebuggerHidden]
        public override void Validate(string userName, string password)
        {
            if ((userName != webService.Username) || (password != webService.Password))
            {
                Core.GetLogging().WriteLine("Failed Login on webService", LogLevel.Error);
                throw new SecurityTokenException();
            }
            else
            {
                Core.GetLogging().WriteLine("Validation success on webServer", LogLevel.Debug);
            }
        }
    }

    class webService
    {
        private IPAddress IP;
        private int Port;
        public static string Username;
        public static string Password;
        Assembly packetHandlerDllAssembly;

        /// <summary>
        /// initialize webService</summary>      
        /// <param name="Ip">Ip to bind</param>   
        /// <param name="port">Port to bind</param>   
        /// <param name="username">Set username</param>   
        /// <param name="password">Set password</param>      
        /// </summary>
        public webService(IPAddress IP, int Port, string username, string password)
        {          
            this.IP = IP;
            this.Port = Port;
            Username = username;
            Password = password;
        }

        /// <summary>
        /// Set assembly on which there are webService Methods
        /// <param name="packetHandlerDllAssembly">webService Methods DLL</param>      
        /// </summary>
        public void SetAssembly(Assembly packetHandlerDllAssembly)
        {
            this.packetHandlerDllAssembly = packetHandlerDllAssembly;
        }

        /// <summary>
        /// Custom webService Behavior to log incoming/outgoing Method call/response
        /// </summary>     
        class webServiceEvent : IEndpointBehavior, IDispatchMessageInspector
        {
            public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
            {
            }

            public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
            {
            }

            public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
            {
                endpointDispatcher.DispatchRuntime.MessageInspectors.Add(this);
            }

            public void Validate(ServiceEndpoint endpoint)
            {
            }

            public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
            {
                //Log incoming method call
                var action = OperationContext.Current.IncomingMessageHeaders.Action;
                var operationName = action.Substring(action.LastIndexOf("/") + 1);
                Core.GetLogging().WriteLine("Called webService Method: " + operationName, LogLevel.Debug);

                return null;
            }

            public void BeforeSendReply(ref Message reply, object correlationState)
            {
                //Log outgoing method response
                Core.GetLogging().WriteLine("Sent webService response: " + reply.Headers.Action.Substring(reply.Headers.Action.LastIndexOf("/") + 1), LogLevel.Debug);
            }
        }

        /// <summary>
        /// Start webService
        /// </summary>     
        public void Start()
        {
            Uri baseAddress = new Uri("http://" + IP.ToString() + ":" + Port + "/FlexibleServer/");

            //Get WebserviceHandler from User Created DLL
            Type Webservice = packetHandlerDllAssembly.GetType("WebserviceHandler");
            //Get Webservice interface from User Created DLL
            Type Interface = packetHandlerDllAssembly.GetType("IWebservice");
            //Get webService methods created by user
            foreach (MethodInfo m in Interface.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)) 
            {
                string param = "";

                //Get method parameters
                foreach (ParameterInfo pParameter in m.GetParameters())
                {
                    param += pParameter.Name + " (" + pParameter.ParameterType.Name + ") ";
                }

                if (param == "")
                    param = "none ";

                Core.GetLogging().WriteLine("webService Method name: " + m.Name + " parameters: " + param + "registered", LogLevel.Information);
            }

            // Create the ServiceHost. Bind on http://ip:port/FlexibleServer/
            ServiceHost selfHost = new ServiceHost(Webservice, baseAddress);

            //Binding to configure endpoint
            BasicHttpBinding http = new BasicHttpBinding();

            //Set a basic username/password authentication
            http.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;

            http.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

            try
            {
                 //Add the endpoint to the service host
                ServiceEndpoint endpoint = selfHost.AddServiceEndpoint(Interface, http, "RemoteControlService");
                //Add the Custom webService Behavior to endpoint
                endpoint.Behaviors.Add(new webServiceEvent());

                //Set the custom username/password validation
                selfHost.Credentials.UserNameAuthentication.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;
                selfHost.Credentials.UserNameAuthentication.CustomUserNamePasswordValidator = new LoginValidator();

                // Enable metadata publishing.
                ServiceMetadataBehavior smb = selfHost.Description.Behaviors.Find<ServiceMetadataBehavior>();
                if (smb == null)
                {
                    smb = new ServiceMetadataBehavior();
                    smb.HttpGetEnabled = true;
                    selfHost.Description.Behaviors.Add(smb);
                }

                try
                {
                    //Start webService
                    selfHost.Open();
                    Core.GetLogging().WriteLine("webService is ready on http://" + IP.ToString() + ":" + Port + "/FlexibleServer/", LogLevel.Information);
                }
                catch (Exception e)
                {
                    if (e is AddressAccessDeniedException)
                    {
                        Core.GetLogging().WriteLine("Could not register url: http://" + IP + ":" + Port + ". Start server as administrator", LogLevel.Error);
                    }

                    if (e is AddressAlreadyInUseException)
                    {
                        Core.GetLogging().WriteLine("Could not register url: http://" + IP + ":" + Port + ". Address already in use", LogLevel.Error);
                    }

                    Core.GetLogging().WriteLine("webService aborted due to an exception", LogLevel.Error);
                }
            }
            catch (CommunicationException ce)
            {
                Console.WriteLine("An exception occurred: {0}", ce.Message);
                selfHost.Abort();               
            }
        }
    }
}
