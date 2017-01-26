using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// An RCS block or thruster. Obtained by calling <see cref="Part.RCS"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class RCS : Equatable<RCS>
    {
        readonly ModuleRCS rcs;

        internal static bool Is (Part part)
        {
            return Is (part.InternalPart);
        }

        internal static bool Is (global::Part part)
        {
            return part.HasModule<ModuleRCS> ();
        }

        internal RCS (Part part)
        {
            Part = part;
            rcs = part.InternalPart.Module<ModuleRCS> ();
            if (rcs == null)
                throw new ArgumentException ("Part does not have a ModuleRCS PartModule");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (RCS other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && rcs.Equals (other.rcs);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode () ^ rcs.GetHashCode ();
        }

        /// <summary>
        /// The part object for this RCS.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the RCS thrusters are active.
        /// An RCS thruster is inactive if the RCS action group is disabled (<see cref="Control.RCS"/>),
        /// the RCS thruster itself is not enabled (<see cref="Enabled"/>) or
        /// it is covered by a fairing (<see cref="Part.Shielded"/>).
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get {
                // TODO: what about rcs.shieldedCanThrust?
                var p = Part.InternalPart;
                return
                p.vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.RCS)] &&
                !p.ShieldedFromAirstream &&
                rcs.rcsEnabled &&
                rcs.isEnabled &&
                !rcs.isJustForShow;
            }
        }

        /// <summary>
        /// Whether the RCS thrusters are enabled.
        /// </summary>
        [KRPCProperty]
        public bool Enabled {
            get { return rcs.rcsEnabled; }
            set { rcs.rcsEnabled = value; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when pitch control input is given.
        /// </summary>
        [KRPCProperty]
        public bool PitchEnabled {
            get { return rcs.enablePitch; }
            set { rcs.enablePitch = value; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when yaw control input is given.
        /// </summary>
        [KRPCProperty]
        public bool YawEnabled {
            get { return rcs.enableYaw; }
            set { rcs.enableYaw = value; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when roll control input is given.
        /// </summary>
        [KRPCProperty]
        public bool RollEnabled {
            get { return rcs.enableRoll; }
            set { rcs.enableRoll = value; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when pitch control input is given.
        /// </summary>
        [KRPCProperty]
        public bool ForwardEnabled {
            get { return rcs.enableZ; }
            set { rcs.enableZ = value; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when yaw control input is given.
        /// </summary>
        [KRPCProperty]
        public bool UpEnabled {
            get { return rcs.enableY; }
            set { rcs.enableY = value; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when roll control input is given.
        /// </summary>
        [KRPCProperty]
        public bool RightEnabled {
            get { return rcs.enableX; }
            set { rcs.enableX = value; }
        }

        /// <summary>
        /// The available torque in the pitch, roll and yaw axes of the vessel, in Newton meters.
        /// These axes correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame" />.
        /// Returns zero if the RCS is inactive.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        public Tuple<Tuple3, Tuple3> AvailableTorque {
            get { return AvailableTorqueVectors.ToTuple (); }
        }

        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        internal Tuple<Vector3d,Vector3d> AvailableTorqueVectors {
            get {
                if (!Active)
                    return ITorqueProviderExtensions.zero;
                return rcs.GetPotentialTorque ();
            }
        }

        /// <summary>
        /// Get the thrust of the RCS thruster with the given atmospheric conditions, in Newtons.
        /// </summary>
        float GetThrust (double pressure)
        {
            pressure *= PhysicsGlobals.KpaToAtmospheres;
            return 1000f * (float)rcs.maxFuelFlow * (float)rcs.G * rcs.atmosphereCurve.Evaluate ((float)pressure);
        }

        /// <summary>
        /// The maximum amount of thrust that can be produced by the RCS thrusters when active, in Newtons.
        /// </summary>
        [KRPCProperty]
        public float MaxThrust {
            get { return GetThrust (rcs.vessel.staticPressurekPa); }
        }

        /// <summary>
        /// The maximum amount of thrust that can be produced by the RCS thrusters when active in a vacuum, in Newtons.
        /// </summary>
        [KRPCProperty]
        public float MaxVacuumThrust {
            get { return rcs.thrusterPower * 1000f; }
        }

        /// <summary>
        /// A list of thrusters, one of each nozzel in the RCS part.
        /// </summary>
        [KRPCProperty]
        public IList<Thruster> Thrusters {
            get { return Enumerable.Range (0, rcs.thrusterTransforms.Count).Select (i => new Thruster (Part, rcs, i)).ToList (); }
        }

        /// <summary>
        /// The current specific impulse of the RCS, in seconds. Returns zero
        /// if the RCS is not active.
        /// </summary>
        [KRPCProperty]
        public float SpecificImpulse {
            get { return rcs.realISP; }
        }

        /// <summary>
        /// The vacuum specific impulse of the RCS, in seconds.
        /// </summary>
        [KRPCProperty]
        public float VacuumSpecificImpulse {
            get { return rcs.atmosphereCurve.Evaluate (0); }
        }

        /// <summary>
        /// The specific impulse of the RCS at sea level on Kerbin, in seconds.
        /// </summary>
        [KRPCProperty]
        public float KerbinSeaLevelSpecificImpulse {
            get { return rcs.atmosphereCurve.Evaluate (1); }
        }

        /// <summary>
        /// The names of resources that the RCS consumes.
        /// </summary>
        [KRPCProperty]
        public IList<string> Propellants {
            get { return rcs.propellants.Select (x => x.name).ToList (); }
        }

        /// <summary>
        /// The ratios of resources that the RCS consumes. A dictionary mapping resource names
        /// to the ratios at which they are consumed by the RCS.
        /// </summary>
        [KRPCProperty]
        public IDictionary<string, float> PropellantRatios {
            get {
                var max = rcs.propellants.Max (p => p.ratio);
                return rcs.propellants.ToDictionary (p => p.name, p => p.ratio / max);
            }
        }

        /// <summary>
        /// Whether the RCS has fuel available.
        /// </summary>
        /// <remarks>
        /// The RCS thruster must be activated for this property to update correctly.
        /// </remarks>
        // FIXME: should not have to enable the RCS thruster for this to update
        [KRPCProperty]
        public bool HasFuel {
            get {
                foreach (var propellant in rcs.propellants)
                    if (propellant.isDeprived && !propellant.ignoreForIsp)
                        return false;
                return true;
            }
        }
    }
}
