using System.Collections.Generic;
using KRPC.SpaceCenter.Services.Parts;
using KRPC.Utils;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon to apply forces to parts.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class PartForcesAddon : ClientCleanupAddon
    {
        static readonly ClientOwnedObjects<Force> forces =
            new ClientOwnedObjects<Force> ();
        static readonly ClientOwnedObjects<Force> instantaneousForces =
            new ClientOwnedObjects<Force> ();

        static readonly IClientOwnedCollection[] collections = { forces, instantaneousForces };

        /// <summary>
        /// The forces currently being applied.
        /// </summary>
        protected override IEnumerable<IClientOwnedCollection> Collections {
            get { return collections; }
        }

        /// <summary>
        /// Add a force
        /// </summary>
        static internal void Add (Force force)
        {
            forces.Add (force);
        }

        /// <summary>
        /// Add an instantaneous force
        /// </summary>
        static internal void AddInstantaneous (Force force)
        {
            instantaneousForces.Add (force);
        }

        /// <summary>
        /// Remove a force
        /// </summary>
        static internal void Remove (Force force)
        {
            forces.Remove (force);
        }

        /// <summary>
        /// Apply the forces to the parts, first dropping the forces of any client that
        /// has disconnected so they are not applied again.
        /// </summary>
        public void FixedUpdate ()
        {
            Sweep ();
            foreach (var force in instantaneousForces.Items)
                force.Update ();
            instantaneousForces.Clear ();
            foreach (var force in forces.Items)
                force.Update ();
        }
    }
}
