using System;
using System.Collections.Generic;
using System.IO;

namespace FlexibleServer.Configuration
{   
    public class ConfigurationData
    {
        public Dictionary<string, string> data;

        /// <summary>
        /// Load configuration file</summary>     
        /// <param name="filePath">Configuration file path</param>   
        public ConfigurationData(string filePath)
        {
            data = new Dictionary<string, string>();

            if (!File.Exists(filePath))
            {
                throw new Exception("Unable to locate configuration file at '" + filePath + "'.");
            }

            try
            {
                using (StreamReader stream = new StreamReader(filePath))
                {
                    string line = null;

                    while ((line = stream.ReadLine()) != null)
                    {
                        if (line.Length < 1 || line.StartsWith("#"))
                        {
                            continue;
                        }

                        int delimiterIndex = line.IndexOf('=');

                        if (delimiterIndex != -1)
                        {
                            string key = line.Substring(0, delimiterIndex);
                            string val = line.Substring(delimiterIndex + 1);

                            data.Add(key, val);
                        }
                    }

                    stream.Close();
                }
            }

            catch (Exception e)
            {
                throw new Exception("Could not process configuration file: " + e.Message);
            }
        }
    }
}
