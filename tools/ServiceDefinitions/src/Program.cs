using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
#if NET
using System.Runtime.Loader;
#endif
using KRPC.Utils;
using NDesk.Options;
using Newtonsoft.Json;

namespace ServiceDefinitions
{
    static class MainClass
    {
        static void Help (OptionSet options)
        {
            Console.Error.WriteLine ("usage: ServiceDefinitions.exe [-h] [-v] [--output=PATH] [--reference-dir=PATH]... service assembly...");
            Console.Error.WriteLine ();
            Console.Error.WriteLine ("Generate service definitions JSON file for a kRPC service");
            Console.Error.WriteLine ();
            options.WriteOptionDescriptions (Console.Error);
            Console.Error.WriteLine ("  service                    Name of service to generate");
            Console.Error.WriteLine ("  assembly...                Path(s) to assembly DLL(s) to load");
        }

#if NET
        static readonly List<string> searchDirs = new List<string> ();

        /// <summary>
        /// Resolves references of the loaded assemblies (for example UnityEngine
        /// and KSP assemblies referenced by service assemblies) from the
        /// directories given by --reference-dir, the directories containing the
        /// assemblies being scanned, and the application directory. Assemblies
        /// that are not part of the application are not resolved automatically
        /// by the .NET host.
        /// </summary>
        static Assembly ResolveAssembly (AssemblyLoadContext context, AssemblyName name)
        {
            foreach (var dir in searchDirs) {
                var candidate = Path.Combine (dir, name.Name + ".dll");
                if (File.Exists (candidate))
                    return context.LoadFromAssemblyPath (Path.GetFullPath (candidate));
            }
            return null;
        }
#endif

        public static int Main (string [] args)
        {
            bool showHelp = false;
            bool showVersion = false;
            string outputPath = null;
            var referenceDirs = new List<string> ();

            var options = new OptionSet { {
                    "h|help", "show this help message and exit",
                    v => showHelp = v != null
                }, {
                    "v|version", "show program's version number and exit",
                    v => showVersion = v != null
                }, {
                    "o|output=", "{PATH} to write the service definitions to. If unspecified, the output is written to stanadard output.",
                    (string v) => outputPath = v
                }, {
                    "reference-dir=", "Additional directory {PATH} to search for referenced assemblies.",
                    (string v) => referenceDirs.Add (v)
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
#if NET
            searchDirs.AddRange (referenceDirs);
            for (var i = 1; i < positionalArgs.Count; i++)
                searchDirs.Add (Path.GetDirectoryName (Path.GetFullPath (positionalArgs [i])));
            searchDirs.Add (AppContext.BaseDirectory);
            AssemblyLoadContext.Default.Resolving += ResolveAssembly;
#endif
            for (var i = 1; i < positionalArgs.Count; i++) {
                var path = positionalArgs [i];

                try {
#if NET
                    AssemblyLoadContext.Default.LoadFromAssemblyPath (Path.GetFullPath (path));
#else
                    AssemblyName name = AssemblyName.GetAssemblyName (path);
                    Assembly.Load (name);
#endif
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
