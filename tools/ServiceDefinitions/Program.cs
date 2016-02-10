using System;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Security;
using KRPC.Utils;

namespace ServiceDefinitions
{
    class MainClass
    {
        public static int Main (string[] args)
        {
            if (args.Length < 2) {
                Console.Error.WriteLine ("Not enough arguments.\nUsage: ServiceDefinitions.exe SERVICE OUTPUT ASSEMBLYPATH...");
                return 1;
            }
            Logger.Enabled = true;
            Logger.Level = Logger.Severity.Warning;
            var service = args [0];
            var outputPath = args [1];
            for (var i = 2; i < args.Count (); i++) {
                var path = args[i];
                try {
                    Assembly.LoadFrom (path);
                }
                catch (FileNotFoundException)
                {
                    Console.Error.WriteLine ("Assembly '"+ path +"' not found.");
                    return 1;
                }
                catch (FileLoadException e)
                {
                    Console.Error.WriteLine ("Failed to load assembly '"+ path +"'.");
                    Console.Error.WriteLine (e.Message);
                    if (e.InnerException != null)
                        Console.Error.WriteLine (e.InnerException.Message);
                    return 1;
                }
                catch (BadImageFormatException)
                {
                    Console.Error.WriteLine ("Failed to load assembly '"+ path +"'. Bad image format.");
                    return 1;
                }
                catch (SecurityException)
                {
                    Console.Error.WriteLine ("Failed to load assembly '"+ path +"'. Security exception.");
                    return 1;
                }
                catch (PathTooLongException)
                {
                    Console.Error.WriteLine ("Failed to load assembly '"+ path +"'. File name too long.");
                    return 1;
                }
            }
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
