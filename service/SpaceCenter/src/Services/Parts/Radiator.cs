using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A radiator. Obtained by calling <see cref="Part.Radiator"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class Radiator : Equatable<Radiator>, IGameObjectState
    {
        ModuleRef activeRadiatorRef;
        ModuleRef deployableRadiatorRef;

        internal static bool Is (Part part)
        {
            var internalPart = part.InternalPart;
            return
            internalPart.HasModule<ModuleActiveRadiator> () ||
            internalPart.HasModule<ModuleDeployableRadiator> ();
        }

        internal Radiator (Part part)
        {
            Part = part;
            var internalPart = part.InternalPart;
            activeRadiatorRef = ModuleRef.ForType<ModuleActiveRadiator> (internalPart);
            deployableRadiatorRef = ModuleRef.ForType<ModuleDeployableRadiator> (internalPart);
            if (activeRadiatorRef.Find (internalPart) == null &&
                deployableRadiatorRef.Find (internalPart) == null)
                throw new ArgumentException ("Part is not a radiator");
        }

        ModuleActiveRadiator InternalActiveRadiator {
            get { return (ModuleActiveRadiator)activeRadiatorRef.Find (Part.InternalPart); }
        }

        ModuleDeployableRadiator InternalDeployableRadiator {
            get { return (ModuleDeployableRadiator)deployableRadiatorRef.Find (Part.InternalPart); }
        }

        /// <summary>
        /// What the game holds for the radiator. A part carries an active radiator
        /// module, a deployable one, or both, and either is enough for the radiator, so
        /// it is as alive as the more alive of them.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return activeRadiatorRef.StateOn (Part).MostAlive (deployableRadiatorRef.StateOn (Part)); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Radiator other)
        {
            return
            !ReferenceEquals (other, null) &&
            Part == other.Part &&
            activeRadiatorRef == other.activeRadiatorRef &&
            deployableRadiatorRef == other.deployableRadiatorRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (Part).And (activeRadiatorRef).And (deployableRadiatorRef);
        }

        /// <summary>
        /// The part object for this radiator.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the radiator is deployable.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool Deployable {
            get { return InternalDeployableRadiator != null; }
        }

        /// <summary>
        /// For a deployable radiator, <c>true</c> if the radiator is extended.
        /// If the radiator is not deployable, this is always <c>true</c>.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get {
                return
                !Deployable ||
                InternalDeployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDED ||
                InternalDeployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDING;
            }
            set {
                if (!Deployable)
                    throw new InvalidOperationException ("Radiator is not deployable");
                if (value)
                    InternalDeployableRadiator.Extend ();
                else
                    InternalDeployableRadiator.Retract ();
            }
        }

        /// <summary>
        /// The current state of the radiator.
        /// </summary>
        /// <remarks>
        /// A fixed radiator is always <see cref="DeployableState.Deployed" />.
        /// </remarks>
        [KRPCProperty]
        public DeployableState State {
            get {
                if (!Deployable)
                    return DeployableState.Deployed;
                return InternalDeployableRadiator.deployState.ToDeployableState ();
            }
        }
    }
}
