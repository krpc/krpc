using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Server;
using KRPC.Service;

namespace KRPC.Utils
{
    /// <summary>
    /// An override on a single in-game object, tagged with the client that owns it (so
    /// it can be released when that client disconnects).
    /// </summary>
    public abstract class ClientOwnedEntry
    {
        /// <summary>
        /// The client that owns this entry, or null if it was set outside an RPC.
        /// </summary>
        public IClient Owner { get; set; }
    }

    /// <summary>
    /// A set of overrides of one kind, keyed by the in-game object being overridden.
    /// Owns the shared lifecycle: installing an override on enable, releasing it on
    /// disable, and dropping overrides whose owning client has disconnected or whose
    /// key object has been destroyed. The per-kind behaviour is supplied as delegates.
    /// </summary>
    public sealed class ClientOwnedOverrides<TKey, TEntry> : IClientOwnedCollection
        where TKey : UnityEngine.Object
        where TEntry : ClientOwnedEntry
    {
        readonly IDictionary<TKey, TEntry> entries = new Dictionary<TKey, TEntry> ();
        readonly Func<TKey, TEntry> install;
        readonly Action<TKey, TEntry> release;

        /// <summary>
        /// Create a set of overrides, installed and released by the given delegates.
        /// The release delegate should restore any state saved by the install
        /// delegate, and must handle its key having been destroyed.
        /// </summary>
        public ClientOwnedOverrides (Func<TKey, TEntry> install, Action<TKey, TEntry> release)
        {
            this.install = install;
            this.release = release;
        }

        /// <summary>
        /// Whether the given object is being overridden.
        /// </summary>
        public bool IsSet (TKey key)
        {
            return entries.ContainsKey (key);
        }

        /// <summary>
        /// The override for the given object, or null if it is not being overridden.
        /// </summary>
        public TEntry Get (TKey key)
        {
            TEntry entry;
            return entries.TryGetValue (key, out entry) ? entry : null;
        }

        /// <summary>
        /// The override for the given object, re-tagged with the calling client, or
        /// null if it is not being overridden. Used when changing a live override's
        /// command.
        /// </summary>
        public TEntry Own (TKey key)
        {
            var entry = Get (key);
            if (entry != null)
                entry.Owner = CallContext.Client;
            return entry;
        }

        /// <summary>
        /// Enable or disable the override on the given object. Enabling installs the
        /// override (or re-tags an existing one) owned by the calling client;
        /// disabling releases it.
        /// </summary>
        public void Set (TKey key, bool enabled)
        {
            var entry = Get (key);
            if (entry != null) {
                if (enabled)
                    entry.Owner = CallContext.Client;
                else {
                    release (key, entry);
                    entries.Remove (key);
                }
            } else if (enabled) {
                entry = install (key);
                entry.Owner = CallContext.Client;
                entries [key] = entry;
            }
        }

        /// <summary>
        /// Release the overrides whose key object has been destroyed or whose owning
        /// client has disconnected.
        /// </summary>
        public void Sweep ()
        {
            foreach (var key in entries.Keys.ToList ()) {
                var entry = entries [key];
                if (Destroyed (key) || ClientConnections.Disconnected (entry.Owner)) {
                    release (key, entry);
                    entries.Remove (key);
                }
            }
        }

        /// <summary>
        /// Release all overrides.
        /// </summary>
        public void Clear ()
        {
            foreach (var entry in entries)
                release (entry.Key, entry.Value);
            entries.Clear ();
        }

        static bool Destroyed (UnityEngine.Object obj)
        {
            return obj == null;
        }
    }
}
