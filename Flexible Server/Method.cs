using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace FlexibleServer
{
    public class Parameter
    {
        private string Name; //Parameter name
        private Type Type; //Parameter type

        /// <summary>
        /// Parameter
        /// <param name="Name">Parameter name</param>   
        /// <param name="Type">Parameter type</param>   
        /// </summary>
        public Parameter(string Name, Type Type)
        {
            this.Name = Name;
            this.Type = Type;
        }
    }

    public class Method
    {
        private MethodInfo MethodInfo; //MethodInfo needed to invoke method from DLL
        private string Name; //Method Name
        private List<Parameter> Parameters; //Method parameters

        /// <summary>
        /// Method
        /// <param name="Name">Method name</param>   
        /// <param name="MethodInfo">MethodInfo</param>   
        /// </summary>
        public Method(string Name, MethodInfo MethodInfo)
        {
            this.Name = Name;
            this.MethodInfo = MethodInfo;
            Parameters = new List<Parameter>();
        }

        /// <summary>
        /// Add Parameter to List
        /// <param name="Name">Parameter name</param>   
        /// <param name="Type">Parameter Type</param>   
        /// </summary>
        public void AddParameter(string Name, Type Type)
        {
            Parameters.Add(new Parameter(Name, Type));
        }

        /// <summary>
        /// Return Parameters Count
        /// </summary>
        public int GetParametersCount()
        {
            return Parameters.Count;
        }

        /// <summary>
        /// Return List of Parameters
        /// </summary>
        public List<Parameter> GetParameters()
        {
            return Parameters;
        }

        /// <summary>
        /// Return Method name
        /// </summary>
        public string GetName()
        {
            return Name;
        }

        /// <summary>
        /// Return MethodInfo
        /// </summary>
        public MethodInfo GetMethodInfo()
        {
            return MethodInfo;
        }
    }
}
