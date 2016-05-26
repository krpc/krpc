using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Server;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// Addon for doing the drawing
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class Addon : MonoBehaviour
    {
        static IDictionary<IClient, IList<IDrawingObject>> objects = new Dictionary<IClient, IList<IDrawingObject>> ();

        internal static void AddObject (IDrawingObject obj)
        {
            var client = KRPCCore.Context.RPCClient;
            if (!objects.ContainsKey (client))
                objects [client] = new List<IDrawingObject> ();
            objects [client].Add (obj);
        }

        internal static void RemoveObject (IDrawingObject obj)
        {
            var client = KRPCCore.Context.RPCClient;
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
        public void Awake ()
        {
        }

        /// <summary>
        /// Update the addon
        /// </summary>
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
        public void OnDestroy ()
        {
            Clear ();
        }
    }
}
