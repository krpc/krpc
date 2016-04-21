using System;
using System.Linq;
using UnityEngine;

namespace KRPC.SpaceCenter.ExternalAPI
{
    /// <summary>
    /// Addon to load external APIs
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Instantly, true)]
    public class Loader : MonoBehaviour
    {
        /// <summary>
        /// Load the external APIs
        /// </summary>
        public void Awake ()
        {
            FAR.Load ();
            RemoteTech.Load ();
        }

        internal static bool LoadAPI (Type api, string assemblyName, string apiName, Version requiredVersion)
        {
            // Find the assembly
            var assembly = AssemblyLoader.loadedAssemblies
                .FirstOrDefault (a => a.assembly.GetName ().Name.Equals (assemblyName));
            if (assembly == null) {
                log (assemblyName + " not found; skipping");
                return false;
            }
            var version = new Version (assembly.versionMajor, assembly.versionMinor);

            // Version check
            if (version.CompareTo (requiredVersion) < 0) {
                error ("Failed to load " + assemblyName + "; found version " + version + " but version >=" + requiredVersion + " is required");
                return false;
            }

            // Get type of API's static class
            var type = assembly.assembly.GetTypes ().FirstOrDefault (t => t.FullName.Equals (apiName));
            if (type == null) {
                error (apiName + " not found in " + assemblyName);
                return false;
            }

            var apiMethods = type.GetMethods ();
            foreach (var property in api.GetProperties()) {
                // Skip the property if it does not return a delegate
                if (!property.GetGetMethod ().ReturnType.IsSubclassOf (typeof(Delegate)))
                    continue;

                // Assign the API method to the property
                var method = apiMethods.FirstOrDefault (m => m.Name.Equals (property.Name));
                if (method == null) {
                    error ("Method not found for " + property.Name);
                    return false;
                }
                var f = Delegate.CreateDelegate (property.PropertyType, type, method.Name);
                property.SetValue (null, f, null);
            }

            log ("Successfully loaded " + assemblyName + " version " + version);
            return true;
        }

        static void log (string message)
        {
            Console.WriteLine ("[kRPCSpaceCenter] LoadAPI: " + message);
        }

        static void error (string message)
        {
            log (message);
            PopupDialog.SpawnPopupDialog (new Vector2 (0.5f, 0.5f), new Vector2 (0.5f, 0.5f), "kRPCSpaceCenter error - LoadAPI", message, "OK", true, HighLogic.UISkin);
        }
    }
}
