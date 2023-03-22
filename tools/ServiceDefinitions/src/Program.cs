using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using KRPC.Utils;
using NDesk.Options;
using Newtonsoft.Json;

namespace ServiceDefinitions
{
    static class MainClass
    {
        static void Help (OptionSet options)
        {
            Console.Error.WriteLine ("usage: ServiceDefinitions.exe [-h] [-v] [--output=PATH] service assembly...");
            Console.Error.WriteLine ();
            Console.Error.WriteLine ("Generate service definitions JSON file for a kRPC service");
            Console.Error.WriteLine ();
            options.WriteOptionDescriptions (Console.Error);
            Console.Error.WriteLine ("  service                    Name of service to generate");
            Console.Error.WriteLine ("  assembly...                Path(s) to assembly DLL(s) to load");
        }

        [SuppressMessage ("Gendarme.Rules.Portability", "ExitCodeIsLimitedOnUnixRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public static int Main (string [] args)
        {
            bool showHelp = false;
            bool showVersion = false;
            string outputPath = null;

            var options = new OptionSet { {
                    "h|help", "show this help message and exit",
                    v => showHelp = v != null
                }, {
                    "v|version", "show program's version number and exit",
                    v => showVersion = v != null
                }, {
                    "o|output=", "{PATH} to write the service definitions to. If unspecified, the output is written to stanadard output.",
                    (string v) => outputPath = v
                }
            };
            List<string> positionalArgs = options.Parse (args);

            if (showHelp) {
                Help (options);
                return 0;
            }

            if (showVersion) {
                var version = Assembly.GetEntryAssembly ().GetName().Version;
                Console.Error.WriteLine ("ServiceDefinitions.exe version " + version);
                return 0;
            }

            if (positionalArgs.Count < 2) {
                Console.Error.WriteLine ("Not enough arguments");
                return 1;
            }

            Logger.Enabled = true;
            Logger.Level = Logger.Severity.Warning;
            var service = positionalArgs [0];
            for (var i = 1; i < positionalArgs.Count; i++) {
                var path = positionalArgs [i];

                try {
                    AssemblyName name = AssemblyName.GetAssemblyName (path);
                    Assembly.Load (name);
                } catch (FileNotFoundException) {
                    Console.Error.WriteLine ("Assembly '" + path + "' not found.");
                    return 1;
                } catch (FileLoadException e) {
                    Console.Error.WriteLine ("Failed to load assembly '" + path + "'.");
                    Console.Error.WriteLine (e.Message);
                    if (e.InnerException != null)
                        Console.Error.WriteLine (e.InnerException.Message);
                    return 1;
                } catch (BadImageFormatException) {
                    Console.Error.WriteLine ("Failed to load assembly '" + path + "'. Bad image format.");
                    return 1;
                } catch (SecurityException) {
                    Console.Error.WriteLine ("Failed to load assembly '" + path + "'. Security exception.");
                    return 1;
                } catch (PathTooLongException) {
                    Console.Error.WriteLine ("Failed to load assembly '" + path + "'. File name too long.");
                    return 1;
                }
            }
            var services = KRPC.Service.Scanner.Scanner.GetServices ();
            if (!services.ContainsKey (service)) {
                Console.Error.WriteLine ("Service " + service + " not found");
                return 1;
            }
            services = new Dictionary<string, KRPC.Service.Scanner.ServiceSignature> { { service, services [service] } };
            string output = JsonConvert.SerializeObject (services, Formatting.Indented);
            if (outputPath != null)
                File.WriteAllText (outputPath, output);
            else
                Console.Write (output);
            return 0;
        }
    }
}
