using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// <see cref="RadiatorState"/>
    /// </summary>
    [KRPCEnum (Service = "SpaceCenter")]
    public enum RadiatorState
    {
        /// <summary>
        /// Radiator is fully extended.
        /// </summary>
        Extended,
        /// <summary>
        /// Radiator is fully retracted.
        /// </summary>
        Retracted,
        /// <summary>
        /// Radiator is being extended.
        /// </summary>
        Extending,
        /// <summary>
        /// Radiator is being retracted.
        /// </summary>
        Retracting,
        /// <summary>
        /// Radiator is being broken.
        /// </summary>
        Broken
    }

    /// <summary>
    /// Obtained by calling <see cref="Part.Radiator"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Radiator : Equatable<Radiator>
    {
        readonly Part part;
        readonly ModuleDeployableRadiator radiator;

        internal Radiator (Part part)
        {
            this.part = part;
            radiator = part.InternalPart.Module<ModuleDeployableRadiator> ();
            if (radiator == null)
                throw new ArgumentException ("Part does not have a ModuleDeployableRadiator PartModule");
        }

        public override bool Equals (Radiator obj)
        {
            return part == obj.part;
        }

        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        /// <summary>
        /// The part object for this radiator.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// Whether the radiator is extended.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get {
                return radiator.panelState == ModuleDeployableRadiator.panelStates.EXTENDED || radiator.panelState == ModuleDeployableRadiator.panelStates.EXTENDING;
            }
            set {
                if (value)
                    radiator.Extend ();
                else
                    radiator.Retract ();
            }
        }

        /// <summary>
        /// The current state of the radiator.
        /// </summary>
        [KRPCProperty]
        public RadiatorState State {
            get {
                switch (radiator.panelState) {
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
                    throw new ArgumentException ("Unsupported solar radiator state");
                }
            }
        }
    }
}
