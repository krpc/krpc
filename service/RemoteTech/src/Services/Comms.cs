using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.RemoteTech.Services
{
    /// <summary>
    /// Communications for a vessel.
    /// </summary>
    [KRPCClass (Service = "RemoteTech")]
    public sealed class Comms : Equatable<Comms>
    {
        readonly KRPC.SpaceCenter.Services.Vessel vessel;
        readonly Guid vesselId;

        internal Comms (KRPC.SpaceCenter.Services.Vessel vessel)
        {
            if (!API.IsAvailable)
                throw new InvalidOperationException ("RemoteTech is not installed");
            this.vessel = vessel;
            vesselId = vessel.InternalVessel.id;
        }

        /// <summary>
        /// Check that the comms are for the same vessel.
        /// </summary>
        public override bool Equals (Comms obj)
        {
            return vessel == obj.vessel;
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
        public KRPC.SpaceCenter.Services.Vessel Vessel {
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
        public double SignalDelayToVessel (KRPC.SpaceCenter.Services.Vessel other)
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
