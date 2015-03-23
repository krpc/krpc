using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services.Parts
{
    [KRPCEnum (Service = "SpaceCenter")]
    public enum ParachuteState
    {
        Active,
        Cut,
        Deployed,
        SemiDeployed,
        Stowed
    }

    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Parachute : Equatable<Parachute>
    {
        readonly Part part;
        readonly ModuleParachute parachute;

        internal Parachute (Part part)
        {
            this.part = part;
            parachute = part.InternalPart.Module<ModuleParachute> ();
            if (parachute == null)
                throw new ArgumentException ("Part does not have a ModuleParachute PartModule");
        }

        public override bool Equals (Parachute obj)
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

        [KRPCMethod]
        public void Deploy ()
        {
            parachute.Deploy ();
        }

        [KRPCProperty]
        public bool Deployed {
            get { return parachute.deploymentState != ModuleParachute.deploymentStates.STOWED; }
        }

        [KRPCProperty]
        public ParachuteState State {
            get {
                switch (parachute.deploymentState) {
                case ModuleParachute.deploymentStates.ACTIVE:
                    return ParachuteState.Active;
                case ModuleParachute.deploymentStates.CUT:
                    return ParachuteState.Cut;
                case ModuleParachute.deploymentStates.DEPLOYED:
                    return ParachuteState.Deployed;
                case ModuleParachute.deploymentStates.SEMIDEPLOYED:
                    return ParachuteState.SemiDeployed;
                case ModuleParachute.deploymentStates.STOWED:
                    return ParachuteState.Stowed;
                default:
                    throw new ArgumentException ("Unsupported parachute state");
                }
            }
        }

        [KRPCProperty]
        public float DeployAltitude {
            get { return parachute.deployAltitude; }
            set { parachute.deployAltitude = value; }
        }

        [KRPCProperty]
        public float DeployMinPressure {
            get { return parachute.minAirPressureToOpen; }
            set { parachute.minAirPressureToOpen = value; }
        }
    }
}
