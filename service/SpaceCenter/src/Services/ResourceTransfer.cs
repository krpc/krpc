using System;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Transfer resources between parts.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class ResourceTransfer : Equatable<ResourceTransfer>
    {
        static ulong nextId;
        readonly ulong id;
        readonly Part fromPart;
        readonly Part toPart;
        readonly PartResourceDefinition resource;
        readonly float amount;
        readonly float transferRate;

        ResourceTransfer (Part fromPart, Part toPart, PartResourceDefinition resource, float amount)
        {
            id = nextId;
            nextId++;
            this.fromPart = fromPart;
            this.toPart = toPart;
            this.resource = resource;
            this.amount = amount;
            // Compute the transfer rate (in units/sec) as one tenth the size of the destination tank (determined experimentally from the KSP transfer UI)
            var totalStorage = (float)toPart.Resources.GetAll (resource.id).Sum (r => r.maxAmount);
            transferRate = 0.1f * totalStorage;
            ResourceTransferAddon.AddTransfer (this);
        }

        /// <summary>
        /// Check if resource transfer objects are equal.
        /// </summary>
        public override bool Equals (ResourceTransfer obj)
        {
            return id == obj.id;
        }

        /// <summary>
        /// Hash the resource transfer object.
        /// </summary>
        public override int GetHashCode ()
        {
            return (int)id;
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
        public static ResourceTransfer Start (Parts.Part fromPart, Parts.Part toPart, string resource, float maxAmount)
        {
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
            var resourceInfo = PartResourceLibrary.Instance.resourceDefinitions
                .FirstOrDefault (r => r.name == resource);
            if (resourceInfo == null)
                throw new ArgumentException ("Resource '" + resource + "' does not exist");
            // Check the parts contain the required resource
            if (internalFromPart.Resources.Get (resourceInfo.id) == null)
                throw new ArgumentException ("Source part does not contain '" + resource + "'");
            if (internalToPart.Resources.Get (resourceInfo.id) == null)
                throw new ArgumentException ("Destination part cannot store '" + resource + "'");
            // Create the resource transfer
            return new ResourceTransfer (internalFromPart, internalToPart, resourceInfo, maxAmount);
        }

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
            var resourceAvailable = (float)fromPart.Resources.GetAll (resource.id).Sum (r => r.amount);
            var storageAvailable = (float)toPart.Resources.GetAll (resource.id).Sum (r => r.maxAmount - r.amount);
            var available = Math.Min (resourceAvailable, storageAvailable);
            var amountToTransfer = Math.Min (available, Math.Min (amount - Amount, transferRate * deltaTime));
            fromPart.TransferResource (resource.id, -amountToTransfer);
            toPart.TransferResource (resource.id, amountToTransfer);
            Amount += amountToTransfer;
            Complete |= amountToTransfer < 0.0001f;
        }
    }
}
