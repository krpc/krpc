using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// An antenna. Obtained by calling <see cref="Part.Antenna"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class Antenna : Equatable<Antenna>, IGameObjectState
    {
        ModuleRef transmitterRef;
        ModuleRef deploymentRef;

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
            transmitterRef = ModuleRef.ForType<ModuleDataTransmitter> (internalPart);
            deploymentRef = ModuleRef.ForType<ModuleDeployableAntenna> (internalPart);
        }

        ModuleDataTransmitter InternalTransmitter {
            get { return (ModuleDataTransmitter)transmitterRef.Get (Part.InternalPart); }
        }

        ModuleDeployableAntenna InternalDeployment {
            get { return (ModuleDeployableAntenna)deploymentRef.Find (Part.InternalPart); }
        }

        /// <summary>
        /// The state of the part carrying the antenna, or destroyed once that part loses
        /// the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return transmitterRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Antenna other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && transmitterRef == other.transmitterRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (Part).And (transmitterRef);
        }

        /// <summary>
        /// The part object for this antenna.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// The current state of the antenna.
        /// </summary>
        [KRPCProperty]
        public DeployableState State {
            get {
                if (InternalDeployment == null)
                    return DeployableState.Deployed;
                return InternalDeployment.deployState.ToDeployableState ();
            }
        }

        /// <summary>
        /// Whether the antenna is deployable.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool Deployable {
            get { return InternalDeployment != null; }
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
            get { return State == DeployableState.Deployed; }
            set {
                if (InternalDeployment == null)
                    throw new InvalidOperationException ("Antenna is not deployable");
                if (value)
                    InternalDeployment.Extend ();
                else
                    InternalDeployment.Retract ();
            }
        }

        /// <summary>
        /// Whether data can be transmitted by this antenna.
        /// </summary>
        [KRPCProperty]
        public bool CanTransmit {
            get { return InternalTransmitter.CanTransmit(); }
        }

        /// <summary>
        /// Transmit data.
        /// </summary>
        [KRPCMethod]
        public void Transmit ()
        {
            InternalTransmitter.StartTransmission ();
        }

        /// <summary>
        /// Cancel current transmission of data.
        /// </summary>
        [KRPCMethod]
        public void Cancel ()
        {
            InternalTransmitter.StopTransmission ();
        }

        /// <summary>
        /// Whether partial data transmission is permitted.
        /// </summary>
        [KRPCProperty]
        public bool AllowPartial
        {
            get { return InternalTransmitter.xmitIncomplete; }
            set {
                if (value != InternalTransmitter.xmitIncomplete)
                    InternalTransmitter.TransmitIncompleteToggle ();
            }
        }

        /// <summary>
        /// The power of the antenna.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public double Power {
            get { return InternalTransmitter.CommPower; }
        }

        /// <summary>
        /// Whether the antenna can be combined with other antennae on the vessel
        /// to boost the power.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool Combinable {
            get { return InternalTransmitter.CommCombinable; }
        }

        /// <summary>
        /// Exponent used to calculate the combined power of multiple antennae on a vessel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public double CombinableExponent {
            get { return InternalTransmitter.CommCombinableExponent; }
        }

        /// <summary>
        /// Interval between sending packets in seconds.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float PacketInterval {
            get { return InternalTransmitter.packetInterval; }
        }

        /// <summary>
        /// Amount of data sent per packet in Mits.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float PacketSize {
            get { return InternalTransmitter.packetSize; }
        }

        /// <summary>
        /// Units of electric charge consumed per packet sent.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public double PacketResourceCost {
            get { return InternalTransmitter.packetResourceCost; }
        }
    }
}
