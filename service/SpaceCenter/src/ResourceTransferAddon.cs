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
            new ClientOwnedObjects<ResourceTransfer> (transfer => transfer.Release ());

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
        /// Stop holding a transfer that its client has removed. Raises if the transfer is
        /// not one this client started, which is also what a transfer let go of on leaving
        /// the flight scene looks like.
        /// </summary>
        static internal void Remove (ResourceTransfer transfer)
        {
            transfers.RemoveOwnedByCaller (transfer, "Resource transfer");
        }

        /// <summary>
        /// Update the transfers, first stopping those whose client has disconnected
        /// so they move no more resource.
        /// </summary>
        /// <remarks>
        /// A transfer that has finished is kept, rather than dropped here: it goes on
        /// answering for how much it moved, and it is the client's object until that
        /// client removes it or disconnects. Updating it does nothing.
        /// </remarks>
        public void FixedUpdate ()
        {
            Sweep ();
            foreach (var transfer in transfers.Items)
                transfer.Update (Time.fixedDeltaTime);
        }
    }
}
