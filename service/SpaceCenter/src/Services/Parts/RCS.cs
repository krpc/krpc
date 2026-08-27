using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;
using TupleV3 = System.Tuple<Vector3d, Vector3d>;
using TupleT3 = System.Tuple<System.Tuple<double, double, double>, System.Tuple<double, double, double>>;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// An RCS block or thruster. Obtained by calling <see cref="Part.RCS"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class RCS : Equatable<RCS>, IGameObjectState
    {
        ModuleRef rcsRef;

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
            rcsRef = ModuleRef.ForType<ModuleRCS> (part.InternalPart);
            if (rcsRef.Find (part.InternalPart) == null)
                throw new ArgumentException ("Part does not have a ModuleRCS PartModule");
        }

        ModuleRCS InternalRCS {
            get { return (ModuleRCS)rcsRef.Get (Part.InternalPart); }
        }

        /// <summary>
        /// The state of the part carrying the RCS thrusters, or destroyed once that part
        /// loses the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return rcsRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (RCS other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && rcsRef == other.rcsRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (Part).And (rcsRef);
        }

        /// <summary>
        /// The part object for this RCS.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the RCS thrusters are active.
        /// An RCS thruster is inactive if the RCS action group is disabled
        /// (<see cref="Control.RCS"/>), the RCS thruster itself is not enabled
        /// (<see cref="Enabled"/>), or it is covered by a fairing
        /// (<see cref="Part.Shielded"/>) and cannot thrust while shielded.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get {
                var p = Part.InternalPart;
                return
                p.vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.RCS)] &&
                (!p.ShieldedFromAirstream || InternalRCS.shieldedCanThrust) &&
                InternalRCS.rcsEnabled &&
                InternalRCS.isEnabled &&
                !InternalRCS.isJustForShow;
            }
        }

        /// <summary>
        /// Whether the RCS thrusters are enabled.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool Enabled {
            get { return InternalRCS.rcsEnabled; }
            set { InternalRCS.rcsEnabled = value; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when pitch control input is given.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool PitchEnabled {
            get { return InternalRCS.enablePitch; }
            set { InternalRCS.enablePitch = value; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when yaw control input is given.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool YawEnabled {
            get { return InternalRCS.enableYaw; }
            set { InternalRCS.enableYaw = value; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when roll control input is given.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool RollEnabled {
            get { return InternalRCS.enableRoll; }
            set { InternalRCS.enableRoll = value; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when pitch control input is given.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool ForwardEnabled {
            get { return InternalRCS.enableZ; }
            set { InternalRCS.enableZ = value; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when yaw control input is given.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool UpEnabled {
            get { return InternalRCS.enableY; }
            set { InternalRCS.enableY = value; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when roll control input is given.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool RightEnabled {
            get { return InternalRCS.enableX; }
            set { InternalRCS.enableX = value; }
        }

        /// <summary>
        /// Whether the RCS control is being set directly, bypassing the vessel's
        /// normal flight controls. When enabled, the rotation and translation demand is set by
        /// <see cref="RotationOverride"/> and <see cref="TranslationOverride"/> instead of the
        /// normal control inputs. The override is automatically released if the
        /// controlling client disconnects or the vessel changes.
        /// </summary>
        [KRPCProperty]
        public bool InputOverride {
            get { return ActuatorControlAddon.GetRCSOverride (InternalRCS); }
            set { ActuatorControlAddon.SetRCSOverride (InternalRCS, value); }
        }

        /// <summary>
        /// The rotation demand applied when <see cref="InputOverride"/> is enabled, in the pitch,
        /// roll and yaw axes. Each component is a normalized control input between -1 and 1.
        /// </summary>
        [KRPCProperty]
        public Tuple3 RotationOverride {
            get { return ActuatorControlAddon.GetRCSRotation (InternalRCS).ToTuple (); }
            set { ActuatorControlAddon.SetRCSRotation (InternalRCS, value.ToVector ()); }
        }

        /// <summary>
        /// The translation demand applied when <see cref="InputOverride"/> is enabled, in the
        /// right, up and forward axes. Each component is a normalized control input between -1
        /// and 1.
        /// </summary>
        [KRPCProperty]
        public Tuple3 TranslationOverride {
            get { return ActuatorControlAddon.GetRCSTranslation (InternalRCS).ToTuple (); }
            set { ActuatorControlAddon.SetRCSTranslation (InternalRCS, value.ToVector ()); }
        }

        /// <summary>
        /// The available torque, in Newton meters, that can be produced by this RCS,
        /// in the positive and negative pitch, roll and yaw axes of the vessel. These axes
        /// correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame"/>.
        /// Returns zero if RCS is disable.
        /// </summary>
        [KRPCProperty]
        public TupleT3 AvailableTorque {
            get { return AvailableTorqueVectors.ToTuple (); }
        }

        internal TupleV3 AvailableTorqueVectors {
            get {
                if (!Active)
                    return ITorqueProviderExtensions.zero;
                return GetTorqueVectors();
            }
        }

        /// <summary>
        /// The available force, in Newtons, that can be produced by this RCS,
        /// in the positive and negative x, y and z axes of the vessel. These axes
        /// correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame"/>.
        /// Returns zero if RCS is disabled.
        /// </summary>
        [KRPCProperty]
        public TupleT3 AvailableForce {
            get { return AvailableForceVectors.ToTuple (); }
        }

        internal TupleV3 AvailableForceVectors {
            get {
                if (!Active)
                    return ITorqueProviderExtensions.zero;
                return GetForceVectors ();
            }
        }

        /// <summary>
        /// The force being applied by the RCS thrusters, in Newtons. The vector is in the
        /// vessel's reference frame (<see cref="Vessel.ReferenceFrame"/>).
        /// Returns zero if the RCS is inactive.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Force {
            get { return AppliedForceAndTorque ().Item1.ToTuple (); }
        }

        /// <summary>
        /// The torque being applied by the RCS thrusters about the vessel's center of mass,
        /// in Newton meters. The vector is in the vessel's reference frame
        /// (<see cref="Vessel.ReferenceFrame"/>). Returns zero if the RCS is inactive.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Torque {
            get { return AppliedForceAndTorque ().Item2.ToTuple (); }
        }

        /// <summary>
        /// The force the given demands would produce, in Newtons. The vector is in the
        /// vessel's reference frame (<see cref="Vessel.ReferenceFrame"/>).
        /// Returns zero if the RCS is inactive.
        /// </summary>
        /// <returns>The force as a vector.</returns>
        /// <param name="rotation">A rotation demand, as set by
        /// <see cref="RotationOverride"/>.</param>
        /// <param name="translation">A translation demand, as set by
        /// <see cref="TranslationOverride"/>.</param>
        /// <remarks>
        /// The demands do not have to be applied. The prediction leaves out precision mode,
        /// full thrust and the lever divisor, which <see cref="Force"/> includes.
        /// </remarks>
        [KRPCMethod]
        public Tuple3 OverrideForce (Tuple3 rotation, Tuple3 translation)
        {
            return DemandForceAndTorque (rotation, translation).Item1.ToTuple ();
        }

        /// <summary>
        /// The torque the given demands would produce about the vessel's center of mass, in
        /// Newton meters. The vector is in the vessel's reference frame
        /// (<see cref="Vessel.ReferenceFrame"/>). Returns zero if the RCS is inactive.
        /// </summary>
        /// <returns>The torque as a vector.</returns>
        /// <param name="rotation">A rotation demand, as set by
        /// <see cref="RotationOverride"/>.</param>
        /// <param name="translation">A translation demand, as set by
        /// <see cref="TranslationOverride"/>.</param>
        /// <remarks>
        /// The demands do not have to be applied. The prediction leaves out precision mode,
        /// full thrust and the lever divisor, which <see cref="Torque"/> includes.
        /// </remarks>
        [KRPCMethod]
        public Tuple3 OverrideTorque (Tuple3 rotation, Tuple3 translation)
        {
            return DemandForceAndTorque (rotation, translation).Item2.ToTuple ();
        }

        /// <summary>
        /// Whether the game has stopped allocating thrust to the nozzles. ModuleRCS returns
        /// early from its update in high warp, leaving the last allocation in place.
        /// </summary>
        internal static bool ThrustAllocationStale {
            get { return TimeWarp.CurrentRate > 1f && TimeWarp.WarpMode == TimeWarp.Modes.HIGH; }
        }

        /// <summary>
        /// Sums the force and torque the game allocated to each nozzle in the last physics
        /// frame, in the vessel's reference frame.
        /// </summary>
        TupleV3 AppliedForceAndTorque ()
        {
            if (!Active || ThrustAllocationStale)
                return ITorqueProviderExtensions.zero;
            var frame = Part.Vessel.ReferenceFrame;
            var force = Vector3d.zero;
            var torque = Vector3d.zero;
            foreach (var thruster in Thrusters) {
                var thrust = thruster.Thrust;
                if (thrust <= 0f)
                    continue;
                var thrusterForce = thruster.ThrustDirection (frame).ToVector () * thrust;
                force += thrusterForce;
                torque += Vector3d.Cross (thruster.ThrustPosition (frame).ToVector (), thrusterForce);
            }
            return new TupleV3 (force, torque);
        }

        /// <summary>
        /// Runs the game's thrust allocation over the nozzles for the given demands, in the
        /// vessel's reference frame. A nozzle fires when its exhaust aligns with the demand,
        /// hence the sign of each dot product, and the two terms are summed before the clamp.
        /// </summary>
        TupleV3 DemandForceAndTorque (Tuple3 rotationDemand, Tuple3 translationDemand)
        {
            if (!Active)
                return ITorqueProviderExtensions.zero;
            // The demands go through the mapping the override applies, clamped as its setters
            // clamp them
            Vector3 rotationInput = rotationDemand.ToVector ();
            Vector3 translationInput = translationDemand.ToVector ();
            Vector3d rotation = ActuatorControlAddon.RCSRotationInput (rotationInput.Clamp (-1f, 1f));
            Vector3d translation = ActuatorControlAddon.RCSTranslationInput (translationInput.Clamp (-1f, 1f));
            var frame = Part.Vessel.ReferenceFrame;
            var thrust = AvailableThrust;
            var force = Vector3d.zero;
            var torque = Vector3d.zero;
            foreach (var thruster in Thrusters) {
                var direction = thruster.ThrustDirection (frame).ToVector ();
                var position = thruster.ThrustPosition (frame).ToVector ();
                var demand = Math.Max (0, -Vector3d.Dot (direction, translation));
                if (rotation != Vector3d.zero) {
                    var lever = Vector3d.Exclude (rotation, position);
                    if (lever.sqrMagnitude > 0)
                        demand += Math.Max (0, -Vector3d.Dot (direction, Vector3d.Cross (rotation, lever.normalized)));
                }
                var thrusterForce = direction * Math.Min (demand, 1) * thrust;
                force += thrusterForce;
                torque += Vector3d.Cross (position, thrusterForce);
            }
            return new TupleV3 (force, torque);
        }

        /// <summary>
        /// Calculates available torque vectors.
        /// We use this custom code rather than KSPs ITorqueProvider as it produces erroneous values.
        /// </summary>
        private TupleV3 GetTorqueVectors()
        {
            var frame = Part.Vessel.ReferenceFrame;
            var thrust = AvailableThrust;
            double torqueX = 0;
            double torqueXn = 0;
            double torqueY = 0;
            double torqueYn = 0;
            double torqueZ = 0;
            double torqueZn = 0;
            foreach (var thruster in Thrusters) {
                // torque = cross product of position and force
                var thrustPosition = thruster.ThrustPosition(frame);
                var thrustDirection = thruster.ThrustDirection(frame);
                var forceX = thrustDirection.Item1 * thrust;
                var forceY = thrustDirection.Item2 * thrust;
                var forceZ = thrustDirection.Item3 * thrust;
                var posX = thrustPosition.Item1;
                var posY = thrustPosition.Item2;
                var posZ = thrustPosition.Item3;
                double torque = 0;
                // Torque around X axis (pitch)
                torque = InternalRCS.enablePitch ? posY * forceZ - posZ * forceY : 0d;
                if (torque > 0) torqueX += torque;
                else torqueXn += -torque;
                // Torque around Y axis (roll)
                torque = InternalRCS.enableRoll ? posZ * forceX - posX * forceZ : 0d;
                if (torque > 0) torqueY += torque;
                else torqueYn += -torque;
                // Torque around Z axis (yaw)
                torque = InternalRCS.enableYaw ? posX * forceY - posY * forceX : 0d;
                if (torque > 0) torqueZ += torque;
                else torqueZn += -torque;
            }
            return new TupleV3(
                new Vector3d(torqueX, torqueY, torqueZ),
                new Vector3d(-torqueXn, -torqueYn, -torqueZn));
        }

        /// <summary>
        /// Calculates available force vectors.
        /// </summary>
        private TupleV3 GetForceVectors ()
        {
            var frame = Part.Vessel.ReferenceFrame;
            // Thrust-limited (and fuel-gated) thrust, so available force reflects the thrust limiter
            // (consistent with GetTorqueVectors).
            var thrust = AvailableThrust;
            var force = Vector3d.zero;
            var forceN = Vector3d.zero;
            foreach (var thruster in Thrusters) {
                var thrustDirection = thruster.ThrustDirection (frame);
                var forceX = thrustDirection.Item1 * thrust;
                var forceY = thrustDirection.Item2 * thrust;
                var forceZ = thrustDirection.Item3 * thrust;
                if (forceX > 0)
                    force.x += forceX;
                else
                    forceN.x += forceX;
                if (forceY > 0)
                    force.y += forceY;
                else
                    forceN.y += forceY;
                if (forceZ > 0)
                    force.z += forceZ;
                else
                    forceN.z += forceZ;
            }
            return new TupleV3(force, forceN);
        }

        /// <summary>
        /// Get the thrust of the RCS thruster with the given atmospheric conditions, in Newtons.
        /// </summary>
        float GetThrust (double throttle, double pressure)
        {
            pressure *= PhysicsGlobals.KpaToAtmospheres;
            return 1000f * (float)InternalRCS.maxFuelFlow * (float)throttle * (float)InternalRCS.G * InternalRCS.atmosphereCurve.Evaluate ((float)pressure);
        }

        /// <summary>
        /// The amount of thrust, in Newtons, that would be produced by the thruster when activated.
        /// Returns zero if the thruster does not have any fuel.
        /// Takes the thrusters current <see cref="ThrustLimit"/> and atmospheric conditions
        /// into account.
        /// </summary>
        [KRPCProperty]
        public float AvailableThrust {
            get {
                if (!HasFuel)
                    return 0f;
                return GetThrust (ThrustLimit, InternalRCS.vessel.staticPressurekPa);
            }
        }

        /// <summary>
        /// The maximum amount of thrust that can be produced by the RCS thrusters when active,
        /// in Newtons, with the <see cref="ThrustLimit"/> set to 100%.
        /// Takes atmospheric conditions into account.
        /// </summary>
        [KRPCProperty]
        public float MaxThrust {
            get { return GetThrust (1f, InternalRCS.vessel.staticPressurekPa); }
        }

        /// <summary>
        /// The maximum amount of thrust that can be produced by the RCS thrusters when active
        /// in a vacuum, in Newtons.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float MaxVacuumThrust {
            get { return InternalRCS.thrusterPower * 1000f; }
        }

        /// <summary>
        /// The thrust limiter of the thruster. A value between 0 and 1.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float ThrustLimit {
            get { return InternalRCS.thrustPercentage / 100f; }
            set { InternalRCS.thrustPercentage = (value * 100f).Clamp (0f, 100f); }
        }

        /// <summary>
        /// A list of thrusters, one of each nozzel in the RCS part.
        /// </summary>
        [KRPCProperty]
        public IList<Thruster> Thrusters {
            get {
                // The mesh carries the nozzles of every part variant, and the game fires only the
                // ones the chosen variant leaves active in the hierarchy
                var transforms = InternalRCS.thrusterTransforms;
                return Enumerable.Range (0, transforms.Count)
                    .Where (i => transforms [i].gameObject.activeInHierarchy)
                    .Select (i => new Thruster (Part, InternalRCS, i))
                    .ToList ();
            }
        }

        /// <summary>
        /// The current specific impulse of the RCS, in seconds. Returns zero
        /// if the RCS is not active.
        /// </summary>
        [KRPCProperty]
        public float SpecificImpulse {
            get { return InternalRCS.realISP; }
        }

        /// <summary>
        /// The vacuum specific impulse of the RCS, in seconds.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float VacuumSpecificImpulse {
            get { return InternalRCS.atmosphereCurve.Evaluate (0); }
        }

        /// <summary>
        /// The specific impulse of the RCS at sea level on Kerbin, in seconds.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float KerbinSeaLevelSpecificImpulse {
            get { return InternalRCS.atmosphereCurve.Evaluate (1); }
        }

        /// <summary>
        /// Ensures the propellant amounts have been updated, which may not have
        /// happened if the engine has not been activated.
        /// </summary>
        void UpdateConnectedResources()
        {
            foreach (var propellant in InternalRCS.propellants)
                propellant.UpdateConnectedResources(InternalRCS.part);
        }

        /// <summary>
        /// The names of resources that the RCS consumes.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public IList<string> Propellants {
            get { return InternalRCS.propellants.Select (x => x.name).ToList (); }
        }

        /// <summary>
        /// The ratios of resources that the RCS consumes. A dictionary mapping resource names
        /// to the ratios at which they are consumed by the RCS.
        /// </summary>
        [KRPCProperty]
        public IDictionary<string, float> PropellantRatios {
            get
            {
                UpdateConnectedResources();
                var max = InternalRCS.propellants.Max (p => p.ratio);
                return InternalRCS.propellants.ToDictionary (p => p.name, p => p.ratio / max);
            }
        }

        /// <summary>
        /// Whether the RCS has fuel available.
        /// </summary>
        [KRPCProperty]
        public bool HasFuel {
            get
            {
                UpdateConnectedResources();
                foreach (var propellant in InternalRCS.propellants)
                    if (propellant.actualTotalAvailable < 0.001)
                        return false;
                return true;
            }
        }
    }
}
