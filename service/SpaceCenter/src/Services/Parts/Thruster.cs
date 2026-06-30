using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Controls a thruster's gimbal actuation.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class ThrusterGimbalControl : Equatable<ThrusterGimbalControl>
    {
        private static List<ThrusterGimbalControl> _activeControls = new List<ThrusterGimbalControl>();
        private class GimbalControlAdjuster : Expansions.Missions.Adjusters.AdjusterGimbalBase
        {
            
            public GimbalControlAdjuster() { }

            public GimbalControlAdjuster(Expansions.Missions.MENode node) : base(node)
            {
            }
            public Vector3 Setting = Vector3.zero;
            public override Vector3 ApplyControlAdjustment(Vector3 control) 
            {
                return Setting;
            }
        }
        private readonly ModuleGimbal gimbal;
        private readonly GimbalControlAdjuster adjuster;

        internal ThrusterGimbalControl(ModuleGimbal thrusterGimbal)
        {
            gimbal = thrusterGimbal;
            adjuster = new GimbalControlAdjuster();
            gimbal.AddPartModuleAdjuster(adjuster);
            _activeControls.Add(this);
        }

        /// <summary>
        /// Changes the control setting applied to the engine TVC. Reference frame TBD.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Control {
            get => adjuster.Setting.ToTuple();
            set => adjuster.Setting = value.ToVector();
        }

        /// <summary>
        /// Permanently disables the adjuster and removes its affect.
        /// </summary>
        [KRPCMethod]
        public void Disable()
        {
            gimbal.RemovePartModuleAdjuster(adjuster);
            _activeControls.Remove(this);
        }

        /// <summary>
        /// Permanently disables all adjusters and removes their affects.
        /// </summary>
        [KRPCProcedure]
        public static void DisableAllGimbalControls()
        {
            foreach (var control in _activeControls) control.Disable();
            _activeControls.Clear();
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ThrusterGimbalControl other)
        {
            return !ReferenceEquals (other, null) && gimbal == other.gimbal;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return gimbal.GetHashCode();
        }
    }
    /// <summary>
    /// The component of an <see cref="Engine"/> or <see cref="RCS"/> part that generates thrust.
    /// Can obtained by calling <see cref="Engine.Thrusters"/> or <see cref="RCS.Thrusters"/>.
    /// </summary>
    /// <remarks>
    /// Engines can consist of multiple thrusters.
    /// For example, the S3 KS-25x4 "Mammoth" has four rocket nozzels, and so consists of
    /// four thrusters.
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter")]
    public class Thruster : Equatable<Thruster>
    {
        readonly Part part;
        readonly ModuleEngines engine;
        readonly ModuleRCS rcs;
        readonly ModuleGimbal gimbal;
        readonly int transformIndex;

        internal Thruster (Part thrusterPart, ModuleEngines thrusterEngine, ModuleGimbal thrusterGimbal, int thrusterTransformIndex)
        {
            part = thrusterPart;
            engine = thrusterEngine;
            gimbal = thrusterGimbal;
            transformIndex = thrusterTransformIndex;
        }

        internal Thruster (Part thrusterPart, ModuleRCS thrusterRCS, int thrusterTransformIndex)
        {
            part = thrusterPart;
            rcs = thrusterRCS;
            transformIndex = thrusterTransformIndex;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Thruster other)
        {
            return !ReferenceEquals (other, null) && part == other.part && transformIndex == other.transformIndex;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode () ^ transformIndex.GetHashCode ();
        }

        /// <summary>
        /// The <see cref="Part"/> that contains this thruster.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// The position at which the thruster generates thrust, in the given reference frame.
        /// For gimballed engines, this takes into account the current rotation of the gimbal.
        /// </summary>
        /// <returns>The position as a vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// position vector is in.</param>
        [KRPCMethod]
        public Tuple3 ThrustPosition (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.PositionFromWorldSpace (WorldTransform.position).ToTuple ();
        }

        /// <summary>
        /// The direction of the force generated by the thruster, in the given reference frame.
        /// This is opposite to the direction in which the thruster expels propellant.
        /// For gimballed engines, this takes into account the current rotation of the gimbal.
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// direction is in.</param>
        [KRPCMethod]
        public Tuple3 ThrustDirection (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.DirectionFromWorldSpace (WorldThrustDirection).ToTuple ();
        }

        /// <summary>
        /// The position at which the thruster generates thrust, when the engine is in its
        /// initial position (no gimballing), in the given reference frame.
        /// </summary>
        /// <returns>The position as a vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// position vector is in.</param>
        /// <remarks>
        /// This position can move when the gimbal rotates. This is because the thrust position and
        /// gimbal position are not necessarily the same.
        /// </remarks>
        [KRPCMethod]
        public Tuple3 InitialThrustPosition (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            StashGimbalRotation ();
            var position = WorldTransform.position;
            RestoreGimbalRotation ();
            return referenceFrame.PositionFromWorldSpace (position).ToTuple ();
        }

        /// <summary>
        /// The direction of the force generated by the thruster, when the engine is in its
        /// initial position (no gimballing), in the given reference frame.
        /// This is opposite to the direction in which the thruster expels propellant.
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// direction is in.</param>
        [KRPCMethod]
        public Tuple3 InitialThrustDirection (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            StashGimbalRotation ();
            var direction = WorldThrustDirection;
            RestoreGimbalRotation ();
            return referenceFrame.DirectionFromWorldSpace (direction).ToTuple ();
        }

        /// <summary>
        /// A reference frame that is fixed relative to the thruster and orientated with
        /// its thrust direction (<see cref="ThrustDirection"/>).
        /// For gimballed engines, this takes into account the current rotation of the gimbal.
        /// <list type="bullet">
        /// <item><description>
        /// The origin is at the position of thrust for this thruster
        /// (<see cref="ThrustPosition"/>).</description></item>
        /// <item><description>
        /// The axes rotate with the thrust direction.
        /// This is the direction in which the thruster expels propellant, including any gimballing.
        /// </description></item>
        /// <item><description>The y-axis points along the thrust direction.</description></item>
        /// <item><description>The x-axis and z-axis are perpendicular to the thrust direction.
        /// </description></item>
        /// </list>
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ThrustReferenceFrame {
            get { return ReferenceFrame.Thrust (this); }
        }

        /// <summary>
        /// Whether the thruster is gimballed.
        /// </summary>
        [KRPCProperty]
        public bool Gimballed {
            get { return gimbal != null; }
        }

        void CheckGimballed ()
        {
            if (!Gimballed)
                throw new InvalidOperationException ("The engine is not gimballed");
        }

        /// <summary>
        /// Position around which the gimbal pivots.
        /// </summary>
        /// <returns>The position as a vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// position vector is in.</param>
        [KRPCMethod]
        public Tuple3 GimbalPosition (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            CheckGimballed ();
            return referenceFrame.PositionFromWorldSpace (gimbal.gimbalTransforms [transformIndex].position).ToTuple ();
        }

        /// <summary>
        /// Take control over the thruster's gimbal angle.
        /// </summary>
        /// <returns>The gimbal control handle.</returns>
        [KRPCMethod]
        public ThrusterGimbalControl GimbalControl() 
        {
            return new ThrusterGimbalControl(gimbal);
        }

        /// <summary>
        /// The current gimbal angle in the pitch, roll and yaw axes, in degrees.
        /// </summary>
        [KRPCProperty]
        public Tuple3 GimbalAngle {
            get {
                CheckGimballed ();
                return gimbal.actuation.ToTuple ();
            }
        }

        /// <summary>
        /// Transform of the thrust vector in world space.
        /// </summary>
        internal Transform WorldTransform {
            get { return (engine != null ? engine.thrustTransforms : rcs.thrusterTransforms) [transformIndex]; }
        }

        /// <summary>
        /// The direction of the thrust vector in world space.
        /// </summary>
        internal Vector3d WorldThrustDirection {
            get {
                var transform = WorldTransform;
                return (rcs != null && !rcs.useZaxis) ? -transform.up : -transform.forward;
            }
        }

        /// <summary>
        /// A direction perpendicular to <see cref="WorldThrustDirection"/>.
        /// </summary>
        internal Vector3d WorldThrustPerpendicularDirection {
            get { return WorldTransform.right; }
        }

        Quaternion savedRotation;

        /// <summary>
        /// Save the gimbal rotation and set it to its initial position.
        /// </summary>
        void StashGimbalRotation ()
        {
            savedRotation = gimbal.gimbalTransforms [transformIndex].localRotation;
            gimbal.gimbalTransforms [transformIndex].localRotation = gimbal.initRots [transformIndex];
        }

        /// <summary>
        /// Restore the previously saved gimbal rotation.
        /// </summary>
        void RestoreGimbalRotation ()
        {
            gimbal.gimbalTransforms [transformIndex].localRotation = savedRotation;
        }
    }
}
