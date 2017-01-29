using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Transfer resources between parts.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class ResourceTransfer
    {
        readonly Part internalFromPart;
        readonly Part internalToPart;
        readonly PartResourceDefinition internalResource;
        readonly float transferRate;

        ResourceTransfer (Part fromPart, Part toPart, PartResourceDefinition resource, float amount)
        {
            internalFromPart = fromPart;
            internalToPart = toPart;
            internalResource = resource;
            FromPart = new Parts.Part (fromPart);
            ToPart = new Parts.Part (toPart);
            Resource = resource.name;
            TotalAmount = amount;
            // Compute the transfer rate (in units/sec) as one tenth the size of the destination tank (determined experimentally from the KSP transfer UI)
            var totalStorage = (float)toPart.Resources.Get (resource.id).maxAmount;
            transferRate = 0.1f * totalStorage;
            ResourceTransferAddon.AddTransfer (this);
        }

        /// <summary>
        /// Start transferring a resource transfer between a pair of parts. The transfer will move at most
        /// <paramref name="maxAmount"/> units of the resource, depending on how much of the resource is
        /// available in the source part and how much storage is available in the destination part.
        /// Use <see cref="Complete"/> to check if the transfer is complete.
        /// Use <see cref="Amount"/> to see how much of the resource has been transferred.
        /// </summary>
        /// <param name="fromPart">The part to transfer to.</param>
        /// <param name="toPart">The part to transfer from.</param>
        /// <param name="resource">The name of the resource to transfer.</param>
        /// <param name="maxAmount">The maximum amount of resource to transfer.</param>
        [KRPCMethod]
        [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
        public static ResourceTransfer Start (Parts.Part fromPart, Parts.Part toPart, string resource, float maxAmount)
        {
            if (ReferenceEquals (fromPart, null))
                throw new ArgumentNullException (nameof (fromPart));
            if (ReferenceEquals (toPart, null))
                throw new ArgumentNullException (nameof (toPart));
            // Get the internal part objects
            var internalFromPart = fromPart.InternalPart;
            var internalToPart = toPart.InternalPart;
            // Check the parts are in the same vessel
            if (internalFromPart.vessel.id != internalToPart.vessel.id)
                throw new ArgumentException ("Parts are not on the same vessel");
            // Check the parts are different
            if (internalFromPart.flightID == internalToPart.flightID)
                throw new ArgumentException ("Source and destination parts are the same");
            // Get the resource info object
            if (!PartResourceLibrary.Instance.resourceDefinitions.Contains (resource))
                throw new ArgumentException ("Resource '" + resource + "' does not exist");
            var resourceInfo = PartResourceLibrary.Instance.GetDefinition (resource);
            // Check the parts contain the required resource
            if (internalFromPart.Resources.Get (resourceInfo.id) == null)
                throw new ArgumentException ("Source part does not contain '" + resource + "'");
            if (internalToPart.Resources.Get (resourceInfo.id) == null)
                throw new ArgumentException ("Destination part cannot store '" + resource + "'");
            // Create the resource transfer
            return new ResourceTransfer (internalFromPart, internalToPart, resourceInfo, maxAmount);
        }

        /// <summary>
        /// Part the resource is being transferred from.
        /// </summary>
        public Parts.Part FromPart { get; private set; }

        /// <summary>
        /// Part the resource is being transferred to.
        /// </summary>
        public Parts.Part ToPart { get; private set; }

        /// <summary>
        /// The resource being transferred.
        /// </summary>
        public string Resource { get; private set; }

        /// <summary>
        /// The total amount to be transferred.
        /// </summary>
        public float TotalAmount { get; private set; }

        /// <summary>
        /// Whether the transfer has completed.
        /// </summary>
        [KRPCProperty]
        public bool Complete { get; private set; }

        /// <summary>
        /// The amount of the resource that has been transferred.
        /// </summary>
        [KRPCProperty]
        public float Amount { get; private set; }

        /// <summary>
        /// Update the transfer. Called once per fixed update.
        /// Transfers at most transferRate of resource from the source part to the
        /// destination part, whilst respecting the amount of resource available in the source and amount
        /// of storage in the destination.
        /// Decrements maxAmount ready for the next update.
        /// </summary>
        internal void Update (float deltaTime)
        {
            if (Complete)
                return;
            var resourceAvailable = (float)internalFromPart.Resources.Get (internalResource.id).amount;
            var storage = internalToPart.Resources.Get (internalResource.id);
            var storageAvailable = (float)(storage.maxAmount - storage.amount);
            var available = Math.Min (resourceAvailable, storageAvailable);
            var amountToTransfer = Math.Min (available, Math.Min (TotalAmount - Amount, transferRate * deltaTime));
            internalFromPart.TransferResource (internalResource.id, -amountToTransfer);
            internalToPart.TransferResource (internalResource.id, amountToTransfer);
            Amount += amountToTransfer;
            Complete |= amountToTransfer < 0.0001f;
        }
    }
}
