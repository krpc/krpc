using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service;
using KRPC.Service.Scanner;
using UnityEngine;

namespace KRPC
{
    /// <summary>
    /// Check kRPC services.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Instantly, true)]
    public sealed class ServicesChecker : MonoBehaviour
    {
        internal static bool OK { get; private set; }

        internal static bool CheckDocumented { get; set; }

        readonly List<string> notDocumented = new List<string> ();

        /// <summary>
        /// Start the services checker addon
        /// </summary>
        public void Start ()
        {
            OK = true;
            try {
                var services = Scanner.GetServices ();
                if (CheckDocumented)
                    CheckDocumentation (services.Values);
            } catch (ServiceException e) {
                OK = false;
                var msg = e.Message + Environment.NewLine + (e.Assembly == null ? "unknown" : e.Assembly.Location);
                PopupDialog.SpawnPopupDialog (new Vector2 (0.5f, 0.5f), new Vector2 (0.5f, 0.5f), "kRPC service error - plugin disabled", msg, "OK", true, HighLogic.UISkin);
            }
        }

        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        void CheckDocumentation (IEnumerable<ServiceSignature> services)
        {
            foreach (var service in services)
                CheckDocumentation (service);
            if (notDocumented.Count > 0) {
                var n = notDocumented.Count;
                var msg = n + " item" + (n != 1 ? "s are" : " is") + " not documented.";
                for (int i = 0; i < 10 && i < n; i++)
                    msg += Environment.NewLine + notDocumented [i];
                if (n > 10)
                    msg += Environment.NewLine + "...";
                PopupDialog.SpawnPopupDialog (new Vector2 (0.5f, 0.5f), new Vector2 (0.5f, 0.5f), "kRPC service warning", msg, "OK", true, HighLogic.UISkin);
            }
        }

        void CheckDocumentation (ServiceSignature service)
        {
            CheckDocumentation (service.Name, service.Documentation);
            foreach (var cls in service.Classes.Values)
                CheckDocumentation (cls);
            foreach (var enm in service.Enumerations.Values)
                CheckDocumentation (enm);
            foreach (var proc in service.Procedures.Values)
                CheckDocumentation (proc);
        }

        void CheckDocumentation (ProcedureSignature proc)
        {
            CheckDocumentation (proc.FullyQualifiedName, proc.Documentation);
        }

        void CheckDocumentation (ClassSignature cls)
        {
            CheckDocumentation (cls.FullyQualifiedName, cls.Documentation);
        }

        void CheckDocumentation (EnumerationSignature enm)
        {
            CheckDocumentation (enm.FullyQualifiedName, enm.Documentation);
            foreach (var enmValue in enm.Values)
                CheckDocumentation (enmValue.FullyQualifiedName, enmValue.Documentation);
        }

        void CheckDocumentation (string fullyQualifiedName, string documentation)
        {
            if (documentation.Length == 0)
                notDocumented.Add (fullyQualifiedName);
        }
    }
}
