using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Server;
using KRPC.Service;
using UnityEngine;

namespace KRPC.Drawing
{
    /// <summary>
    /// Addon for doing the drawing
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class Addon : MonoBehaviour
    {
        static IDictionary<IClient, IList<IDrawable>> objects = new Dictionary<IClient, IList<IDrawable>> ();

        internal static void AddObject (IDrawable obj)
        {
            var client = CallContext.Client;
            if (!objects.ContainsKey (client))
                objects [client] = new List<IDrawable> ();
            objects [client].Add (obj);
        }

        internal static void RemoveObject (IDrawable obj)
        {
            var client = CallContext.Client;
            if (!objects.ContainsKey (client) || !objects [client].Contains (obj))
                throw new ArgumentException ("Drawing object not found");
            obj.Destroy ();
            objects [client].Remove (obj);
        }

        internal static void Clear ()
        {
            foreach (var clientObjects in objects.Values)
                foreach (var obj in clientObjects)
                    obj.Destroy ();
            objects.Clear ();
        }

        internal static void Clear (IClient client)
        {
            if (objects.ContainsKey (client)) {
                foreach (var obj in objects [client])
                    obj.Destroy ();
                objects.Remove (client);
            }
        }

        /// <summary>
        /// Wake the addon
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void Awake ()
        {
        }

        /// <summary>
        /// Update the addon
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void Update ()
        {
            if (!objects.Any ())
                return;

            var disconnectedClients = objects.Keys.Where (x => !x.Connected).ToList ();
            foreach (var client in disconnectedClients)
                Clear (client);

            foreach (var clientObjects in objects.Values)
                foreach (var obj in clientObjects)
                    obj.Update ();
        }

        /// <summary>
        /// Destroy the addon
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void OnDestroy ()
        {
            Clear ();
        }
    }
}
