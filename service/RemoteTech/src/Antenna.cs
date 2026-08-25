using System;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.RemoteTech
{
    /// <summary>
    /// A RemoteTech antenna. Obtained by calling <see cref="Comms.Antennas"/> or <see cref="RemoteTech.Antenna"/>.
    /// </summary>
    [KRPCClass (Service = "RemoteTech")]
    public class Antenna : Equatable<Antenna>, IGameObjectState
    {
        // The module that makes a part an antenna. The mod's API takes the part, so the part
        // identifies the antenna
        const string moduleName = "ModuleRTAntenna";

        readonly SpaceCenter.Services.Parts.Part part;

        internal static bool Is (SpaceCenter.Services.Parts.Part innerPart)
        {
            return innerPart.InternalPart.Modules.Contains (moduleName);
        }

        internal Antenna (SpaceCenter.Services.Parts.Part innerPart)
        {
            part = innerPart;
            if (!Is (part))
                throw new ArgumentException ("Part is not a RemoteTech antenna");
        }

        /// <summary>
        /// Check that the antennas are the same.
        /// </summary>
        public override bool Equals (Antenna other)
        {
            return !ReferenceEquals (other, null) && part == other.part;
        }

        /// <summary>
        /// Hash the antenna.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        /// <summary>
        /// The state of the antenna. It takes the state of its part, and is destroyed once
        /// a live part no longer carries the antenna module.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                var state = part.GameObjectState;
                if (state != GameObjectState.Live)
                    return state;
                return part.InternalPart.Modules.Contains (moduleName)
                    ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        // The part the antenna is on, checked to still be an antenna. Every member that
        // reaches the mod goes through this.
        global::Part InternalPart {
            get {
                var innerPart = part.InternalPart;
                if (!innerPart.Modules.Contains (moduleName))
                    throw new ObjectDestroyedException (
                        "The antenna no longer exists, as its part no longer has one.");
                return innerPart;
            }
        }

        /// <summary>
        /// Get the part containing this antenna.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Parts.Part Part {
            get { return part; }
        }

        /// <summary>
        /// Whether the antenna has a connection.
        /// </summary>
        [KRPCProperty]
        public bool HasConnection {
            get { return API.AntennaHasConnection (InternalPart); }
        }

        /// <summary>
        /// The object that the antenna is targetting.
        /// This property can be used to set the target to <see cref="Target.None"/> or <see cref="Target.ActiveVessel"/>.
        /// To set the target to a celestial body, ground station or vessel see <see cref="TargetBody"/>,
        /// <see cref="TargetGroundStation"/> and <see cref="TargetVessel"/>.
        /// </summary>
        [KRPCProperty]
        public Target Target {
            get {
                var target = API.GetAntennaTarget (InternalPart);
                if (target == API.GetNoTargetGuid ())
                    return Target.None;
                if (target == API.GetActiveVesselGuid ())
                    return Target.ActiveVessel;
                if (RemoteTech.GroundStationIds.ContainsKey (target))
                    return Target.GroundStation;
                if (FlightGlobals.Vessels.Any (x => x.id == target))
                    return Target.Vessel;
                return Target.CelestialBody;
            }
            set {
                if (value == Target.ActiveVessel)
                    API.SetAntennaTarget (InternalPart, API.GetActiveVesselGuid ());
                else if (value == Target.None)
                    API.SetAntennaTarget (InternalPart, API.GetNoTargetGuid ());
                else
                    throw new ArgumentException ("Failed to set target");
            }
        }

        /// <summary>
        /// The celestial body the antenna is targetting.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.CelestialBody TargetBody {
            get {
                if (Target != Target.CelestialBody)
                    throw new InvalidOperationException ("Antenna is not targetting a celestial body.");
                return new SpaceCenter.Services.CelestialBody (RemoteTech.CelestialBodyIds [API.GetAntennaTarget (InternalPart)]);
            }
            set {
                API.SetAntennaTarget (InternalPart, API.GetCelestialBodyGuid (value.InternalBody));
            }
        }

        /// <summary>
        /// The ground station the antenna is targetting.
        /// </summary>
        [KRPCProperty]
        public string TargetGroundStation {
            get {
                if (Target != Target.GroundStation)
                    throw new InvalidOperationException ("Antenna is not targetting a ground station.");
                return RemoteTech.GroundStationIds [API.GetAntennaTarget (InternalPart)];
            }
            set {
                if (RemoteTech.GroundStationIds.Values.All (x => x != value))
                    throw new ArgumentException ("Ground station does not exist.");
                API.SetAntennaTarget (InternalPart, API.GetGroundStationGuid (value));
            }
        }

        /// <summary>
        /// The vessel the antenna is targetting.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Vessel TargetVessel {
            get {
                if (Target != Target.Vessel)
                    throw new InvalidOperationException ("Antenna is not targetting a vessel.");
                return new SpaceCenter.Services.Vessel (API.GetAntennaTarget (InternalPart));
            }
            set {
                API.SetAntennaTarget (InternalPart, value.InternalVessel.id);
            }
        }
    }
}
