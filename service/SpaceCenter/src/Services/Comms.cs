using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Used to interact with CommNet for a given vessel.
    /// Obtained by calling <see cref="Vessel.Comms"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class Comms : Equatable<Comms>, IGameObjectState
    {
        /// <summary>
        /// Construct from a KSP vessel object.
        /// </summary>
        public Comms (Vessel vessel)
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            VesselId = vessel.Id;
        }

        /// <summary>
        /// Construct from a KSP vessel id.
        /// </summary>
        public Comms (Guid vesselId)
        {
            VesselId = vesselId;
        }

        /// <summary>
        /// What the game holds for the vessel this belongs to.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return FlightGlobalsExtensions.VesselState (VesselId); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Comms other)
        {
            return !ReferenceEquals (other, null) && VesselId == other.VesselId;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return VesselId.GetHashCode ();
        }

        /// <summary>
        /// The KSP vessel id.
        /// </summary>
        public Guid VesselId { get; private set; }

        /// <summary>
        /// The KSP vessel object.
        /// </summary>
        public global::Vessel InternalVessel {
            get { return FlightGlobalsExtensions.GetVesselById (VesselId); }
        }

        /// <summary>
        /// The KSP vessel communication object.
        /// Null for a vessel that is not part of the network, see <see cref="InNetwork"/>.
        /// </summary>
        public CommNet.CommNetVessel InternalComms {
            get { return InternalVessel.Connection; }
        }

        /// <summary>
        /// Whether the vessel is part of the communication network at all, and so has anything
        /// to report about its communications. KSP gives no network node to a vessel whose type
        /// is debris, a space object, unknown, a flag or a deployed science part. Debris is the
        /// type it gives a vessel that is left with no command module, by a decoupler firing for
        /// example.
        /// </summary>
        bool InNetwork {
            get { return InternalComms != null; }
        }

        /// <summary>
        /// Whether the vessel can communicate with KSC.
        /// </summary>
        [KRPCProperty]
        public bool CanCommunicate {
            get { return InNetwork && InternalComms.CanComm; }
        }

        /// <summary>
        /// Whether the vessel can transmit science data to KSC.
        /// </summary>
        [KRPCProperty]
        public bool CanTransmitScience {
            get { return InNetwork && InternalComms.CanScience; }
        }

        /// <summary>
        /// Signal strength to KSC.
        /// </summary>
        [KRPCProperty]
        public double SignalStrength {
            get { return InNetwork ? InternalComms.SignalStrength : 0; }
        }

        /// <summary>
        /// Signal delay to KSC in seconds.
        /// </summary>
        [KRPCProperty]
        public double SignalDelay {
            get { return InNetwork ? InternalComms.SignalDelay : 0; }
        }

        /// <summary>
        /// The combined power of all active antennae on the vessel.
        /// </summary>
        [KRPCProperty]
        public double Power {
            get {
                var antennas = new Vessel (VesselId).Parts.Antennas;
                var powers = antennas.Select (x => x.Power).ToList ();
                var totalPower = powers.Sum ();
                // The combination below divides by the total power, and by the largest of the
                // powers, so both have to be above zero. The total is zero for a vessel that
                // carries no antennae, and for one whose antennae all supply no power.
                if (totalPower <= 0)
                    return 0;
                var maxPower = powers.Max ();
                var averageWeightedExponent = antennas.Select (x => x.Power * x.CombinableExponent).Sum () / totalPower;
                return maxPower * Math.Pow (totalPower / maxPower, averageWeightedExponent);
            }
        }

        /// <summary>
        /// The communication path used to control the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<CommLink> ControlPath {
            get {
                if (!InNetwork)
                    return new List<CommLink> ();
                return InternalComms.ControlPath.Select (x => new CommLink (x)).ToList ();
            }
        }
    }
}
