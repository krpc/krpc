using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services.Parts
{
    [KRPCEnum (Service = "SpaceCenter")]
    public enum RadiatorState
    {
        Extended,
        Retracted,
        Extending,
        Retracting,
        Broken
    }

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

        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

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
