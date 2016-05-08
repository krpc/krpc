using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.Radiator"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Radiator : Equatable<Radiator>
    {
        readonly Part part;
        readonly ModuleActiveRadiator activeRadiator;
        readonly ModuleDeployableRadiator deployableRadiator;

        internal static bool Is (Part part)
        {
            return
            part.InternalPart.HasModule<ModuleActiveRadiator> () ||
            part.InternalPart.HasModule<ModuleDeployableRadiator> ();
        }

        internal Radiator (Part part)
        {
            this.part = part;
            activeRadiator = part.InternalPart.Module<ModuleActiveRadiator> ();
            deployableRadiator = part.InternalPart.Module<ModuleDeployableRadiator> ();
            if (activeRadiator == null && deployableRadiator == null)
                throw new ArgumentException ("Part is not a radiator");
        }

        /// <summary>
        /// Check if radiators are equal.
        /// </summary>
        public override bool Equals (Radiator obj)
        {
            return part == obj.part && activeRadiator == obj.activeRadiator && deployableRadiator == obj.deployableRadiator;
        }

        /// <summary>
        /// Hash the radiator.
        /// </summary>
        public override int GetHashCode ()
        {
            int hash = part.GetHashCode ();
            if (activeRadiator != null)
                hash ^= activeRadiator.GetHashCode ();
            if (deployableRadiator != null)
                hash ^= deployableRadiator.GetHashCode ();
            return hash;
        }

        /// <summary>
        /// The part object for this radiator.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// Whether the radiator is deployable.
        /// </summary>
        [KRPCProperty]
        public bool Deployable {
            get { return deployableRadiator != null; }
        }

        /// <summary>
        /// For a deployable radiator, <c>true</c> if the radiator is extended.
        /// If the radiator is not deployable, this is always <c>true</c>.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get {
                if (!Deployable)
                    return true;
                return deployableRadiator.panelState == ModuleDeployableRadiator.panelStates.EXTENDED ||
                deployableRadiator.panelState == ModuleDeployableRadiator.panelStates.EXTENDING;
            }
            set {
                if (!Deployable)
                    throw new InvalidOperationException ("Radiator is not deployable");
                if (value)
                    deployableRadiator.Extend ();
                else
                    deployableRadiator.Retract ();
            }
        }

        /// <summary>
        /// The current state of the radiator.
        /// </summary>
        /// <remarks>
        /// A fixed radiator is always <see cref="RadiatorState.Extended" />.
        /// </remarks>
        [KRPCProperty]
        public RadiatorState State {
            get {
                if (!Deployable)
                    return RadiatorState.Extended;
                switch (deployableRadiator.panelState) {
                case ModuleDeployableRadiator.panelStates.EXTENDED:
                    return RadiatorState.Extended;
                case ModuleDeployableRadiator.panelStates.RETRACTED:
                    return RadiatorState.Retracted;
                case ModuleDeployableRadiator.panelStates.EXTENDING:
                    return RadiatorState.Extending;
                case ModuleDeployableRadiator.panelStates.RETRACTING:
                    return RadiatorState.Retracting;
                case ModuleDeployableRadiator.panelStates.BROKEN:
                    return RadiatorState.Broken;
                default:
                    throw new ArgumentException ("Unsupported radiator state");
                }
            }
        }
    }
}
