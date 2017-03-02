using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

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
        /// Reference frame. Defaults to the vessel's surface reference frame (<see cref="SurfaceReferenceFrame"/>).
        /// </param>
        [KRPCMethod]
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
        [KRPCProperty]
        public Control Control {
            get { return new Control (InternalVessel); }
        }

        /// <summary>
        /// Returns a <see cref="Comms"/> object that can be used to interact
        /// with CommNet for this vessel.
        /// </summary>
        [KRPCProperty]
        public Comms Comms {
            get { return new Comms (Id); }
        }

        /// <summary>
        /// An <see cref="AutoPilot"/> object, that can be used to perform
        /// simple auto-piloting of the vessel.
        /// </summary>
        [KRPCProperty]
        public AutoPilot AutoPilot {
            get { return new AutoPilot (InternalVessel); }
        }

        /// <summary>
        /// A <see cref="Resources"/> object, that can used to get information
        /// about resources stored in the vessel.
        /// </summary>
        [KRPCProperty]
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
        [KRPCMethod]
        public Resources ResourcesInDecoupleStage (int stage, bool cumulative = true)
        {
            return new Resources (InternalVessel, stage, cumulative);
        }

        /// <summary>
        /// A <see cref="Parts.Parts"/> object, that can used to interact with the parts that make up this vessel.
        /// </summary>
        [KRPCProperty]
        public Parts.Parts Parts {
            get { return new Parts.Parts (InternalVessel); }
        }

        /// <summary>
        /// The total mass of the vessel, including resources, in kg.
        /// </summary>
        [KRPCProperty]
        public float Mass {
            get { return InternalVessel.parts.Sum (PartExtensions.WetMass); }
        }

        /// <summary>
        /// The total mass of the vessel, excluding resources, in kg.
        /// </summary>
        [KRPCProperty]
        public float DryMass {
            get { return InternalVessel.parts.Sum (PartExtensions.DryMass); }
        }

        IEnumerable<Parts.Engine> ActiveEngines {
            get { return Parts.Engines.Where (e => e.Active); }
        }

        /// <summary>
        /// The total thrust currently being produced by the vessel's engines, in
        /// Newtons. This is computed by summing <see cref="Parts.Engine.Thrust"/> for
        /// every engine in the vessel.
        /// </summary>
        [KRPCProperty]
        public float Thrust {
            get { return Parts.Engines.Sum (e => e.Thrust); }
        }

        /// <summary>
        /// Gets the total available thrust that can be produced by the vessel's
        /// active engines, in Newtons. This is computed by summing
        /// <see cref="Parts.Engine.AvailableThrust"/> for every active engine in the vessel.
        /// </summary>
        [KRPCProperty]
        public float AvailableThrust {
            get { return ActiveEngines.Sum (e => e.AvailableThrust); }
        }

        /// <summary>
        /// The total maximum thrust that can be produced by the vessel's active
        /// engines, in Newtons. This is computed by summing
        /// <see cref="Parts.Engine.MaxThrust"/> for every active engine.
        /// </summary>
        [KRPCProperty]
        public float MaxThrust {
            get { return ActiveEngines.Sum (e => e.MaxThrust); }
        }

        /// <summary>
        /// The total maximum thrust that can be produced by the vessel's active
        /// engines when the vessel is in a vacuum, in Newtons. This is computed by
        /// summing <see cref="Parts.Engine.MaxVacuumThrust"/> for every active engine.
        /// </summary>
        [KRPCProperty]
        public float MaxVacuumThrust {
            get { return ActiveEngines.Sum (e => e.MaxVacuumThrust); }
        }

        static float SpecificImpulseAtConsumption (IList<Parts.Engine> engines, float fuelConsumption)
        {
            return fuelConsumption > 0f ? engines.Sum (e => e.MaxThrust) / fuelConsumption : 0f;
        }

        /// <summary>
        /// The combined specific impulse of all active engines, in seconds. This is computed using the formula
        /// <a href="http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines">described here</a>.
        /// </summary>
        [KRPCProperty]
        public float SpecificImpulse {
            get {
                var activeEngines = ActiveEngines.ToList ();
                var fuelConsumption = activeEngines.Sum (e => e.MaxThrust / e.SpecificImpulse);
                return SpecificImpulseAtConsumption (activeEngines, fuelConsumption);
            }
        }

        /// <summary>
        /// The combined vacuum specific impulse of all active engines, in seconds. This is computed using the formula
        /// <a href="http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines">described here</a>.
        /// </summary>
        [KRPCProperty]
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
        /// <a href="http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines">described here</a>.
        /// </summary>
        [KRPCProperty]
        public float KerbinSeaLevelSpecificImpulse {
            get {
                var activeEngines = ActiveEngines.ToList ();
                var fuelConsumption = activeEngines.Sum (e => e.MaxThrust / e.KerbinSeaLevelSpecificImpulse);
                return SpecificImpulseAtConsumption (activeEngines, fuelConsumption);
            }
        }

        /// <summary>
        /// The moment of inertia of the vessel around its center of mass in <math>kg.m^2</math>.
        /// The inertia values are around the pitch, roll and yaw directions respectively.
        /// This corresponds to the vessels reference frame (<see cref="ReferenceFrame"/>).
        /// </summary>
        [KRPCProperty]
        public Tuple3 MomentOfInertia {
            get { return MomentOfInertiaVector.ToTuple (); }
        }

        internal Vector3d MomentOfInertiaVector {
            get { return InternalVessel.MOI * 1000; }
        }

        /// <summary>
        /// The inertia tensor of the vessel around its center of mass, in the vessels reference frame (<see cref="ReferenceFrame"/>).
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
            // Use the part ReferenceTransform because we want pitch/roll/yaw relative to controlling part
            Transform vesselTransform = vessel.GetTransform ();

            foreach (var part in vessel.parts) {
                if (part.rb != null) {
                    Matrix4x4 partTensor = part.rb.inertiaTensor.ToDiagonalMatrix ();

                    // translate: inertiaTensor frame to part frame, part frame to world frame, world frame to vessel frame
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
        /// The maximum torque that the vessel generate. Includes contributions from reaction wheels,
        /// RCS, gimballed engines and aerodynamic control surfaces.
        /// Returns the torques in <math>N.m</math> around each of the coordinate axes of the
        /// vessels reference frame (<see cref="ReferenceFrame"/>).
        /// These axes are equivalent to the pitch, roll and yaw axes of the vessel.
        /// </summary>
        [KRPCProperty]
        public Tuple<Tuple3,Tuple3> AvailableTorque {
            get { return AvailableTorqueVectors.ToTuple (); }
        }

        /// <summary>
        /// The maximum torque that the currently active and powered reaction wheels can generate.
        /// Returns the torques in <math>N.m</math> around each of the coordinate axes of the
        /// vessels reference frame (<see cref="ReferenceFrame"/>).
        /// These axes are equivalent to the pitch, roll and yaw axes of the vessel.
        /// </summary>
        [KRPCProperty]
        public Tuple<Tuple3, Tuple3> AvailableReactionWheelTorque {
            get { return AvailableReactionWheelTorqueVectors.ToTuple (); }
        }

        /// <summary>
        /// The maximum torque that the currently active RCS thrusters can generate.
        /// Returns the torques in <math>N.m</math> around each of the coordinate axes of the
        /// vessels reference frame (<see cref="ReferenceFrame"/>).
        /// These axes are equivalent to the pitch, roll and yaw axes of the vessel.
        /// </summary>
        [KRPCProperty]
        public Tuple<Tuple3, Tuple3> AvailableRCSTorque {
            get { return AvailableRCSTorqueVectors.ToTuple (); }
        }

        /// <summary>
        /// The maximum torque that the currently active and gimballed engines can generate.
        /// Returns the torques in <math>N.m</math> around each of the coordinate axes of the
        /// vessels reference frame (<see cref="ReferenceFrame"/>).
        /// These axes are equivalent to the pitch, roll and yaw axes of the vessel.
        /// </summary>
        [KRPCProperty]
        public Tuple<Tuple3, Tuple3> AvailableEngineTorque {
            get { return AvailableEngineTorqueVectors.ToTuple (); }
        }

        /// <summary>
        /// The maximum torque that the aerodynamic control surfaces can generate.
        /// Returns the torques in <math>N.m</math> around each of the coordinate axes of the
        /// vessels reference frame (<see cref="ReferenceFrame"/>).
        /// These axes are equivalent to the pitch, roll and yaw axes of the vessel.
        /// </summary>
        [KRPCProperty]
        public Tuple<Tuple3, Tuple3> AvailableControlSurfaceTorque {
            get { return AvailableControlSurfaceTorqueVectors.ToTuple (); }
        }

        /// <summary>
        /// The maximum torque that parts (excluding reaction wheels, gimballed engines, RCS and control surfaces) can generate.
        /// Returns the torques in <math>N.m</math> around each of the coordinate axes of the
        /// vessels reference frame (<see cref="ReferenceFrame"/>).
        /// These axes are equivalent to the pitch, roll and yaw axes of the vessel.
        /// </summary>
        [KRPCProperty]
        public Tuple<Tuple3, Tuple3> AvailableOtherTorque {
            get { return AvailableOtherTorqueVectors.ToTuple (); }
        }

        internal Tuple<Vector3d,Vector3d> AvailableTorqueVectors {
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

        Tuple<Vector3d,Vector3d> AvailableReactionWheelTorqueVectors {
            get { return ITorqueProviderExtensions.Sum (Parts.ReactionWheels.Select (x => x.AvailableTorqueVectors)); }
        }

        Tuple<Vector3d,Vector3d> AvailableRCSTorqueVectors {
            get { return ITorqueProviderExtensions.Sum (Parts.RCS.Select (x => x.AvailableTorqueVectors)); }
        }

        Tuple<Vector3d,Vector3d> AvailableEngineTorqueVectors {
            get { return ITorqueProviderExtensions.Sum (Parts.Engines.Select (x => x.AvailableTorqueVectors)); }
        }

        Tuple<Vector3d,Vector3d> AvailableControlSurfaceTorqueVectors {
            get { return ITorqueProviderExtensions.Sum (Parts.ControlSurfaces.Select (x => x.AvailableTorqueVectors)); }
        }

        Tuple<Vector3d,Vector3d> AvailableOtherTorqueVectors {
            get {
                var torques = new List<Tuple<Vector3d,Vector3d>> ();
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
        /// The reference frame that is fixed relative to the vessel, and orientated with the vessel.
        /// <list type="bullet">
        /// <item><description>The origin is at the center of mass of the vessel.</description></item>
        /// <item><description>The axes rotate with the vessel.</description></item>
        /// <item><description>The x-axis points out to the right of the vessel.</description></item>
        /// <item><description>The y-axis points in the forward direction of the vessel.</description></item>
        /// <item><description>The z-axis points out of the bottom off the vessel.</description></item>
        /// </list>
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return ReferenceFrame.Object (InternalVessel); }
        }

        /// <summary>
        /// The reference frame that is fixed relative to the vessel, and orientated with the vessels
        /// orbital prograde/normal/radial directions.
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
        [KRPCProperty]
        public ReferenceFrame OrbitalReferenceFrame {
            get { return ReferenceFrame.Orbital (InternalVessel); }
        }

        /// <summary>
        /// The reference frame that is fixed relative to the vessel, and orientated with the surface
        /// of the body being orbited.
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
        [KRPCProperty]
        public ReferenceFrame SurfaceReferenceFrame {
            get { return ReferenceFrame.Surface (InternalVessel); }
        }

        /// <summary>
        /// The reference frame that is fixed relative to the vessel, and orientated with the velocity
        /// vector of the vessel relative to the surface of the body being orbited.
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
        [KRPCProperty]
        public ReferenceFrame SurfaceVelocityReferenceFrame {
            get { return ReferenceFrame.SurfaceVelocity (InternalVessel); }
        }

        /// <summary>
        /// Returns the position vector of the center of mass of the vessel in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.PositionFromWorldSpace (InternalVessel.CoM).ToTuple ();
        }

        /// <summary>
        /// The axis-aligned bounding box of the vessel in the given reference frame.
        /// Returns the minimum and maximum vertices of the box.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        public Tuple<Tuple3,Tuple3> BoundingBox (ReferenceFrame referenceFrame)
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
        /// Returns the velocity vector of the center of mass of the vessel in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
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
        /// Returns the rotation of the center of mass of the vessel in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple4 Rotation (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.RotationFromWorldSpace (InternalVessel.ReferenceTransform.rotation).ToTuple ();
        }

        /// <summary>
        /// Returns the direction in which the vessel is pointing, as a unit vector, in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.DirectionFromWorldSpace (InternalVessel.ReferenceTransform.up).ToTuple ();
        }

        /// <summary>
        /// Returns the angular velocity of the vessel in the given reference frame. The magnitude of the returned
        /// vector is the rotational speed in radians per second, and the direction of the vector indicates the
        /// axis of rotation (using the right hand rule).
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 AngularVelocity (ReferenceFrame referenceFrame)
        {
            // FIXME: finding the rigidbody is expensive - cache it
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.AngularVelocityFromWorldSpace (InternalVessel.GetComponent<Rigidbody> ().angularVelocity).ToTuple ();
        }
    }
}
