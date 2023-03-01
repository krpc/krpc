using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;
using TupleV3 = KRPC.Utils.Tuple<Vector3d, Vector3d>;
using TupleT3 = KRPC.Utils.Tuple<KRPC.Utils.Tuple<double, double, double>, KRPC.Utils.Tuple<double, double, double>>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// These objects are used to interact with vessels in KSP. This includes getting
    /// orbital and flight data, manipulating control inputs and managing resources.
    /// Created using <see cref="SpaceCenter.ActiveVessel"/> or <see cref="SpaceCenter.Vessels"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
    [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
    public class Vessel : Equatable<Vessel>
    {
        /// <summary>
        /// Construct from a KSP vessel object.
        /// </summary>
        public Vessel (global::Vessel vessel)
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            Id = vessel.id;
        }

        /// <summary>
        /// Construct from a KSP vessel id.
        /// </summary>
        public Vessel (Guid id)
        {
            Id = id;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Vessel other)
        {
            return !ReferenceEquals (other, null) && Id == other.Id;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Id.GetHashCode ();
        }

        /// <summary>
        /// The KSP vessel id.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// The KSP vessel object.
        /// </summary>
        public global::Vessel InternalVessel {
            get { return FlightGlobalsExtensions.GetVesselById (Id); }
        }

        /// <summary>
        /// The name of the vessel.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return InternalVessel.vesselName; }
            set { InternalVessel.vesselName = value; }
        }

        /// <summary>
        /// The type of the vessel.
        /// </summary>
        [KRPCProperty]
        public VesselType Type {
            get { return InternalVessel.vesselType.ToVesselType (); }
            set { InternalVessel.vesselType = value.FromVesselType (); }
        }

        /// <summary>
        /// The situation the vessel is in.
        /// </summary>
        [KRPCProperty]
        public VesselSituation Situation {
            get { return InternalVessel.situation.ToVesselSituation (); }
        }

        /// <summary>
        /// Whether the vessel is recoverable.
        /// </summary>
        [KRPCProperty]
        public bool Recoverable {
            get { return InternalVessel.IsRecoverable; }
        }

        /// <summary>
        /// Recover the vessel.
        /// </summary>
        [KRPCMethod]
        public void Recover ()
        {
            if (!Recoverable)
                throw new InvalidOperationException ("Vessel is not recoverable");
            GameEvents.OnVesselRecoveryRequested.Fire (InternalVessel);
        }

        /// <summary>
        /// The mission elapsed time in seconds.
        /// </summary>
        [KRPCProperty]
        public double MET {
            get { return InternalVessel.missionTime; }
        }

        /// <summary>
        /// The name of the biome the vessel is currently in.
        /// </summary>
        [KRPCProperty]
        public string Biome {
            get {
                var vessel = InternalVessel;
                var body = vessel.orbit.referenceBody;
                return ScienceUtil.GetExperimentBiome (body, vessel.latitude, vessel.longitude);
            }
        }

        /// <summary>
        /// Returns a <see cref="Flight"/> object that can be used to get flight
        /// telemetry for the vessel, in the specified reference frame.
        /// </summary>
        /// <param name="referenceFrame">
        /// Reference frame. Defaults to the vessel's surface reference frame
        /// (<see cref="SurfaceReferenceFrame"/>).
        /// </param>
        [KRPCMethod (GameScene = GameScene.Flight)]
        public Flight Flight (ReferenceFrame referenceFrame = null)
        {
            var vessel = InternalVessel;
            if (ReferenceEquals (referenceFrame, null))
                referenceFrame = ReferenceFrame.Surface (vessel);
            return new Flight (vessel, referenceFrame);
        }

        /// <summary>
        /// The current orbit of the vessel.
        /// </summary>
        [KRPCProperty]
        public Orbit Orbit {
            get { return new Orbit (InternalVessel); }
        }

        /// <summary>
        /// Returns a <see cref="Control"/> object that can be used to manipulate
        /// the vessel's control inputs. For example, its pitch/yaw/roll controls,
        /// RCS and thrust.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public Control Control {
            get { return new Control (InternalVessel); }
        }

        /// <summary>
        /// Returns a <see cref="Comms"/> object that can be used to interact
        /// with CommNet for this vessel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public Comms Comms {
            get { return new Comms (Id); }
        }

        /// <summary>
        /// An <see cref="AutoPilot"/> object, that can be used to perform
        /// simple auto-piloting of the vessel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public AutoPilot AutoPilot {
            get { return new AutoPilot (InternalVessel); }
        }

        /// <summary>
        /// The number of crew that can occupy the vessel.
        /// </summary>
        [KRPCProperty]
        public int CrewCapacity {
            get { return InternalVessel.GetCrewCapacity (); }
        }

        /// <summary>
        /// The number of crew that are occupying the vessel.
        /// </summary>
        [KRPCProperty]
        public int CrewCount {
            get { return InternalVessel.GetCrewCount (); }
        }

        /// <summary>
        /// The crew in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<CrewMember> Crew {
            get { return InternalVessel.GetVesselCrew ().Select(x => new CrewMember (x)).ToList (); }
        }

        /// <summary>
        /// A <see cref="Resources"/> object, that can used to get information
        /// about resources stored in the vessel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public Resources Resources {
            get { return new Resources (InternalVessel); }
        }

        /// <summary>
        /// Returns a <see cref="Resources"/> object, that can used to get
        /// information about resources stored in a given <paramref name="stage"/>.
        /// </summary>
        /// <param name="stage">Get resources for parts that are decoupled in this stage.</param>
        /// <param name="cumulative">When <c>false</c>, returns the resources for parts
        /// decoupled in just the given stage. When <c>true</c> returns the resources decoupled in
        /// the given stage and all subsequent stages combined.</param>
        [KRPCMethod (GameScene = GameScene.Flight)]
        public Resources ResourcesInDecoupleStage (int stage, bool cumulative = true)
        {
            return new Resources (InternalVessel, stage, cumulative);
        }

        /// <summary>
        /// A <see cref="Parts.Parts"/> object, that can used to interact with the parts that make up this vessel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public Parts.Parts Parts {
            get { return new Parts.Parts (InternalVessel); }
        }

        /// <summary>
        /// The total mass of the vessel, including resources, in kg.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public float Mass {
            get { return InternalVessel.parts.Sum(part => part.WetMass()); }
        }

        /// <summary>
        /// The total mass of the vessel, excluding resources, in kg.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public float DryMass {
            get { return InternalVessel.parts.Sum(part => part.DryMass()); }
        }

        IEnumerable<Parts.Engine> ActiveEngines {
            get { return Parts.Engines.Where (e => e.Active); }
        }

        /// <summary>
        /// The total thrust currently being produced by the vessel's engines, in
        /// Newtons. This is computed by summing <see cref="Parts.Engine.Thrust"/> for
        /// every engine in the vessel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public float Thrust {
            get { return Parts.Engines.Sum (e => e.Thrust); }
        }

        /// <summary>
        /// Gets the total available thrust that can be produced by the vessel's
        /// active engines, in Newtons. This is computed by summing
        /// <see cref="Parts.Engine.AvailableThrust"/> for every active engine in the vessel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public float AvailableThrust {
            get { return ActiveEngines.Sum (e => e.AvailableThrust); }
        }

        /// <summary>
        /// Gets the total available thrust that can be produced by the vessel's
        /// active engines, in Newtons. This is computed by summing
        /// <see cref="Parts.Engine.AvailableThrustAt"/> for every active engine in the vessel.
        /// Takes the given pressure into account.
        /// </summary>
        /// <param name="pressure">Atmospheric pressure in atmospheres</param>
        [KRPCMethod (GameScene = GameScene.Flight)]
        public float AvailableThrustAt (double pressure)
        {
            return ActiveEngines.Sum (e => e.AvailableThrustAt (pressure));
        }

        /// <summary>
        /// The total maximum thrust that can be produced by the vessel's active
        /// engines, in Newtons. This is computed by summing
        /// <see cref="Parts.Engine.MaxThrust"/> for every active engine.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public float MaxThrust {
            get { return ActiveEngines.Sum (e => e.MaxThrust); }
        }

        /// <summary>
        /// The total maximum thrust that can be produced by the vessel's active
        /// engines, in Newtons. This is computed by summing
        /// <see cref="Parts.Engine.MaxThrustAt"/> for every active engine.
        /// Takes the given pressure into account.
        /// </summary>
        /// <param name="pressure">Atmospheric pressure in atmospheres</param>
        [KRPCMethod (GameScene = GameScene.Flight)]
        public float MaxThrustAt (double pressure)
        {
            return ActiveEngines.Sum (e => e.MaxThrustAt (pressure));
        }

        /// <summary>
        /// The total maximum thrust that can be produced by the vessel's active
        /// engines when the vessel is in a vacuum, in Newtons. This is computed by
        /// summing <see cref="Parts.Engine.MaxVacuumThrust"/> for every active engine.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public float MaxVacuumThrust {
            get { return ActiveEngines.Sum (e => e.MaxVacuumThrust); }
        }

        static float SpecificImpulseAtConsumption (IList<Parts.Engine> engines, float fuelConsumption)
        {
            return fuelConsumption > 0f ? engines.Sum (e => e.MaxThrust) / fuelConsumption : 0f;
        }

        /// <summary>
        /// The combined specific impulse of all active engines, in seconds. This is computed using the formula
        /// <a href="https://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines">described here</a>.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public float SpecificImpulse {
            get {
                var activeEngines = ActiveEngines.ToList ();
                var fuelConsumption = activeEngines.Sum (e => e.MaxThrust / e.SpecificImpulse);
                return SpecificImpulseAtConsumption (activeEngines, fuelConsumption);
            }
        }

        /// <summary>
        /// The combined specific impulse of all active engines, in seconds. This is computed using the formula
        /// <a href="https://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines">described here</a>.
        /// Takes the given pressure into account.
        /// </summary>
        /// <param name="pressure">Atmospheric pressure in atmospheres</param>
        [KRPCMethod (GameScene = GameScene.Flight)]
        public float SpecificImpulseAt (double pressure)
        {
            var activeEngines = ActiveEngines.ToList ();
            var fuelConsumption = activeEngines.Sum (e => e.MaxThrust / e.SpecificImpulseAt (pressure));
            return SpecificImpulseAtConsumption (activeEngines, fuelConsumption);
        }

        /// <summary>
        /// The combined vacuum specific impulse of all active engines, in seconds. This is computed using the formula
        /// <a href="https://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines">described here</a>.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public float VacuumSpecificImpulse {
            get {
                var activeEngines = ActiveEngines.ToList ();
                var fuelConsumption = activeEngines.Sum (e => e.MaxThrust / e.VacuumSpecificImpulse);
                return SpecificImpulseAtConsumption (activeEngines, fuelConsumption);
            }
        }

        /// <summary>
        /// The combined specific impulse of all active engines at sea level on Kerbin, in seconds.
        /// This is computed using the formula
        /// <a href="https://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines">described here</a>.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public float KerbinSeaLevelSpecificImpulse {
            get {
                var activeEngines = ActiveEngines.ToList ();
                var fuelConsumption = activeEngines.Sum (e => e.MaxThrust / e.KerbinSeaLevelSpecificImpulse);
                return SpecificImpulseAtConsumption (activeEngines, fuelConsumption);
            }
        }

        /// <summary>
        /// The moment of inertia of the vessel around its center of mass in <math>kg.m^2</math>.
        /// The inertia values in the returned 3-tuple are around the
        /// pitch, roll and yaw directions respectively.
        /// This corresponds to the vessels reference frame (<see cref="ReferenceFrame"/>).
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public Tuple3 MomentOfInertia {
            get { return MomentOfInertiaVector.ToTuple (); }
        }

        internal Vector3d MomentOfInertiaVector {
            get { return InternalVessel.MOI * 1000; }
        }

        /// <summary>
        /// The inertia tensor of the vessel around its center of mass,
        /// in the vessels reference frame (<see cref="ReferenceFrame"/>).
        /// Returns the 3x3 matrix as a list of elements, in row-major order.
        /// </summary>
        [KRPCProperty]
        public IList<double> InertiaTensor {
            get { return ComputeInertiaTensor ().ToList (); }
        }

        /// <summary>
        /// Computes the inertia tensor of the vessel. Uses the parallel axis theorem to
        /// sum the contributions to the inertia tensor from every part in the vessel.
        /// It (ab)uses the Matrix4x4 class in order to do 3x3 matrix operations.
        /// </summary>
        Matrix4x4 ComputeInertiaTensor ()
        {
            var vessel = InternalVessel;
            Matrix4x4 inertiaTensor = Matrix4x4.zero;
            Vector3 CoM = vessel.CoM;
            // Use the part ReferenceTransform because we want pitch/roll/yaw
            // relative to controlling part
            Transform vesselTransform = vessel.GetTransform ();

            foreach (var part in vessel.parts) {
                if (part.rb != null) {
                    Matrix4x4 partTensor = part.rb.inertiaTensor.ToDiagonalMatrix ();

                    // translate: inertiaTensor frame to part frame, part frame to world frame,
                    // world frame to vessel frame
                    Quaternion rot = Quaternion.Inverse (vesselTransform.rotation) * part.transform.rotation * part.rb.inertiaTensorRotation;
                    Quaternion inv = Quaternion.Inverse (rot);

                    Matrix4x4 rotMatrix = Matrix4x4.TRS (Vector3.zero, rot, Vector3.one);
                    Matrix4x4 invMatrix = Matrix4x4.TRS (Vector3.zero, inv, Vector3.one);

                    // add the part inertiaTensor to the ship inertiaTensor
                    inertiaTensor = inertiaTensor.Add (rotMatrix * partTensor * invMatrix);

                    Vector3 position = vesselTransform.InverseTransformDirection (part.rb.position - CoM);

                    // add the part mass to the ship inertiaTensor
                    inertiaTensor = inertiaTensor.Add ((part.rb.mass * position.sqrMagnitude).ToDiagonalMatrix ());

                    // add the part distance offset to the ship inertiaTensor
                    inertiaTensor = inertiaTensor.Add (position.OuterProduct (-part.rb.mass * position));
                }
            }
            return inertiaTensor.MultiplyScalar (1000f);
        }

        /// <summary>
        /// The maximum torque that the vessel generates. Includes contributions from
        /// reaction wheels, RCS, gimballed engines and aerodynamic control surfaces.
        /// Returns the torques in <math>N.m</math> around each of the coordinate axes of the
        /// vessels reference frame (<see cref="ReferenceFrame"/>).
        /// These axes are equivalent to the pitch, roll and yaw axes of the vessel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public TupleT3 AvailableTorque {
            get { return AvailableTorqueVectors.ToTuple (); }
        }

        /// <summary>
        /// The maximum torque that the currently active and powered reaction wheels can generate.
        /// Returns the torques in <math>N.m</math> around each of the coordinate axes of the
        /// vessels reference frame (<see cref="ReferenceFrame"/>).
        /// These axes are equivalent to the pitch, roll and yaw axes of the vessel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public TupleT3 AvailableReactionWheelTorque {
            get { return AvailableReactionWheelTorqueVectors.ToTuple (); }
        }

        /// <summary>
        /// The maximum torque that the currently active RCS thrusters can generate.
        /// Returns the torques in <math>N.m</math> around each of the coordinate axes of the
        /// vessels reference frame (<see cref="ReferenceFrame"/>).
        /// These axes are equivalent to the pitch, roll and yaw axes of the vessel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public TupleT3 AvailableRCSTorque {
            get { return AvailableRCSTorqueVectors.ToTuple (); }
        }

        /// <summary>
        /// The maximum force that the currently active RCS thrusters can generate.
        /// Returns the forces in <math>N</math> along each of the coordinate axes of the
        /// vessels reference frame (<see cref="ReferenceFrame"/>).
        /// These axes are equivalent to the right, forward and bottom directions of the vessel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public TupleT3 AvailableRCSForce {
            get { return AvailableRCSForceVectors.ToTuple (); }
        }

        /// <summary>
        /// The maximum torque that the currently active and gimballed engines can generate.
        /// Returns the torques in <math>N.m</math> around each of the coordinate axes of the
        /// vessels reference frame (<see cref="ReferenceFrame"/>).
        /// These axes are equivalent to the pitch, roll and yaw axes of the vessel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public TupleT3 AvailableEngineTorque {
            get { return AvailableEngineTorqueVectors.ToTuple (); }
        }

        /// <summary>
        /// The maximum torque that the aerodynamic control surfaces can generate.
        /// Returns the torques in <math>N.m</math> around each of the coordinate axes of the
        /// vessels reference frame (<see cref="ReferenceFrame"/>).
        /// These axes are equivalent to the pitch, roll and yaw axes of the vessel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public TupleT3 AvailableControlSurfaceTorque {
            get { return AvailableControlSurfaceTorqueVectors.ToTuple (); }
        }

        /// <summary>
        /// The maximum torque that parts (excluding reaction wheels, gimballed engines,
        /// RCS and control surfaces) can generate.
        /// Returns the torques in <math>N.m</math> around each of the coordinate axes of the
        /// vessels reference frame (<see cref="ReferenceFrame"/>).
        /// These axes are equivalent to the pitch, roll and yaw axes of the vessel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public TupleT3 AvailableOtherTorque {
            get { return AvailableOtherTorqueVectors.ToTuple (); }
        }

        internal TupleV3 AvailableTorqueVectors {
            get {
                return ITorqueProviderExtensions.Sum (new [] {
                    AvailableReactionWheelTorqueVectors,
                    AvailableRCSTorqueVectors,
                    AvailableEngineTorqueVectors,
                    AvailableControlSurfaceTorqueVectors,
                    AvailableOtherTorqueVectors
                });
            }
        }

        TupleV3 AvailableReactionWheelTorqueVectors {
            get { return ITorqueProviderExtensions.Sum (Parts.ReactionWheels.Select (x => x.AvailableTorqueVectors)); }
        }

        TupleV3 AvailableRCSTorqueVectors {
            get { return ITorqueProviderExtensions.Sum (Parts.RCS.Select (x => x.AvailableTorqueVectors)); }
        }

        TupleV3 AvailableRCSForceVectors {
            get { return ITorqueProviderExtensions.Sum (Parts.RCS.Select (x => x.AvailableForceVectors)); }
        }

        TupleV3 AvailableEngineTorqueVectors {
            get { return ITorqueProviderExtensions.Sum (Parts.Engines.Select (x => x.AvailableTorqueVectors)); }
        }

        TupleV3 AvailableControlSurfaceTorqueVectors {
            get { return ITorqueProviderExtensions.Sum (Parts.ControlSurfaces.Select (x => x.AvailableTorqueVectors)); }
        }

        TupleV3 AvailableOtherTorqueVectors {
            get {
                var torques = new List<TupleV3> ();
                // Include contributions from other ITorqueProviders
                var parts = InternalVessel.parts;
                for (var i = 0; i < parts.Count; i++) {
                    var part = parts [i];
                    if (Services.Parts.ReactionWheel.Is (part) ||
                        Services.Parts.RCS.Is (part) ||
                        Services.Parts.Engine.Is (part) ||
                        Services.Parts.ControlSurface.Is (part))
                        continue;
                    for (var j = 0; j < part.Modules.Count; j++) {
                        var module = part.Modules [j];
                        var torqueProvider = module as ITorqueProvider;
                        if (torqueProvider != null)
                            torques.Add (torqueProvider.GetPotentialTorque ());
                    }
                }
                return ITorqueProviderExtensions.Sum (torques);
            }
        }

        /// <summary>
        /// The reference frame that is fixed relative to the vessel,
        /// and orientated with the vessel.
        /// <list type="bullet">
        /// <item><description>The origin is at the center of mass of the vessel.</description></item>
        /// <item><description>The axes rotate with the vessel.</description></item>
        /// <item><description>The x-axis points out to the right of the vessel.</description></item>
        /// <item><description>The y-axis points in the forward direction of the vessel.</description></item>
        /// <item><description>The z-axis points out of the bottom off the vessel.</description></item>
        /// </list>
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public ReferenceFrame ReferenceFrame {
            get { return ReferenceFrame.Object (InternalVessel); }
        }

        /// <summary>
        /// The reference frame that is fixed relative to the vessel,
        /// and orientated with the vessels orbital prograde/normal/radial directions.
        /// <list type="bullet">
        /// <item><description>The origin is at the center of mass of the vessel.</description></item>
        /// <item><description>The axes rotate with the orbital prograde/normal/radial directions.</description></item>
        /// <item><description>The x-axis points in the orbital anti-radial direction.</description></item>
        /// <item><description>The y-axis points in the orbital prograde direction.</description></item>
        /// <item><description>The z-axis points in the orbital normal direction.</description></item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// Be careful not to confuse this with 'orbit' mode on the navball.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public ReferenceFrame OrbitalReferenceFrame {
            get { return ReferenceFrame.Orbital (InternalVessel); }
        }

        /// <summary>
        /// The reference frame that is fixed relative to the vessel,
        /// and orientated with the surface of the body being orbited.
        /// <list type="bullet">
        /// <item><description>The origin is at the center of mass of the vessel.</description></item>
        /// <item><description>The axes rotate with the north and up directions on the surface of the body.</description></item>
        /// <item><description>The x-axis points in the <a href="https://en.wikipedia.org/wiki/Zenith">zenith</a>
        /// direction (upwards, normal to the body being orbited, from the center of the body towards the center of
        /// mass of the vessel).</description></item>
        /// <item><description>The y-axis points northwards towards the
        /// <a href="https://en.wikipedia.org/wiki/Horizon">astronomical horizon</a> (north, and tangential to the
        /// surface of the body -- the direction in which a compass would point when on the surface).</description></item>
        /// <item><description>The z-axis points eastwards towards the
        /// <a href="https://en.wikipedia.org/wiki/Horizon">astronomical horizon</a> (east, and tangential to the
        /// surface of the body -- east on a compass when on the surface).</description></item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// Be careful not to confuse this with 'surface' mode on the navball.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public ReferenceFrame SurfaceReferenceFrame {
            get { return ReferenceFrame.Surface (InternalVessel); }
        }

        /// <summary>
        /// The reference frame that is fixed relative to the vessel,
        /// and orientated with the velocity vector of the vessel relative
        /// to the surface of the body being orbited.
        /// <list type="bullet">
        /// <item><description>The origin is at the center of mass of the vessel.</description></item>
        /// <item><description>The axes rotate with the vessel's velocity vector.</description></item>
        /// <item><description>The y-axis points in the direction of the vessel's velocity vector,
        /// relative to the surface of the body being orbited.</description></item>
        /// <item><description>The z-axis is in the plane of the
        /// <a href="https://en.wikipedia.org/wiki/Horizon">astronomical horizon</a>.</description></item>
        /// <item><description>The x-axis is orthogonal to the other two axes.</description></item>
        /// </list>
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public ReferenceFrame SurfaceVelocityReferenceFrame {
            get { return ReferenceFrame.SurfaceVelocity (InternalVessel); }
        }

        /// <summary>
        /// The position of the center of mass of the vessel, in the given reference frame.
        /// </summary>
        /// <returns>The position as a vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// position vector is in.</param>
        [KRPCMethod (GameScene = GameScene.Flight)]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.PositionFromWorldSpace (InternalVessel.CoM).ToTuple ();
        }

        /// <summary>
        /// The axis-aligned bounding box of the vessel in the given reference frame.
        /// </summary>
        /// <returns>The positions of the minimum and maximum vertices of the box,
        /// as position vectors.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// position vectors are in.</param>
        [KRPCMethod (GameScene = GameScene.Flight)]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        public TupleT3 BoundingBox (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            var parts = InternalVessel.parts;
            var bounds = parts [0].GetBounds (referenceFrame);
            for (int i = 1; i < parts.Count; i++)
                bounds.Encapsulate (parts [i].GetBounds (referenceFrame));
            return bounds.ToTuples ();
        }

        /// <summary>
        /// The velocity of the center of mass of the vessel, in the given reference frame.
        /// </summary>
        /// <returns>The velocity as a vector. The vector points in the direction of travel,
        /// and its magnitude is the speed of the body in meters per second.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// velocity vector is in.</param>
        [KRPCMethod (GameScene = GameScene.Flight)]
        public Tuple3 Velocity (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            var vessel = InternalVessel;
            var worldCoM = vessel.CoM;
            var worldVelocity = vessel.GetOrbit ().GetVel ();
            return referenceFrame.VelocityFromWorldSpace (worldCoM, worldVelocity).ToTuple ();
        }

        /// <summary>
        /// The rotation of the vessel, in the given reference frame.
        /// </summary>
        /// <returns>The rotation as a quaternion of the form <math>(x, y, z, w)</math>.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// rotation is in.</param>
        [KRPCMethod (GameScene = GameScene.Flight)]
        public Tuple4 Rotation (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.RotationFromWorldSpace (InternalVessel.ReferenceTransform.rotation).ToTuple ();
        }

        /// <summary>
        /// The direction in which the vessel is pointing, in the given reference frame.
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// direction is in.</param>
        [KRPCMethod (GameScene = GameScene.Flight)]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.DirectionFromWorldSpace (InternalVessel.ReferenceTransform.up).ToTuple ();
        }

        /// <summary>
        /// The angular velocity of the vessel, in the given reference frame.
        /// </summary>
        /// <returns>The angular velocity as a vector. The magnitude of the vector is the rotational
        /// speed of the vessel, in radians per second. The direction of the vector indicates the
        /// axis of rotation, using the right-hand rule.</returns>
        /// <param name="referenceFrame">The reference frame the returned
        /// angular velocity is in.</param>
        [KRPCMethod (GameScene = GameScene.Flight)]
        public Tuple3 AngularVelocity (ReferenceFrame referenceFrame)
        {
            // FIXME: finding the rigidbody is expensive - cache it
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.AngularVelocityFromWorldSpace (InternalVessel.GetComponent<Rigidbody> ().angularVelocity).ToTuple ();
        }
    }
}
