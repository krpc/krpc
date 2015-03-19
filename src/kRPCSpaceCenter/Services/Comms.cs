using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExternalAPI;

namespace KRPCSpaceCenter.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Comms : Equatable<Comms>
    {
        readonly global::Vessel vessel;

        internal Comms (global::Vessel vessel)
        {
            if (!RemoteTech.IsAvailable)
                throw new RPCException ("RemoteTech is not installed");
            this.vessel = vessel;
        }

        public override bool Equals (Comms obj)
        {
            return vessel == obj.vessel;
        }

        public override int GetHashCode ()
        {
            return vessel.GetHashCode ();
        }

        [KRPCProperty]
        public bool HasFlightComputer {
            get { return RemoteTech.HasFlightComputer (vessel.id); }
        }

        [KRPCProperty]
        public bool HasConnection {
            get { return RemoteTech.HasAnyConnection (vessel.id); }
        }

        [KRPCProperty]
        public bool HasConnectionToGroundStation {
            get { return RemoteTech.HasConnectionToKSC (vessel.id); }
        }

        [KRPCProperty]
        public double SignalDelay {
            get { return RemoteTech.GetShortestSignalDelay (vessel.id); }
        }

        [KRPCProperty]
        public double SignalDelayToGroundStation {
            get { return RemoteTech.GetSignalDelayToKSC (vessel.id); }
        }

        [KRPCMethod]
        public double SignalDelayToVessel (Vessel other) {
            return RemoteTech.GetSignalDelayToSatellite (vessel.id, other.InternalVessel.id);
        }

    }
}