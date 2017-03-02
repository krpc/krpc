using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Used to interact with CommNet for a given vessel.
    /// Obtained by calling <see cref="Vessel.Comms"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Comms : Equatable<Comms>
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
        /// </summary>
        public CommNet.CommNetVessel InternalComms {
            get { return InternalVessel.Connection; }
        }

        /// <summary>
        /// Whether the vessel can communicate with KSC.
        /// </summary>
        [KRPCProperty]
        public bool CanCommunicate {
            get { return InternalComms.CanComm; }
        }

        /// <summary>
        /// Whether the vessel can transmit science data to KSC.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Naming", "UsePreferredTermsRule")]
        public bool CanTransmitScience {
            get { return InternalComms.CanScience; }
        }

        /// <summary>
        /// Signal strength to KSC.
        /// </summary>
        [KRPCProperty]
        public double SignalStrength {
            get { return InternalComms.SignalStrength; }
        }

        /// <summary>
        /// Signal delay to KSC in seconds.
        /// </summary>
        [KRPCProperty]
        public double SignalDelay {
            get { return InternalComms.SignalDelay; }
        }

        /// <summary>
        /// The combined power of all active antennae on the vessel.
        /// </summary>
        [KRPCProperty]
        public double Power {
            get {
                var antennas = new Vessel (VesselId).Parts.Antennas;
                var powers = antennas.Select (x => x.Power);
                var maxPower = powers.Max ();
                var totalPower = powers.Sum ();
                var averageWeightedExponent = antennas.Select (x => x.Power * x.CombinableExponent).Sum () / totalPower;
                return maxPower * Math.Pow (totalPower / maxPower, averageWeightedExponent);
            }
        }

        /// <summary>
        /// The communication path used to control the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<CommLink> ControlPath {
            get { return InternalComms.ControlPath.Select (x => new CommLink (x)).ToList (); }
        }
    }
}
