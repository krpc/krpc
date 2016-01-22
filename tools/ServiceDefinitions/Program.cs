using System;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using KRPC.Utils;

namespace ServiceDefinitions
{
    class MainClass
    {
        public static int Main (string[] args)
        {
            if (args.Length < 2) {
                Console.Error.WriteLine ("Not enough arguments.\nUsage: ServiceDefinitions.exe SERVICE OUTPUT");
                return 1;
            }
            Logger.Enabled = true;
            Logger.Level = Logger.Severity.Warning;
            var service = args [0];
            var outputPath = args [1];
            for (var i = 2; i < args.Count (); i++)
                Assembly.LoadFrom (args [i]);
            var services = KRPC.Service.Scanner.Scanner.GetServices ();
            if (!services.ContainsKey (service)) {
                Console.Error.WriteLine ("Service " + service + " not found");
                return 1;
            }
            services = new Dictionary<string,KRPC.Service.Scanner.ServiceSignature> { { service, services [service] } };
            string output = JsonConvert.SerializeObject (services, Formatting.Indented);
            File.WriteAllText (outputPath, output);
            return 0;
        }
    }
}
