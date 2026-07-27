using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Transfer resources between parts.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class ResourceTransfer : IGameObjectState
    {
        // The resource's definition comes from the game's part database rather than from any
        // craft, so unlike the parts it is not something the game tears down.
        readonly PartResourceDefinition internalResource;
        readonly float transferRate;

        ResourceTransfer (Part fromPart, Part toPart, PartResourceDefinition resource, float amount)
        {
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
        /// What the game holds for the transfer, which needs both of the parts it runs
        /// between and so is as alive as the less alive of them.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return FromPart.GameObjectState.LeastAlive (ToPart.GameObjectState); }
        }

        /// <summary>
        /// Start transferring a resource transfer between a pair of parts. The transfer will move
        /// at most <paramref name="maxAmount"/> units of the resource, depending on how much of
        /// the resource is available in the source part and how much storage is available in the
        /// destination part.
        /// Use <see cref="Complete"/> to check if the transfer is complete.
        /// Use <see cref="Amount"/> to see how much of the resource has been transferred.
        /// </summary>
        /// <param name="fromPart">The part to transfer to.</param>
        /// <param name="toPart">The part to transfer from.</param>
        /// <param name="resource">The name of the resource to transfer.</param>
        /// <param name="maxAmount">The maximum amount of resource to transfer.</param>
        /// <remarks>
        /// Use <see cref="Cancel"/> to stop the transfer before it finishes. The transfer is
        /// also canceled if the client that started it disconnects. A canceled transfer is
        /// marked as complete.
        /// </remarks>
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
        /// Whether the transfer has completed. Also becomes true if the transfer is canceled,
        /// either by calling <see cref="Cancel"/> or because the client that started it
        /// disconnected.
        /// </summary>
        [KRPCProperty]
        public bool Complete { get; private set; }

        /// <summary>
        /// Cancel the transfer. No more of the resource is moved and
        /// <see cref="Complete"/> becomes true.
        /// </summary>
        [KRPCMethod]
        public void Cancel ()
        {
            Complete = true;
        }

        /// <summary>
        /// The amount of the resource that has been transferred.
        /// </summary>
        [KRPCProperty]
        public float Amount { get; private set; }

        /// <summary>
        /// Update the transfer. Called once per fixed update.
        /// Transfers at most transferRate of resource from the source part to the
        /// destination part, whilst respecting the amount of resource available in the source
        /// and amount of storage in the destination.
        /// Decrements maxAmount ready for the next update.
        /// </summary>
        internal void Update (float deltaTime)
        {
            if (Complete)
                return;
            // A transfer runs from the game's fixed update, so it has to decide for itself
            // what to do about a part it can no longer reach rather than raise the error a
            // call would get. A destroyed part ends the transfer, as nothing can move into
            // or out of it again. A part whose vessel the game has unloaded is not gone and
            // is not being simulated either, so the transfer waits for it to come back.
            var fromState = FromPart.GameObjectState;
            var toState = ToPart.GameObjectState;
            if (fromState == GameObjectState.Destroyed || toState == GameObjectState.Destroyed) {
                Cancel ();
                return;
            }
            if (fromState != GameObjectState.Live || toState != GameObjectState.Live)
                return;
            var fromPart = FromPart.InternalPart;
            var toPart = ToPart.InternalPart;
            var resourceAvailable = (float)fromPart.Resources.Get (internalResource.id).amount;
            var storage = toPart.Resources.Get (internalResource.id);
            var storageAvailable = (float)(storage.maxAmount - storage.amount);
            var available = Math.Min (resourceAvailable, storageAvailable);
            var amountToTransfer = Math.Min (available, Math.Min (TotalAmount - Amount, transferRate * deltaTime));
            fromPart.TransferResource (internalResource.id, -amountToTransfer);
            toPart.TransferResource (internalResource.id, amountToTransfer);
            Amount += amountToTransfer;
            Complete |= amountToTransfer < 0.0001f;
        }
    }
}
