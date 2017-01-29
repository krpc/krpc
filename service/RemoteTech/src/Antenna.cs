using System;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.RemoteTech
{
    /// <summary>
    /// A RemoteTech antenna. Obtained by calling <see cref="Comms.Antennas"/> or <see cref="RemoteTech.Antenna"/>.
    /// </summary>
    [KRPCClass (Service = "RemoteTech")]
    public class Antenna : Equatable<Antenna>
    {
        readonly SpaceCenter.Services.Parts.Part part;

        internal static bool Is (SpaceCenter.Services.Parts.Part innerPart)
        {
            return innerPart.InternalPart.Modules.Contains ("ModuleRTAntenna");
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
            get { return API.AntennaHasConnection (part.InternalPart); }
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
                var target = API.GetAntennaTarget (part.InternalPart);
                if (target == API.GetNoTargetGuid ())
                    return Target.None;
                else if (target == API.GetActiveVesselGuid ())
                    return Target.ActiveVessel;
                else if (RemoteTech.GroundStationIds.ContainsKey (target))
                    return Target.GroundStation;
                else if (FlightGlobals.Vessels.Any (x => x.id == target))
                    return Target.Vessel;
                else
                    return Target.CelestialBody;
            }
            set {
                if (value == Target.ActiveVessel)
                    API.SetAntennaTarget (part.InternalPart, API.GetActiveVesselGuid ());
                else if (value == Target.None)
                    API.SetAntennaTarget (part.InternalPart, API.GetNoTargetGuid ());
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
                return new SpaceCenter.Services.CelestialBody (RemoteTech.CelestialBodyIds [API.GetAntennaTarget (part.InternalPart)]);
            }
            set {
                API.SetAntennaTarget (part.InternalPart, API.GetCelestialBodyGuid (value.InternalBody));
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
                return RemoteTech.GroundStationIds [API.GetAntennaTarget (part.InternalPart)];
            }
            set {
                if (RemoteTech.GroundStationIds.Values.All (x => x != value))
                    throw new ArgumentException ("Ground station does not exist.");
                API.SetAntennaTarget (part.InternalPart, API.GetGroundStationGuid (value));
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
                return new SpaceCenter.Services.Vessel (API.GetAntennaTarget (part.InternalPart));
            }
            set {
                API.SetAntennaTarget (part.InternalPart, value.InternalVessel.id);
            }
        }
    }
}
