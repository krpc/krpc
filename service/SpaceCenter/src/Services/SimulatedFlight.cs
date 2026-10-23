using System;
using System.Linq;
using System.Runtime.CompilerServices;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Used to simulate the vessel in the editor in flight. The counterpart of
    /// <see cref="Flight"/>. All of the information returned by this class is given in
    /// the reference frame passed to <see cref="EditorVessel.Flight"/>, or in
    /// <see cref="SurfaceReferenceFrame"/> if none was passed.
    /// Obtained by calling <see cref="EditorVessel.Flight"/>.
    /// </summary>
    /// <remarks>
    /// The simulated attitude is set by <see cref="SurfaceReferenceFrame"/>, and belongs to
    /// this object. The vessel is read from the editor on each call, so the object follows
    /// every edit.
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Editor)]
    public class SimulatedFlight : Equatable<SimulatedFlight>, IGameObjectState
    {
        // Null to follow the simulated surface frame, as a vessel's flight object follows
        // the surface it flies over
        readonly ReferenceFrame referenceFrame;
        ReferenceFrame surfaceReferenceFrame;

        internal SimulatedFlight (ReferenceFrame referenceFrame)
        {
            this.referenceFrame = referenceFrame;
            surfaceReferenceFrame = ReferenceFrame.EditorSurface ();
        }

        /// <summary>
        /// The state of the editor vessel and of the frame the object is measured in.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                var state = EditorExtensions.ShipState;
                if (!ReferenceEquals (referenceFrame, null))
                    state = state.LeastAlive (referenceFrame.GameObjectState);
                return state;
            }
        }

        /// <summary>
        /// Returns true if the objects are equal. Each object holds its own simulated
        /// attitude, so an object is only equal to itself.
        /// </summary>
        public override bool Equals (SimulatedFlight other)
        {
            return ReferenceEquals (this, other);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return RuntimeHelpers.GetHashCode (this);
        }

        /// <summary>
        /// The reference frame of the surface the vessel is simulated over. Its x-axis
        /// points upwards, and gravity acts along the negative x-axis. Defaults to
        /// <see cref="EditorVessel.SurfaceReferenceFrame"/>.
        /// </summary>
        /// <remarks>
        /// Only the rotation of the frame is used. A frame created relative to
        /// <see cref="EditorVessel.ReferenceFrame"/> follows the vessel as it is edited.
        /// </remarks>
        [KRPCProperty]
        public ReferenceFrame SurfaceReferenceFrame {
            get { return surfaceReferenceFrame; }
            set {
                if (ReferenceEquals (value, null))
                    throw new ArgumentNullException (nameof (value));
                surfaceReferenceFrame = value;
            }
        }

        static ShipConstruct Ship {
            get { return EditorExtensions.GetShip (); }
        }

        static Transform VesselTransform {
            get { return new EditorVessel ().VesselTransform; }
        }

        ReferenceFrame OutputFrame {
            get { return referenceFrame ?? surfaceReferenceFrame; }
        }

        /// <summary>
        /// Rotation of the vessel in the output frame.
        /// </summary>
        QuaternionD VesselRotation {
            get { return OutputFrame.RotationFromWorldSpace (VesselTransform.rotation); }
        }

        /// <summary>
        /// The position of the center of mass of the vessel,
        /// in the reference frame <see cref="ReferenceFrame"/>
        /// </summary>
        /// <returns>The position as a vector.</returns>
        [KRPCProperty]
        public Tuple3 CenterOfMass {
            get { return OutputFrame.PositionFromWorldSpace (Ship.WorldCenterOfMass ()).ToTuple (); }
        }

        /// <summary>
        /// The rotation of the vessel, in the reference frame <see cref="ReferenceFrame"/>
        /// </summary>
        /// <returns>The rotation as a quaternion of the form <c>(x, y, z, w)</c>.</returns>
        [KRPCProperty]
        public Tuple4 Rotation {
            get { return VesselRotation.ToTuple (); }
        }

        /// <summary>
        /// The direction that the vessel is pointing in,
        /// in the reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
        [KRPCProperty]
        public Tuple3 Direction {
            get { return OutputFrame.DirectionFromWorldSpace (VesselTransform.up).normalized.ToTuple (); }
        }

        /// <summary>
        /// The pitch of the vessel relative to the horizon, in degrees.
        /// A value between -90° and +90°.
        /// </summary>
        /// <remarks>
        /// An absolute Euler angle, ill-conditioned when the vessel points near vertical (a pitch
        /// near -90° or +90°). For an always-defined attitude use <see cref="Rotation"/> or
        /// <see cref="Direction"/>.
        /// </remarks>
        [KRPCProperty]
        public float Pitch {
            get { return (float)VesselRotation.PitchHeadingRoll ().x; }
        }

        /// <summary>
        /// The roll of the vessel relative to the horizon, in degrees.
        /// A value between -180° and +180°.
        /// </summary>
        /// <remarks>
        /// An absolute Euler angle, ill-conditioned when the vessel points near vertical (a pitch
        /// near -90° or +90°). For an always-defined attitude use <see cref="Rotation"/>.
        /// </remarks>
        [KRPCProperty]
        public float Roll {
            get { return (float)VesselRotation.PitchHeadingRoll ().z; }
        }

        /// <summary>
        /// The volume of water the vessel displaces when fully submerged, in
        /// <unit>m^3</unit>.
        /// </summary>
        [KRPCProperty]
        public double Displacement {
            get { return Ship.Parts.Sum (StockBuoyancy.Displacement); }
        }

        /// <summary>
        /// The point the buoyant force acts through with the vessel fully submerged, in
        /// reference frame <see cref="ReferenceFrame"/>. Returns <c>null</c> if no part of
        /// the vessel displaces any water.
        /// </summary>
        /// <returns>The position as a vector.</returns>
        /// <remarks>
        /// A hull at rest carries this on the same vertical line as
        /// <see cref="CenterOfMass"/>.
        /// </remarks>
        [KRPCProperty (Nullable = true)]
        public Tuple3 FullySubmergedCenterOfBuoyancy {
            get {
                var center = StockBuoyancy.FullySubmerged (Ship.Parts).CenterOfBuoyancy;
                if (!center.HasValue)
                    return null;
                return OutputFrame.PositionFromWorldSpace (center.Value).ToTuple ();
            }
        }

        /// <summary>
        /// The buoyant force on the vessel when fully submerged in a fluid of the given
        /// density, in Newtons.
        /// </summary>
        /// <param name="density">The density of the fluid, in <unit>kg/m^3</unit>.</param>
        /// <param name="gravity">The acceleration due to gravity, in <unit>m/s^2</unit>.</param>
        [KRPCMethod]
        public double BuoyantForceAt (double density, double gravity)
        {
            return Ship.Parts.Sum (
                part => StockBuoyancy.BuoyantForceAt (part, density * 0.001d, gravity)) * 1000d;
        }

        /// <summary>
        /// The torque the buoyant force exerts about the vessel's center of mass when fully
        /// submerged in a fluid of the given density, in reference frame
        /// <see cref="ReferenceFrame"/>. Gravity acts along the negative x-axis of
        /// <see cref="SurfaceReferenceFrame"/>.
        /// </summary>
        /// <returns>A vector pointing along the axis of the torque, with its magnitude
        /// equal to the strength of the torque in <unit>N.m</unit>.</returns>
        /// <param name="density">The density of the fluid, in <unit>kg/m^3</unit>.</param>
        /// <param name="gravity">The acceleration due to gravity, in <unit>m/s^2</unit>.</param>
        [KRPCMethod]
        public Tuple3 BuoyantTorqueAt (double density, double gravity)
        {
            var ship = Ship;
            var down = -surfaceReferenceFrame.DirectionToWorldSpace (Vector3d.right);
            var torque = StockBuoyancy.FullySubmerged (ship.Parts).Torque (
                density * 0.001d, gravity, down, ship.WorldCenterOfMass ());
            return OutputFrame.DirectionFromWorldSpace (torque * 1000d).ToTuple ();
        }
    }
}
