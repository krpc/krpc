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
            var service = args [0];
            var outputPath = args [1];
            for (var i = 2; i < args.Count (); i++)
                Assembly.LoadFrom (args[i]);
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
