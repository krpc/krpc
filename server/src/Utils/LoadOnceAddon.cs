using UnityEngine;

namespace KRPC.Utils
{
    /// <summary>
    /// Base class for addons that load an external API exactly once, in the first
    /// scene to start.
    /// </summary>
    /// <remarks>
    /// Generic on the derived type so that each addon gets its own <c>loaded</c> flag:
    /// a static field on a non-generic base would be shared across all subclasses, so
    /// the first addon to load would suppress every other one. The flag must be static
    /// (not per-instance) because KSP creates a fresh addon instance in each scene, and
    /// the API only needs loading once for the lifetime of the game.
    /// </remarks>
    public abstract class LoadOnceAddon<T> : MonoBehaviour where T : LoadOnceAddon<T>
    {
        static bool loaded;

        /// <summary>
        /// Load the API, once per game.
        /// </summary>
        public void Start ()
        {
            if (loaded)
                return;
            loaded = true;
            Load ();
        }

        /// <summary>
        /// Load the external API. Called exactly once, in the first scene to start.
        /// </summary>
        protected abstract void Load ();
    }
}
