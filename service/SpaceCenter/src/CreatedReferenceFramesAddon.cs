using System;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon that holds the reference frames clients create, so that a frame is let go of
    /// when the client that asked for it disconnects.
    /// </summary>
    /// <remarks>
    /// A created frame stands for nothing in the game, so nothing the game destroys ever
    /// retires one and the object would otherwise stay in the object store for the rest of
    /// the session. Its client removing it or going is the only thing that can say it is
    /// finished with.
    ///
    /// It is not a <see cref="ClientCleanupAddon" />, which gives up its collections on
    /// entering a scene: a frame is arithmetic the server holds on a client's behalf and is
    /// as valid in one scene as in another, so a scene change leaves it alone. It runs in
    /// every game scene because a frame can be created in any of them,
    /// <see cref="Services.ReferenceFrame" /> carrying no scene restriction of its own.
    /// </remarks>
    [KSPAddon (KSPAddon.Startup.AllGameScenes, false)]
    public sealed class CreatedReferenceFramesAddon : MonoBehaviour
    {
        static readonly ClientOwnedObjects<Services.ReferenceFrame> frames =
            new ClientOwnedObjects<Services.ReferenceFrame> (x => x.Release ());

        /// <summary>
        /// Record a frame a client has created.
        /// </summary>
        static internal void Add (Services.ReferenceFrame frame)
        {
            frames.Add (frame);
        }

        /// <summary>
        /// Stop holding a frame that its client has removed. Raises if the frame is not one
        /// this client created, which is also what a frame let go of on leaving a scene
        /// looks like.
        /// </summary>
        static internal void Remove (Services.ReferenceFrame frame)
        {
            if (!frames.OwnedByCaller (frame))
                throw new InvalidOperationException (
                    "Reference frame not found among those created by this client");
            frames.Remove (frame);
        }

        /// <summary>
        /// Let go of the frames of clients that have disconnected. This runs in Update
        /// rather than FixedUpdate, which stops while the game is paused, as nothing about
        /// a frame a client created is tied to the physics step.
        /// </summary>
        public void Update ()
        {
            frames.Sweep ();
        }
    }
}
