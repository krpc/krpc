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
    public class Comms : Equatable<Comms>, IGameObjectState
    {
        readonly SpaceCenter.Services.Vessel vessel;

        internal Comms (SpaceCenter.Services.Vessel innerVessel)
        {
            if (!API.IsAvailable)
                throw new InvalidOperationException ("RemoteTech is not installed");
            if (ReferenceEquals (innerVessel, null))
                throw new ArgumentNullException (nameof (innerVessel));
            vessel = innerVessel;
        }

        /// <summary>
        /// The state of the vessel these communications belong to.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return vessel.GameObjectState; }
        }

        // The id of the vessel, which the mod's API takes. Resolved first, so that a vessel the
        // game has dropped raises here and not inside the mod
        Guid VesselId {
            get { return vessel.InternalVessel.id; }
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
            get { return API.HasLocalControl (VesselId); }
        }

        /// <summary>
        /// Whether the vessel has a flight computer on board.
        /// </summary>
        [KRPCProperty]
        public bool HasFlightComputer {
            get { return API.HasFlightComputer (VesselId); }
        }

        /// <summary>
        /// Whether the vessel has any connection.
        /// </summary>
        [KRPCProperty]
        public bool HasConnection {
            get { return API.HasAnyConnection (VesselId); }
        }

        /// <summary>
        /// Whether the vessel has a connection to a ground station.
        /// </summary>
        [KRPCProperty]
        public bool HasConnectionToGroundStation {
            get { return API.HasConnectionToKSC (VesselId); }
        }

        /// <summary>
        /// The shortest signal delay to the vessel, in seconds.
        /// </summary>
        [KRPCProperty]
        public double SignalDelay {
            get { return API.GetShortestSignalDelay (VesselId); }
        }

        /// <summary>
        /// The signal delay between the vessel and the closest ground station, in seconds.
        /// </summary>
        [KRPCProperty]
        public double SignalDelayToGroundStation {
            get { return API.GetSignalDelayToKSC (VesselId); }
        }

        /// <summary>
        /// The signal delay between the this vessel and another vessel, in seconds.
        /// </summary>
        /// <param name="other"></param>
        [KRPCMethod]
        public double SignalDelayToVessel (SpaceCenter.Services.Vessel other)
        {
            if (ReferenceEquals (other, null))
                throw new ArgumentNullException (nameof (other));
            return API.GetSignalDelayToSatellite (VesselId, other.InternalVessel.id);
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
