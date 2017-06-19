using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// An antenna. Obtained by calling <see cref="Part.Antenna"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public class Antenna : Equatable<Antenna>
    {
        readonly ModuleDataTransmitter transmitter;
        readonly ModuleDeployableAntenna deployment;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<ModuleDataTransmitter> ();
        }

        internal Antenna (Part part)
        {
            if (!Is (part))
                throw new ArgumentException ("Part is not an antenna");
            Part = part;
            var internalPart = part.InternalPart;
            transmitter = internalPart.Module<ModuleDataTransmitter> ();
            deployment = internalPart.Module<ModuleDeployableAntenna> ();
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Antenna other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && transmitter == other.transmitter;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode () ^ transmitter.GetHashCode ();
        }

        /// <summary>
        /// The part object for this antenna.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// The current state of the antenna.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public AntennaState State {
            get {
                if (deployment != null) {
                    switch (deployment.deployState) {
                    case ModuleDeployablePart.DeployState.EXTENDED:
                        return AntennaState.Deployed;
                    case ModuleDeployablePart.DeployState.EXTENDING:
                        return AntennaState.Deploying;
                    case ModuleDeployablePart.DeployState.RETRACTED:
                        return AntennaState.Retracted;
                    case ModuleDeployablePart.DeployState.RETRACTING:
                        return AntennaState.Retracting;
                    case ModuleDeployablePart.DeployState.BROKEN:
                        return AntennaState.Broken;
                    default:
                        throw new InvalidOperationException ();
                    }
                }
                return AntennaState.Deployed;
            }
        }

        /// <summary>
        /// Whether the antenna is deployable.
        /// </summary>
        [KRPCProperty]
        public bool Deployable {
            get { return deployment != null; }
        }

        /// <summary>
        /// Whether the antenna is deployed.
        /// </summary>
        /// <remarks>
        /// Fixed antennas are always deployed.
        /// Returns an error if you try to deploy a fixed antenna.
        /// </remarks>
        [KRPCProperty]
        public bool Deployed {
            get { return State == AntennaState.Deployed; }
            set {
                if (deployment == null)
                    throw new InvalidOperationException ("Antenna is not deployable");
                if (value)
                    deployment.Extend ();
                else
                    deployment.Retract ();
            }
        }

        /// <summary>
        /// Whether data can be transmitted by this antenna.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Naming", "UsePreferredTermsRule")]
        public bool CanTransmit {
            get { return transmitter.CanTransmit(); }
        }

        /// <summary>
        /// Transmit data.
        /// </summary>
        [KRPCMethod]
        public void Transmit ()
        {
            transmitter.StartTransmission ();
        }

        /// <summary>
        /// Cancel current transmission of data.
        /// </summary>
        [KRPCMethod]
        public void Cancel ()
        {
            transmitter.StopTransmission ();
        }

        /// <summary>
        /// Whether partial data transmission is permitted.
        /// </summary>
        [KRPCProperty]
        public bool AllowPartial
        {
            get { return transmitter.xmitIncomplete; }
            set {
                if (value != transmitter.xmitIncomplete)
                    transmitter.TransmitIncompleteToggle ();
            }
        }

        /// <summary>
        /// The power of the antenna.
        /// </summary>
        [KRPCProperty]
        public double Power {
            get { return transmitter.CommPower; }
        }

        /// <summary>
        /// Whether the antenna can be combined with other antennae on the vessel to boost the power.
        /// </summary>
        [KRPCProperty]
        public bool Combinable {
            get { return transmitter.CommCombinable; }
        }

        /// <summary>
        /// Exponent used to calculate the combined power of multiple antennae on a vessel.
        /// </summary>
        [KRPCProperty]
        public double CombinableExponent {
            get { return transmitter.CommCombinableExponent; }
        }

        /// <summary>
        /// Interval between sending packets in seconds.
        /// </summary>
        [KRPCProperty]
        public float PacketInterval {
            get { return transmitter.packetInterval; }
        }

        /// <summary>
        /// Amount of data sent per packet in Mits.
        /// </summary>
        [KRPCProperty]
        public float PacketSize {
            get { return transmitter.packetSize; }
        }

        /// <summary>
        /// Units of electric charge consumed per packet sent.
        /// </summary>
        [KRPCProperty]
        public double PacketResourceCost {
            get { return transmitter.packetResourceCost; }
        }
    }
}
