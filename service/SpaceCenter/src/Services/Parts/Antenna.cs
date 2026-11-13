using System;
using System.Globalization;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using RealAntennasAPI = KRPC.SpaceCenter.ExternalAPI.RealAntennas;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;

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

        /// <summary>
        /// Construct from one of the part's transmitter modules, for a part with several.
        /// </summary>
        internal Antenna (Part part, ModuleDataTransmitter transmitter)
        {
            if (ReferenceEquals (transmitter, null))
                throw new ArgumentNullException (nameof (transmitter));
            Part = part;
            transmitterRef = ModuleRef.ForModule (transmitter);
            deploymentRef = ModuleRef.ForType<ModuleDeployableAntenna> (part.InternalPart);
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
        /// Throws <see cref="InvalidOperationException"/> if set on a fixed antenna.
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
        /// <remarks>
        /// Not used by RealAntennas, which replaces the stock range model.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public double Power {
            get { return InternalTransmitter.CommPower; }
        }

        /// <summary>
        /// Whether the antenna can be combined with other antennae on the vessel
        /// to boost the power.
        /// </summary>
        /// <remarks>
        /// Not used by RealAntennas, which replaces the stock range model.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool Combinable {
            get { return InternalTransmitter.CommCombinable; }
        }

        /// <summary>
        /// Exponent used to calculate the combined power of multiple antennae on a vessel.
        /// </summary>
        /// <remarks>
        /// Not used by RealAntennas, which replaces the stock range model.
        /// </remarks>
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

        /// <summary>
        /// The RealAntennas part module. Throws if RealAntennas is not available.
        /// </summary>
        PartModule RealAntennaModule {
            get {
                var module = InternalTransmitter;
                if (!RealAntennasAPI.IsModule (module))
                    throw new InvalidOperationException ("RealAntennas is not available");
                return module;
            }
        }

        object RealAntenna {
            get { return RealAntennasAPI.Antenna (RealAntennaModule); }
        }

        static void CheckEditor (string member)
        {
            if (!HighLogic.LoadedSceneIsEditor)
                throw new InvalidOperationException (member + " can only be set in the editor");
        }

        /// <summary>
        /// The field of the RealAntennas module, set through the right-click menu path so the
        /// module recalculates its link budget, mass and cost.
        /// </summary>
        PartField MenuField (string name)
        {
            var module = RealAntennaModule;
            return new PartField (new Module (Part, module), module.Fields [name]);
        }

        /// <summary>
        /// The gain of the antenna, in <unit>dBi</unit>.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float Gain {
            get { return RealAntennasAPI.Gain (RealAntenna); }
        }

        /// <summary>
        /// Returns the transmit power of the antenna, in <unit>dBm</unit>, and sets it in the editor.
        /// Throws an exception if set in flight, or outside zero to <see cref="MaxTransmitPower"/>.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float TransmitPower {
            get { return RealAntennasAPI.TxPower (RealAntenna); }
            set {
                CheckEditor ("Transmit power");
                if (value < 0 || value > MaxTransmitPower)
                    throw new ArgumentOutOfRangeException (nameof (value), "Transmit power must be between 0 and " + MaxTransmitPower + " dBm");
                MenuField ("TxPower").FloatValue = value;
            }
        }

        /// <summary>
        /// The largest transmit power the antenna can be set to, in <unit>dBm</unit>.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float MaxTransmitPower {
            get { return RealAntennasAPI.MaxTxPower (RealAntennaModule); }
        }

        /// <summary>
        /// Returns the tech level of the antenna, and sets it in the editor. Throws an
        /// exception if set in flight, or outside zero to <see cref="MaxTechLevel"/>.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public int TechLevel {
            get { return RealAntennasAPI.TechLevel (RealAntenna); }
            set {
                CheckEditor ("Tech level");
                if (value < 0 || value > MaxTechLevel)
                    throw new ArgumentOutOfRangeException (nameof (value), "Tech level must be between 0 and " + MaxTechLevel);
                MenuField ("TechLevel").FloatValue = value;
            }
        }

        /// <summary>
        /// The highest tech level the antenna can be set to.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public int MaxTechLevel {
            get { return RealAntennasAPI.MaxTechLevel (RealAntennaModule); }
        }

        /// <summary>
        /// The name of the radio frequency band the antenna uses, e.g. <c>"S"</c>.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public string Band {
            get { return RealAntennasAPI.Band (RealAntenna); }
        }

        /// <summary>
        /// The frequency of the antenna's band, in Hz.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public double Frequency {
            get { return RealAntennasAPI.Frequency (RealAntenna); }
        }

        /// <summary>
        /// The highest data rate the antenna supports, in bits per second.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public double DataRate {
            get { return RealAntennasAPI.DataRate (RealAntenna); }
        }

        /// <summary>
        /// The half-power beamwidth of the antenna, in degrees.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public double Beamwidth {
            get { return RealAntennasAPI.Beamwidth (RealAntenna); }
        }

        /// <summary>
        /// The power the antenna draws while idle, in watts.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public double IdlePowerDraw {
            get { return RealAntennasAPI.IdlePowerDraw (RealAntenna) * 1000d; }
        }

        /// <summary>
        /// The power the antenna draws while transmitting, in watts. This includes
        /// <see cref="IdlePowerDraw"/>.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public double ActivePowerDraw {
            get {
                var antenna = RealAntenna;
                return RealAntennasAPI.IdlePowerDraw (antenna) * 1000d + RealAntennasAPI.TransmitPowerDraw (antenna) / 1000d;
            }
        }

        /// <summary>
        /// Returns whether the antenna is enabled, and sets it in the editor. Throws an
        /// exception if set in flight.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas. A disabled antenna does not communicate and draws no power.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool Enabled {
            get { return RealAntennasAPI.Enabled (RealAntennaModule); }
            set {
                CheckEditor ("Enabled");
                MenuField ("_enabled").BoolValue = value;
            }
        }

        /// <summary>
        /// Disable the antenna for the rest of the flight. It then draws no power.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCMethod]
        public void DisablePermanently ()
        {
            RealAntennasAPI.PermanentShutdown (RealAntennaModule);
        }

        // Antenna targeting. RealAntennas keeps a dish's target in the TARGET node format it
        // saves, with a mode name and the mode's values. A body's center is a position at the
        // body's center.

        const string TargetVesselMode = "Vessel";
        const string TargetPositionMode = "BodyLatLonAlt";
        const string TargetAzElMode = "AzEl";
        const string TargetOrbitRelativeMode = "OrbitRelative";

        /// <summary>
        /// Whether the antenna can be pointed at a target. Omnidirectional antennas
        /// cannot.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty]
        public bool CanTarget {
            get { return RealAntennasAPI.IsDish (RealAntenna); }
        }

        global::ConfigNode TargetNode {
            get { return RealAntennasAPI.Target (RealAntenna); }
        }

        static global::CelestialBody TargetNodeBody (global::ConfigNode node)
        {
            return node.GetValue ("name") == TargetPositionMode ? FlightGlobals.GetBodyByName (node.GetValue ("bodyName")) : null;
        }

        /// <summary>
        /// The kind of target the antenna points at.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty]
        public AntennaTargetMode TargetMode {
            get {
                var node = TargetNode;
                if (node == null)
                    return AntennaTargetMode.None;
                switch (node.GetValue ("name")) {
                case TargetVesselMode:
                    return AntennaTargetMode.Vessel;
                case TargetAzElMode:
                    return AntennaTargetMode.AzimuthElevation;
                case TargetOrbitRelativeMode:
                    return AntennaTargetMode.OrbitRelative;
                case TargetPositionMode:
                    var body = TargetNodeBody (node);
                    var position = global::ConfigNode.ParseVector3 (node.GetValue ("latLonAlt"));
                    // The saved altitude is a float, so compare it to the radius to within its precision
                    if (body != null && position.x == 0 && position.y == 0 &&
                        Math.Abs (position.z + body.Radius) < Math.Max (1, body.Radius * 1e-6))
                        return AntennaTargetMode.Body;
                    return AntennaTargetMode.Position;
                default:
                    return AntennaTargetMode.None;
                }
            }
        }

        /// <summary>
        /// Returns the vessel the antenna points at, and sets the vessel to point at.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas. <c>null</c> if the antenna does not point at a vessel. Throws an
        /// exception if set to <c>null</c>, or if the antenna cannot be pointed
        /// (see <see cref="CanTarget"/>).
        /// </remarks>
        [KRPCProperty (Nullable = true)]
        public Vessel TargetVessel {
            get {
                var node = TargetNode;
                if (node == null || node.GetValue ("name") != TargetVesselMode)
                    return null;
                var vessel = FlightGlobals.FindVessel (new Guid (node.GetValue ("vesselId")));
                return vessel == null ? null : new Vessel (vessel);
            }
            set {
                if (ReferenceEquals (value, null))
                    throw new ArgumentNullException (nameof (value));
                SetTarget (TargetVesselMode, node => node.AddValue ("vesselId", value.Id.ToString ()));
            }
        }

        /// <summary>
        /// Returns the body the antenna points at, and sets the body to point at the center of.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas. Also the body of a position the antenna points at, or
        /// <c>null</c> if it points at neither. Throws an exception if set to <c>null</c>, or if
        /// the antenna cannot be pointed (see <see cref="CanTarget"/>).
        /// </remarks>
        [KRPCProperty (Nullable = true)]
        public CelestialBody TargetBody {
            get {
                var node = TargetNode;
                var body = node == null ? null : TargetNodeBody (node);
                return body == null ? null : new CelestialBody (body);
            }
            set {
                if (ReferenceEquals (value, null))
                    throw new ArgumentNullException (nameof (value));
                SetTargetPosition (value, 0, 0, -value.InternalBody.Radius);
            }
        }

        /// <summary>
        /// The direction the antenna points in, in the given reference frame. Throws an
        /// exception if the antenna has no target (see <see cref="TargetMode"/>).
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// direction is in.</param>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCMethod]
        public Tuple3 TargetDirection (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            if (TargetNode == null)
                throw new InvalidOperationException ("Antenna has no target");
            var direction = (Vector3d)RealAntennasAPI.ToTarget (RealAntenna);
            return referenceFrame.DirectionFromWorldSpace (direction.normalized).ToTuple ();
        }

        void CheckCanTarget ()
        {
            if (!CanTarget)
                throw new InvalidOperationException ("Antenna cannot be pointed at a target");
        }

        void SetTarget (string mode, Action<global::ConfigNode> values)
        {
            CheckCanTarget ();
            var node = new global::ConfigNode ("TARGET");
            node.AddValue ("name", mode);
            values (node);
            RealAntennasAPI.SetTarget (RealAntenna, node);
        }

        /// <summary>
        /// Point the antenna at a position on or above a body.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="latitude">The latitude, in degrees.</param>
        /// <param name="longitude">The longitude, in degrees.</param>
        /// <param name="altitude">The altitude above sea level, in meters.</param>
        /// <remarks>
        /// Requires RealAntennas. Throws an exception if the antenna cannot be pointed
        /// (see <see cref="CanTarget"/>).
        /// </remarks>
        [KRPCMethod]
        public void SetTargetPosition (CelestialBody body, double latitude, double longitude, double altitude)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            SetTarget (TargetPositionMode, node => {
                node.AddValue ("bodyName", body.InternalBody.bodyName);
                node.AddValue ("latLonAlt", new Vector3 ((float)latitude, (float)longitude, (float)altitude));
            });
        }

        /// <summary>
        /// Point the antenna in a direction relative to the vessel's position on the surface.
        /// </summary>
        /// <param name="azimuth">The azimuth, in degrees clockwise from north, from 0 to 360.</param>
        /// <param name="elevation">The elevation above the horizon, in degrees, from -90 to 90.</param>
        /// <remarks>
        /// Requires RealAntennas. Throws an exception if the antenna cannot be pointed
        /// (see <see cref="CanTarget"/>).
        /// </remarks>
        [KRPCMethod]
        public void SetTargetAzimuthElevation (double azimuth, double elevation)
        {
            if (azimuth < 0 || azimuth > 360)
                throw new ArgumentOutOfRangeException (nameof (azimuth), "Azimuth must be between 0 and 360 degrees");
            CheckElevation (elevation);
            SetTarget (TargetAzElMode, node => {
                node.AddValue ("vesselId", Part.InternalPart.vessel.id.ToString ());
                node.AddValue ("azimuth", azimuth.ToString (CultureInfo.InvariantCulture));
                node.AddValue ("elevation", elevation.ToString (CultureInfo.InvariantCulture));
            });
        }

        /// <summary>
        /// Point the antenna in a direction relative to the vessel's orbital velocity.
        /// </summary>
        /// <param name="deflection">The angle from the orbital velocity in the orbital plane,
        /// in degrees, from -180 to 180.</param>
        /// <param name="elevation">The angle out of the orbital plane, in degrees, from -90 to 90.</param>
        /// <remarks>
        /// Requires RealAntennas. Throws an exception if the antenna cannot be pointed
        /// (see <see cref="CanTarget"/>).
        /// </remarks>
        [KRPCMethod]
        public void SetTargetOrbitRelative (double deflection, double elevation)
        {
            if (deflection < -180 || deflection > 180)
                throw new ArgumentOutOfRangeException (nameof (deflection), "Deflection must be between -180 and 180 degrees");
            CheckElevation (elevation);
            SetTarget (TargetOrbitRelativeMode, node => {
                node.AddValue ("vesselId", Part.InternalPart.vessel.id.ToString ());
                node.AddValue ("forward", deflection.ToString (CultureInfo.InvariantCulture));
                node.AddValue ("elevation", elevation.ToString (CultureInfo.InvariantCulture));
            });
        }

        static void CheckElevation (double elevation)
        {
            if (elevation < -90 || elevation > 90)
                throw new ArgumentOutOfRangeException (nameof (elevation), "Elevation must be between -90 and 90 degrees");
        }
    }
}
