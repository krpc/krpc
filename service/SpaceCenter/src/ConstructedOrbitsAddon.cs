using KRPC.Utils;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon that holds the orbits clients construct, so that an orbit is let go of when
    /// the client that asked for it disconnects.
    /// </summary>
    /// <remarks>
    /// A constructed orbit stands for nothing in the game, so nothing the game destroys
    /// ever retires one and the object would otherwise stay in the object store for the
    /// rest of the session. Its client going is the only thing that can say it is
    /// finished with.
    ///
    /// It is not a <see cref="ClientCleanupAddon" />, which gives up its collections on
    /// entering a scene: an orbit is arithmetic the server holds on a client's behalf and
    /// is as valid in one scene as in another, so a scene change leaves it alone. It runs
    /// in every game scene because an orbit can be constructed in any of them,
    /// <see cref="Services.Orbit" /> carrying no scene restriction of its own.
    /// </remarks>
    [KSPAddon (KSPAddon.Startup.AllGameScenes, false)]
    public sealed class ConstructedOrbitsAddon : MonoBehaviour
    {
        // Qualified, the global namespace holding a KSP class of the same name.
        static readonly ClientOwnedObjects<Services.Orbit> orbits =
            new ClientOwnedObjects<Services.Orbit> (x => x.Release ());

        /// <summary>
        /// Record an orbit a client has constructed.
        /// </summary>
        static internal void Add (Services.Orbit orbit)
        {
            orbits.Add (orbit);
        }

        /// <summary>
        /// Let go of the orbits of clients that have disconnected. This runs in Update
        /// rather than FixedUpdate, which stops while the game is paused, as nothing
        /// about an orbit a client constructed is tied to the physics step.
        /// </summary>
        public void Update ()
        {
            orbits.Sweep ();
        }
    }
}
