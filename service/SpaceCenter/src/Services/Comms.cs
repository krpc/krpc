using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExternalAPI;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Used to interact with RemoteTech.
    /// Created using a call to <see cref="Vessel.Comms"/>.
    /// </summary>
    /// <remarks>
    /// This class requires <a href="http://forum.kerbalspaceprogram.com/threads/83305">RemoteTech</a> to be installed.
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Comms : Equatable<Comms>
    {
        readonly Guid vesselId;

        internal Comms (global::Vessel vessel)
        {
            if (!RemoteTech.IsAvailable)
                throw new InvalidOperationException ("RemoteTech is not installed");
            vesselId = vessel.id;
        }

        /// <summary>
        /// Check that the comms are for the same vessel.
        /// </summary>
        public override bool Equals (Comms obj)
        {
            return vesselId == obj.vesselId;
        }

        /// <summary>
        /// Hash the comms.
        /// </summary>
        public override int GetHashCode ()
        {
            return vesselId.GetHashCode ();
        }

        /// <summary>
        /// Whether the vessel can be controlled locally.
        /// </summary>
        [KRPCProperty]
        public bool HasLocalControl {
            get { return RemoteTech.HasLocalControl (vesselId); }
        }

        /// <summary>
        /// Whether the vessel has a RemoteTech flight computer on board.
        /// </summary>
        [KRPCProperty]
        public bool HasFlightComputer {
            get { return RemoteTech.HasFlightComputer (vesselId); }
        }

        /// <summary>
        /// Whether the vessel can receive commands from the KSC or a command station.
        /// </summary>
        [KRPCProperty]
        public bool HasConnection {
            get { return RemoteTech.HasAnyConnection (vesselId); }
        }

        /// <summary>
        /// Whether the vessel can transmit science data to a ground station.
        /// </summary>
        [KRPCProperty]
        public bool HasConnectionToGroundStation {
            get { return RemoteTech.HasConnectionToKSC (vesselId); }
        }

        /// <summary>
        /// The signal delay when sending commands to the vessel, in seconds.
        /// </summary>
        [KRPCProperty]
        public double SignalDelay {
            get { return RemoteTech.GetShortestSignalDelay (vesselId); }
        }

        /// <summary>
        /// The signal delay between the vessel and the closest ground station, in seconds.
        /// </summary>
        [KRPCProperty]
        public double SignalDelayToGroundStation {
            get { return RemoteTech.GetSignalDelayToKSC (vesselId); }
        }

        /// <summary>
        /// Returns the signal delay between the current vessel and another vessel, in seconds.
        /// </summary>
        /// <param name="other"></param>
        [KRPCMethod]
        public double SignalDelayToVessel (Vessel other)
        {
            return RemoteTech.GetSignalDelayToSatellite (vesselId, other.Id);
        }
    }
}
