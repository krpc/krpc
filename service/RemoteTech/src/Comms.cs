using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.RemoteTech
{
    /// <summary>
    /// Communications for a vessel.
    /// </summary>
    [KRPCClass (Service = "RemoteTech")]
    public class Comms : Equatable<Comms>
    {
        readonly SpaceCenter.Services.Vessel vessel;
        readonly Guid vesselId;

        internal Comms (SpaceCenter.Services.Vessel innerVessel)
        {
            if (!API.IsAvailable)
                throw new InvalidOperationException ("RemoteTech is not installed");
            vessel = innerVessel;
            vesselId = vessel.InternalVessel.id;
        }

        /// <summary>
        /// Check that the comms are for the same vessel.
        /// </summary>
        public override bool Equals (Comms other)
        {
            return !ReferenceEquals (other, null) && vessel == other.vessel;
        }

        /// <summary>
        /// Hash the comms.
        /// </summary>
        public override int GetHashCode ()
        {
            return vessel.GetHashCode ();
        }

        /// <summary>
        /// Get the vessel.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Vessel Vessel {
            get { return vessel; }
        }

        /// <summary>
        /// Whether the vessel can be controlled locally.
        /// </summary>
        [KRPCProperty]
        public bool HasLocalControl {
            get { return API.HasLocalControl (vesselId); }
        }

        /// <summary>
        /// Whether the vessel has a flight computer on board.
        /// </summary>
        [KRPCProperty]
        public bool HasFlightComputer {
            get { return API.HasFlightComputer (vesselId); }
        }

        /// <summary>
        /// Whether the vessel has any connection.
        /// </summary>
        [KRPCProperty]
        public bool HasConnection {
            get { return API.HasAnyConnection (vesselId); }
        }

        /// <summary>
        /// Whether the vessel has a connection to a ground station.
        /// </summary>
        [KRPCProperty]
        public bool HasConnectionToGroundStation {
            get { return API.HasConnectionToKSC (vesselId); }
        }

        /// <summary>
        /// The shortest signal delay to the vessel, in seconds.
        /// </summary>
        [KRPCProperty]
        public double SignalDelay {
            get { return API.GetShortestSignalDelay (vesselId); }
        }

        /// <summary>
        /// The signal delay between the vessel and the closest ground station, in seconds.
        /// </summary>
        [KRPCProperty]
        public double SignalDelayToGroundStation {
            get { return API.GetSignalDelayToKSC (vesselId); }
        }

        /// <summary>
        /// The signal delay between the this vessel and another vessel, in seconds.
        /// </summary>
        /// <param name="other"></param>
        [KRPCMethod]
        public double SignalDelayToVessel (SpaceCenter.Services.Vessel other)
        {
            return API.GetSignalDelayToSatellite (vesselId, other.Id);
        }

        /// <summary>
        /// The antennas for this vessel.
        /// </summary>
        [KRPCProperty]
        public IList<Antenna> Antennas {
            get { return vessel.Parts.All.Where (Antenna.Is).Select (p => new Antenna (p)).ToList (); }
        }
    }
}
