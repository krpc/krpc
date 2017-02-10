using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using KRPC.SpaceCenter.Services;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon to perform resource transfers between parts.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class ResourceTransferAddon : MonoBehaviour
    {
        /// <summary>
        /// The transfers currently in progress.
        /// </summary>
        static readonly List<ResourceTransfer> transfers = new List<ResourceTransfer> ();

        /// <summary>
        /// Destroy the addon
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void OnDestroy ()
        {
            transfers.Clear ();
        }

        /// <summary>
        /// Add a new transfer
        /// </summary>
        static internal void AddTransfer (ResourceTransfer transfer)
        {
            transfers.Add (transfer);
        }

        /// <summary>
        /// Update the transfers
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void FixedUpdate ()
        {
            foreach (var transfer in transfers)
                transfer.Update (Time.fixedDeltaTime);
            transfers.RemoveAll (transfer => transfer.Complete);
        }
    }
}
