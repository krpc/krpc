using System;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection;
using System.IO;

namespace ServiceDefinitions
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            var inputPath = args [0];
            var service = args [1];
            var outputPath = args [2];
            Assembly.LoadFrom (inputPath);
            var services = KRPC.Service.Scanner.Scanner.GetServices ();
            foreach (var key in services.Keys.ToList()) {
                if (key != service) {
                    services.Remove (key);
                }
            }
            string output = JsonConvert.SerializeObject (services, Formatting.Indented);
            File.WriteAllText (outputPath, output);
        }
    }
}
