using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

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
        // The game state the transfer was started in. A transfer is let go of when the
        // flight it runs in is left as much as when its client removes it, and the two
        // look the same to the object, so the state having moved on is what tells them
        // apart when there is a client left to say it to.
        readonly uint generation = GameState.Generation;
        // Whether the client has removed the transfer. A finished transfer moves nothing
        // and goes on standing for a pair of parts the game may keep for hours, so the
        // client saying it is done with it is the only thing that can retire the object.
        bool removed;
        bool complete;
        float amount;

        ResourceTransfer (Part fromPart, Part toPart, PartResourceDefinition resource, float maxAmount)
        {
            internalResource = resource;
            FromPart = new Parts.Part (fromPart);
            ToPart = new Parts.Part (toPart);
            Resource = resource.name;
            TotalAmount = maxAmount;
            // Compute the transfer rate (in units/sec) as one tenth the size of the destination tank (determined experimentally from the KSP transfer UI)
            var totalStorage = (float)toPart.Resources.Get (resource.id).maxAmount;
            transferRate = 0.1f * totalStorage;
            ResourceTransferAddon.AddTransfer (this);
        }

        /// <summary>
        /// The state of the transfer. It needs both of the parts it runs between, and takes
        /// the less alive of their two states. A transfer the client has removed is
        /// destroyed whatever the parts' states.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                if (removed)
                    return GameObjectState.Destroyed;
                return FromPart.GameObjectState.LeastAlive (ToPart.GameObjectState);
            }
        }

        /// <summary>
        /// Raise if the transfer has been let go of.
        /// </summary>
        void CheckExists ()
        {
            if (!removed)
                return;
            throw new ObjectDestroyedException (
                generation == GameState.Generation
                ? "The resource transfer no longer exists, as it has been removed."
                : "The resource transfer no longer exists, as the flight it ran in was left.");
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
        /// Use <see cref="Cancel"/> to stop the transfer before it finishes; a canceled
        /// transfer is marked as complete. Use <see cref="Remove"/> to release the memory
        /// the server holds for a transfer that is done with. A transfer is stopped and
        /// removed if the client that started it disconnects, or if the flight it runs in
        /// is left.
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
        /// Whether the transfer has completed. Also becomes true if the transfer is
        /// canceled by calling <see cref="Cancel"/>.
        /// </summary>
        [KRPCProperty]
        public bool Complete {
            get { CheckExists (); return complete; }
        }

        /// <summary>
        /// Cancel the transfer. No more of the resource is moved and
        /// <see cref="Complete"/> becomes true.
        /// </summary>
        [KRPCMethod]
        public void Cancel ()
        {
            CheckExists ();
            complete = true;
        }

        /// <summary>
        /// Remove the transfer, releasing the memory the server holds for it. The
        /// transfer is canceled if it has not finished.
        /// </summary>
        /// <remarks>
        /// Any further use of this object throws an exception. A transfer that is left is
        /// held until the parts it runs between are gone, the flight it runs in is left,
        /// or the client that started it disconnects, whichever comes first; the first of
        /// those may be the rest of the flight away.
        /// </remarks>
        [KRPCMethod]
        public void Remove ()
        {
            CheckExists ();
            // The addon runs the transfer and has nothing left to do for one the client
            // is finished with. Taking it out is also what asks for the sweep that drops
            // it from the object store.
            ResourceTransferAddon.Remove (this);
            Release ();
        }

        /// <summary>
        /// Stop the transfer and let go of it, so that it leaves the object store at the
        /// next sweep. Called for a transfer the client that started it has finished
        /// with, and for one whose client has disconnected.
        /// </summary>
        internal void Release ()
        {
            complete = true;
            removed = true;
        }

        /// <summary>
        /// The amount of the resource that has been transferred.
        /// </summary>
        [KRPCProperty]
        public float Amount {
            get { CheckExists (); return amount; }
        }

        /// <summary>
        /// Update the transfer. Called once per fixed update.
        /// Transfers at most transferRate of resource from the source part to the
        /// destination part, whilst respecting the amount of resource available in the source
        /// and amount of storage in the destination.
        /// Decrements maxAmount ready for the next update.
        /// </summary>
        internal void Update (float deltaTime)
        {
            if (complete)
                return;
            // A transfer runs from the game's fixed update, so it has to decide for itself
            // what to do about a part it can no longer reach rather than raise the error a
            // call would get. A destroyed part ends the transfer, as nothing can move into
            // or out of it again. A part whose vessel the game has unloaded is not gone and
            // is not being simulated either, so the transfer waits for it to come back.
            var fromState = FromPart.GameObjectState;
            var toState = ToPart.GameObjectState;
            if (fromState == GameObjectState.Destroyed || toState == GameObjectState.Destroyed) {
                complete = true;
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
            var amountToTransfer = Math.Min (available, Math.Min (TotalAmount - amount, transferRate * deltaTime));
            fromPart.TransferResource (internalResource.id, -amountToTransfer);
            toPart.TransferResource (internalResource.id, amountToTransfer);
            amount += amountToTransfer;
            complete |= amountToTransfer < 0.0001f;
        }
    }
}
