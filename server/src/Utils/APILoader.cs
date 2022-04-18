using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

namespace KRPC.Utils
{
    /// <summary>
    /// Utilities to load APIs from an assembly without needing to depend on it.
    /// </summary>
    public static class APILoader
    {
        /// <summary>
        /// Load an API
        /// </summary>
        /// <returns>A type object set to the type of the API if it was successfully loaded, <c>null</c> otherwise.</returns>
        /// <param name="api">A type specifying the interface.</param>
        /// <param name="assemblyName">Name of the assembly to load.</param>
        /// <param name="apiName">Name of the API to load.</param>
        /// <param name="requiredVersion">Required API version.</param>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public static Type Load (Type api, string assemblyName, string apiName, Version requiredVersion = null)
        {
            if (api == null)
                throw new ArgumentNullException (nameof (api));

            // Find the assembly
            var assembly = AssemblyLoader.loadedAssemblies.FirstOrDefault (a => a.assembly.GetName ().Name == assemblyName);
            if (assembly == null) {
                Logger.WriteLine ("Load API: " + assemblyName + " not found; skipping");
                Console.WriteLine ("Load API: " + assemblyName + " not found; skipping");
                return null;
            }

            // Version check
            var version = new Version (assembly.versionMajor, assembly.versionMinor);
            if (requiredVersion != null) {
                if (version.CompareTo (requiredVersion) < 0) {
                    Error ("Failed to load " + assemblyName + "; found version " + version + " but version >= " + requiredVersion + " is required");
                    return null;
                }
            }

            // Get type of APIs static class
            var type = assembly.assembly.GetTypes ().FirstOrDefault (t => t.FullName == apiName);
            if (type == null) {
                Error (apiName + " not found in " + assemblyName);
                return null;
            }

            // Load the API methods
            var apiMethods = type.GetMethods ();
            foreach (var property in api.GetProperties()) {
                // Skip the property if it does not return a delegate
                if (!property.GetGetMethod ().ReturnType.IsSubclassOf (typeof(Delegate)))
                    continue;

                // Assign the API method to the property
                var method = apiMethods.FirstOrDefault (m => m.Name.Equals (property.Name));
                if (method == null) {
                    Error ("Method not found for " + property.Name);
                    return null;
                }
                var f = Delegate.CreateDelegate (property.PropertyType, type, method.Name);
                property.SetValue (null, f, null);
            }

            Logger.WriteLine ("Load API: Successfully loaded " + assemblyName + " version " + version);
            Console.WriteLine ("Load API: Successfully loaded " + assemblyName + " version " + version);
            return type;
        }

        static void Error (string message)
        {
            Logger.WriteLine ("Load API: " + message, Logger.Severity.Error);
            Compatibility.SpawnPopupDialog (new Vector2 (0.5f, 0.5f), new Vector2 (0.5f, 0.5f), "krpc-api-loader",
                                            "kRPC API Loader", message, "OK", true, HighLogic.UISkin);
        }
    }
}
