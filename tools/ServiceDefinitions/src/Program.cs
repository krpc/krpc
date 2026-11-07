using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Security;
using System.Text.Encodings.Web;
using System.Text.Json;
using KRPC.Tools;
using KRPC.Utils;

namespace ServiceDefinitions
{
    static class MainClass
    {
        static readonly Option<string> outputOption = new Option<string> ("--output", "-o") {
            Description = "Path to write the service definitions to. If unspecified, the output is written to standard output."
        };
        static readonly Option<string []> referenceDirOption = new Option<string []> ("--reference-dir") {
            Description = "Additional directory to search for referenced assemblies."
        };
        static readonly Argument<string> serviceArgument = new Argument<string> ("service") {
            Description = "Name of service to generate"
        };
        static readonly Argument<string []> assemblyArgument = new Argument<string []> ("assembly") {
            Arity = ArgumentArity.OneOrMore,
            Description = "Path(s) to assembly DLL(s) to load"
        };

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

        public static int Main (string [] args)
        {
            var root = new RootCommand ("Generate service definitions JSON file for a kRPC service") {
                outputOption,
                referenceDirOption,
                serviceArgument,
                assemblyArgument
            };
            root.SetAction (Run);
            return ToolCommandLine.Invoke (root, args);
        }

        static int Run (ParseResult result)
        {
            var outputPath = result.GetValue (outputOption);
            var assemblies = result.GetValue (assemblyArgument);

            Logger.Enabled = true;
            Logger.Level = Logger.Severity.Warning;
            var service = result.GetValue (serviceArgument);
            searchDirs.AddRange (result.GetValue (referenceDirOption));
            foreach (var path in assemblies)
                searchDirs.Add (Path.GetDirectoryName (Path.GetFullPath (path)));
            searchDirs.Add (AppContext.BaseDirectory);
            AssemblyLoadContext.Default.Resolving += ResolveAssembly;
            foreach (var path in assemblies) {
                try {
                    AssemblyLoadContext.Default.LoadFromAssemblyPath (Path.GetFullPath (path));
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
            var definitions = new Dictionary<string, object> { { service, services [service].Serialize () } };
            var jsonOptions = new JsonSerializerOptions {
                // The documentation holds XML, and is read as JSON rather than embedded in HTML
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };
            string output = JsonSerializer.Serialize (definitions, jsonOptions);
            if (outputPath != null)
                File.WriteAllText (outputPath, output);
            else
                Console.Write (output);
            return 0;
        }
    }
}
