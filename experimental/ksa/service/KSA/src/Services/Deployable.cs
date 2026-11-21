using System;
using System.Reflection;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A part that deploys and retracts, such as a landing leg, a solar panel or a service
    /// module door. Obtained from <see cref="Part.Deployables"/>.
    /// </summary>
    /// <remarks>
    /// Deploying or retracting a part moves its symmetric copies with it, as the part's menu
    /// in the game does.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class Deployable : Equatable<Deployable>, IGameObjectState
    {
        // The game's deploy and retract, which also move the part's symmetric copies
        static readonly MethodInfo applyToMirroredParts = typeof (KeyframeAnimationModule).GetMethod (
            "ApplyToMirroredParts", BindingFlags.NonPublic | BindingFlags.Instance);

        readonly KeyframeAnimationModule module;

        internal Deployable (Part part, KeyframeAnimationModule module)
        {
            Part = part;
            this.module = module;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Deployable other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (module, other.module);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return Part.GameObjectState; }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return module.GetHashCode ();
        }

        /// <summary>
        /// The part the deployable is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        /// <summary>
        /// The state of the part.
        /// </summary>
        [KRPCProperty]
        public DeployableState State {
            get {
                if (!ModuleStateful<KeyframeAnimationModule, KeyframeAnimationModule.State, EmptyStruct, EmptyStruct>.TryGetFrom (
                    Part.InternalVehicle.Parts.States, out var states))
                    throw new InvalidOperationException ("The vehicle has no deployable parts");
                return (DeployableState)states.GetState (module).DeploymentState;
            }
        }

        /// <summary>
        /// Deploy the part. Returns once it has started to move.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the part is broken.
        /// </remarks>
        [KRPCMethod]
        public void Deploy ()
        {
            Move (module.Shared.Duration, KeyframeAnimationModule.DeploymentState.Retracted,
                  DeployableState.Deploying, DeployableState.Deployed);
        }

        /// <summary>
        /// Retract the part. Returns once it has started to move.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the part is broken.
        /// </remarks>
        [KRPCMethod]
        public void Retract ()
        {
            Move (0, KeyframeAnimationModule.DeploymentState.Deployed,
                  DeployableState.Retracting, DeployableState.Retracted);
        }

        // The arguments the part's menu passes to the game
        void Move (float timeGoal, KeyframeAnimationModule.DeploymentState mirrorState,
                   DeployableState moving, DeployableState done)
        {
            if (State == DeployableState.Broken)
                throw new InvalidOperationException ("The part is broken");
            module.TimeGoal = timeGoal;
            applyToMirroredParts.Invoke (module, new object [] { timeGoal, mirrorState });
            Waits.Until (() => State == moving || State == done);
        }
    }
}
