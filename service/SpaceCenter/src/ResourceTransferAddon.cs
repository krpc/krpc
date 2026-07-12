using System.Collections.Generic;
using KRPC.SpaceCenter.Services;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon to perform resource transfers between parts.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class ResourceTransferAddon : ClientCleanupAddon
    {
        static readonly ClientOwnedObjects<ResourceTransfer> transfers =
            new ClientOwnedObjects<ResourceTransfer> (transfer => transfer.Cancel ());

        static readonly IClientOwnedCollection[] collections = { transfers };

        /// <summary>
        /// The transfers currently in progress.
        /// </summary>
        protected override IEnumerable<IClientOwnedCollection> Collections {
            get { return collections; }
        }

        /// <summary>
        /// Add a new transfer
        /// </summary>
        static internal void AddTransfer (ResourceTransfer transfer)
        {
            transfers.Add (transfer);
        }

        /// <summary>
        /// Update the transfers, first cancelling those whose client has disconnected
        /// so they move no more resource.
        /// </summary>
        public void FixedUpdate ()
        {
            Sweep ();
            foreach (var transfer in transfers.Items)
                transfer.Update (Time.fixedDeltaTime);
            transfers.RemoveAll (transfer => transfer.Complete);
        }
    }
}
