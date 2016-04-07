using System;
using System.Linq;
using UnityEngine;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.ExternalAPI;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;
using System.Collections.Generic;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// These objects are used to interact with vessels in KSP. This includes getting
    /// orbital and flight data, manipulating control inputs and managing resources.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Vessel : Equatable<Vessel>
    {
        /// <summary>
        /// Construct from a KSP vessel object.
        /// </summary>
        public Vessel (global::Vessel vessel)
        {
            Id = vessel.id;
        }

        /// <summary>
        /// Check if vessels are equal.
        /// </summary>
        public override bool Equals (Vessel obj)
        {
            return Id == obj.Id;
        }

        /// <summary>
        /// Hash the vessel.
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
        /// The mission elapsed time in seconds.
        /// </summary>
        [KRPCProperty]
        public double MET {
            get { return InternalVessel.missionTime; }
        }

        /// <summary>
        /// Returns a <see cref="Flight"/> object that can be used to get flight
        /// telemetry for the vessel, in the specified reference frame.
        /// </summary>
        /// <param name="referenceFrame">
        /// Reference frame. Defaults to the vessel's surface reference frame (<see cref="Vessel.SurfaceReferenceFrame"/>).
        /// </param>
        [KRPCMethod]
        public Flight Flight (ReferenceFrame referenceFrame = null)
        {
            if (referenceFrame == null)
                referenceFrame = ReferenceFrame.Surface (InternalVessel);
            return new Flight (InternalVessel, referenceFrame);
        }

        /// <summary>
        /// The target vessel. <c>null</c> if there is no target. When
        /// setting the target, the target cannot be the current vessel.
        /// </summary>
        [KRPCProperty]
        public Vessel Target {
            get { throw new NotImplementedException (); }
            set { throw new NotImplementedException (); }
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
        /// A <see cref="Comms"/> object, that can used to interact with RemoteTech for this vessel.
        /// </summary>
        /// <remarks>
        /// Requires <a href="http://forum.kerbalspaceprogram.com/threads/83305">RemoteTech</a> to be installed.
        /// </remarks>
        [KRPCProperty]
        public Comms Comms {
            get {
                if (!RemoteTech.IsAvailable)
                    throw new InvalidOperationException ("RemoteTech is not installed");
                return new Comms (InternalVessel);
            }
        }

        /// <summary>
        /// The total mass of the vessel, including resources, in kg.
        /// </summary>
        [KRPCProperty]
        public float Mass {
            get { return InternalVessel.parts.Where (p => p.IsPhysicallySignificant ()).Sum (p => p.TotalMass ()); }
        }

        /// <summary>
        /// The total mass of the vessel, excluding resources, in kg.
        /// </summary>
        [KRPCProperty]
        public float DryMass {
            get { return InternalVessel.parts.Where (p => p.IsPhysicallySignificant ()).Sum (p => p.DryMass ()); }
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
            get { return Parts.Engines.Where (e => e.Active).Sum (e => e.AvailableThrust); }
        }

        /// <summary>
        /// The total maximum thrust that can be produced by the vessel's active
        /// engines, in Newtons. This is computed by summing
        /// <see cref="Parts.Engine.MaxThrust"/> for every active engine.
        /// </summary>
        [KRPCProperty]
        public float MaxThrust {
            get { return Parts.Engines.Where (e => e.Active).Sum (e => e.MaxThrust); }
        }

        /// <summary>
        /// The total maximum thrust that can be produced by the vessel's active
        /// engines when the vessel is in a vacuum, in Newtons. This is computed by
        /// summing <see cref="Parts.Engine.MaxVacuumThrust"/> for every active engine.
        /// </summary>
        [KRPCProperty]
        public float MaxVacuumThrust {
            get { return Parts.Engines.Where (e => e.Active).Sum (e => e.MaxVacuumThrust); }
        }

        /// <summary>
        /// The combined specific impulse of all active engines, in seconds. This is computed using the formula
        /// <a href="http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines">described here</a>.
        /// </summary>
        [KRPCProperty]
        public float SpecificImpulse {
            get {
                var thrust = Parts.Engines.Where (e => e.Active).Sum (e => e.MaxThrust);
                var fuelConsumption = Parts.Engines.Where (e => e.Active).Sum (e => e.MaxThrust / e.SpecificImpulse);
                if (fuelConsumption > 0f)
                    return thrust / fuelConsumption;
                return 0f;
            }
        }

        /// <summary>
        /// The combined vacuum specific impulse of all active engines, in seconds. This is computed using the formula
        /// <a href="http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines">described here</a>.
        /// </summary>
        [KRPCProperty]
        public float VacuumSpecificImpulse {
            get {
                var thrust = Parts.Engines.Where (e => e.Active).Sum (e => e.MaxThrust);
                var fuelConsumption = Parts.Engines.Where (e => e.Active).Sum (e => e.MaxThrust / e.VacuumSpecificImpulse);
                if (fuelConsumption > 0f)
                    return thrust / fuelConsumption;
                return 0f;
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
                var thrust = Parts.Engines.Where (e => e.Active).Sum (e => e.MaxThrust);
                var fuelConsumption = Parts.Engines.Where (e => e.Active).Sum (e => e.MaxThrust / e.KerbinSeaLevelSpecificImpulse);
                if (fuelConsumption > 0f)
                    return thrust / fuelConsumption;
                return 0f;
            }
        }

        /// <summary>
        /// The moment of inertia of the vessel around its center of mass in the coordinate axes, in $kg.m^2$.
        /// </summary>
        [KRPCProperty]
        public Tuple3 MomentOfInertia {
            get { return ComputeInertiaTensor ().Diag ().ToTuple (); }
        }

        /// <summary>
        /// The inertia tensor of the vessel. Returns a 3x3 matrix as a list of elements, in row-major order.
        /// </summary>
        [KRPCProperty]
        public IList<double> InertiaTensor {
            get { return ComputeInertiaTensor ().ToList (); }
        }

        /// <summary>
        /// The maximum torque that the vessel can generate.
        /// This includes contributions from active reaction wheels, RCS thrusters, engines
        /// and aerodynamic control surfaces.
        /// Returns a vector of torques around the pitch, yaw and roll axes of the vessel, in <math>N.m</math>.
        /// These axes correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame" />.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Torque {
            get {
              return (ComputeReactionWheelTorque () +
                  ComputeRCSTorque () +
                  ComputeEngineTorque () +
                  ComputeControlSurfaceTorque ()).ToTuple ();
            }
        }

        /// <summary>
        /// The maximum torque that the currently active and powered reaction wheels can generate.
        /// Returns a vector of torques around the pitch, yaw and roll axes of the vessel, in <math>N.m</math>.
        /// These axes correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame" />.
        /// </summary>
        [KRPCProperty]
        public Tuple3 ReactionWheelTorque {
            get { return ComputeReactionWheelTorque ().ToTuple (); }
        }

        /// <summary>
        /// The maximum torque that the active RCS thrusters can generate.
        /// Returns a vector of torques around the pitch, yaw and roll axes of the vessel, in <math>N.m</math>.
        /// These axes correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame" />.
        /// </summary>
        [KRPCProperty]
        public Tuple3 RCSTorque {
            get { return ComputeRCSTorque ().ToTuple (); }
        }

        /// <summary>
        /// The maximum torque that the active gimballed engines can generate.
        /// Returns a vector of torques around the pitch, yaw and roll axes of the vessel, in <math>N.m</math>.
        /// These axes correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame" />.
        /// </summary>
        [KRPCProperty]
        public Tuple3 EngineTorque {
            get { return ComputeEngineTorque ().ToTuple (); }
        }

        /// <summary>
        /// The maximum torque that the aerodynamic control surfaces can provide.
        /// Returns a vector of torques around the pitch, yaw and roll axes of the vessel, in <math>N.m</math>.
        /// These axes correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame" />.
        /// </summary>
        [KRPCProperty]
        public Tuple3 ControlSurfaceTorque {
            get { return ComputeControlSurfaceTorque ().ToTuple (); }
        }

        /// <summary>
        /// Computes the full inertia tensor of the vessel.  This applies the parallel axis theorem to
        /// sum up all the contributions to the inertia tensor from every part.  It (ab)uses the Matrix4x4
        /// class in order to do 3x3 matrix operations in the hope that the Matrix4x4 class winds up
        /// running on the GPU.
        /// </summary>
        ///
        /// FIXME: units
        Matrix4x4 ComputeInertiaTensor ()
        {
            Matrix4x4 inertiaTensor = Matrix4x4.zero;
            Vector3 CoM = InternalVessel.findWorldCenterOfMass ();
            // Use the part ReferenceTransform because we want pitch/roll/yaw relative to controlling part
            Transform vesselTransform = InternalVessel.GetTransform ();

            foreach (var part in InternalVessel.parts) {
                if (part.rb != null) {
                    Matrix4x4 partTensor = part.rb.inertiaTensor.ToDiagonalMatrix ();

                    // translate:  inertiaTensor frame to part frame, part frame to world frame, world frame to vessel frame
                    Quaternion rot = Quaternion.Inverse(vesselTransform.rotation) * part.transform.rotation * part.rb.inertiaTensorRotation;
                    Quaternion inv = Quaternion.Inverse(rot);

                    Matrix4x4 rotMatrix = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one);
                    Matrix4x4 invMatrix = Matrix4x4.TRS(Vector3.zero, inv, Vector3.one);

                    // add the part inertiaTensor to the ship inertiaTensor
                    inertiaTensor = inertiaTensor.Add(rotMatrix * partTensor * invMatrix);

                    Vector3 position = vesselTransform.InverseTransformDirection(part.rb.position - CoM);

                    // add the part mass to the ship inertiaTensor
                    inertiaTensor = inertiaTensor.Add((part.rb.mass * position.sqrMagnitude).ToDiagonalMatrix ());

                    // add the part distance offset to the ship inertiaTensor
                    inertiaTensor = inertiaTensor.Add(position.OuterProduct(-part.rb.mass * position));
                }
            }
            return inertiaTensor.MultiplyScalar (1000f);
        }

        /// <summary>
        /// Computes the sum of the available reaction wheel torque in the vessel pitch, roll, yaw frame.
        /// </summary>
        Vector3d ComputeReactionWheelTorque ()
        {
            Vector3d reactionWheelTorque = Vector3d.zero;
            foreach (var rw in Parts.ReactionWheels.Where (e => e.Active && !e.Broken)) {
                reactionWheelTorque += rw.TorqueVector ();
            }
            return reactionWheelTorque;
        }

        Vector3d ComputeRCSTorque ()
        {
            throw new NotImplementedException ();
        }

        Vector3d ComputeEngineTorque ()
        {
            throw new NotImplementedException ();
        }

        Vector3d ComputeControlSurfaceTorque ()
        {
            throw new NotImplementedException ();
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
        /// <item><description>The x-axis points in the <a href="http://en.wikipedia.org/wiki/Zenith">zenith</a>
        /// direction (upwards, normal to the body being orbited, from the center of the body towards the center of
        /// mass of the vessel).</description></item>
        /// <item><description>The y-axis points northwards towards the
        /// <a href="http://en.wikipedia.org/wiki/Horizon">astronomical horizon</a> (north, and tangential to the
        /// surface of the body -- the direction in which a compass would point when on the surface).</description></item>
        /// <item><description>The z-axis points eastwards towards the
        /// <a href="http://en.wikipedia.org/wiki/Horizon">astronomical horizon</a> (east, and tangential to the
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
        /// <a href="http://en.wikipedia.org/wiki/Horizon">astronomical horizon</a>.</description></item>
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
            return referenceFrame.PositionFromWorldSpace (InternalVessel.findWorldCenterOfMass ()).ToTuple ();
        }

        /// <summary>
        /// Returns the velocity vector of the center of mass of the vessel in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Velocity (ReferenceFrame referenceFrame)
        {
            return referenceFrame.VelocityFromWorldSpace (InternalVessel.findWorldCenterOfMass (), InternalVessel.GetOrbit ().GetVel ()).ToTuple ();
        }

        /// <summary>
        /// Returns the rotation of the center of mass of the vessel in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple4 Rotation (ReferenceFrame referenceFrame)
        {
            return referenceFrame.RotationFromWorldSpace (InternalVessel.ReferenceTransform.rotation).ToTuple ();
        }

        /// <summary>
        /// Returns the direction in which the vessel is pointing, as a unit vector, in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
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
            return referenceFrame.AngularVelocityFromWorldSpace (-InternalVessel.rigidbody.angularVelocity).ToTuple ();
        }
    }
}
