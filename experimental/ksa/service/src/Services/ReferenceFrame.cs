using System;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A reference frame, in which positions, directions, rotations and velocities are given.
    /// Obtained from the reference frame properties of <see cref="Vehicle"/> and
    /// <see cref="Body"/>, or created with <see cref="CreateRelative"/> and
    /// <see cref="CreateHybrid"/>.
    /// </summary>
    /// <remarks>
    /// Every reference frame is right-handed. Positions are in meters, velocities in meters
    /// per second, and angular velocities in radians per second. A rotation is a quaternion
    /// <c>(x, y, z, w)</c>.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class ReferenceFrame : Equatable<ReferenceFrame>, IGameObjectState
    {
        internal enum Kind
        {
            Body,
            BodyNonRotating,
            Vehicle,
            VehicleSurface,
            VehicleOrbital,
            Burn,
            Relative,
            Hybrid
        }

        readonly Kind kind;
        readonly IParentBody body;
        readonly global::KSA.Vehicle vehicle;
        readonly Burn burn;
        // A relative frame's parent and offsets, in the parent frame
        readonly ReferenceFrame parent;
        readonly double3 position;
        readonly doubleQuat rotation;
        readonly double3 velocity;
        readonly double3 angularVelocity;
        // The frames a hybrid frame takes its components from
        readonly ReferenceFrame positionFrame;
        readonly ReferenceFrame rotationFrame;
        readonly ReferenceFrame velocityFrame;
        readonly ReferenceFrame angularVelocityFrame;

        internal ReferenceFrame (Kind kind, IParentBody body)
        {
            this.kind = kind;
            this.body = body;
        }

        internal ReferenceFrame (Kind kind, global::KSA.Vehicle vehicle)
        {
            this.kind = kind;
            this.vehicle = vehicle;
        }

        internal ReferenceFrame (Burn burn)
        {
            kind = Kind.Burn;
            this.burn = burn;
        }

        ReferenceFrame (ReferenceFrame parent, double3 position, doubleQuat rotation, double3 velocity, double3 angularVelocity)
        {
            kind = Kind.Relative;
            this.parent = parent;
            this.position = position;
            this.rotation = rotation;
            this.velocity = velocity;
            this.angularVelocity = angularVelocity;
        }

        ReferenceFrame (ReferenceFrame position, ReferenceFrame rotation, ReferenceFrame velocity, ReferenceFrame angularVelocity)
        {
            kind = Kind.Hybrid;
            positionFrame = position;
            rotationFrame = rotation;
            velocityFrame = velocity;
            angularVelocityFrame = angularVelocity;
        }

        static class VectorZero
        {
            public static object Create ()
            {
                return Tuple.Create (0d, 0d, 0d);
            }
        }

        static class QuaternionIdentity
        {
            public static object Create ()
            {
                return Tuple.Create (0d, 0d, 0d, 1d);
            }
        }

        /// <summary>
        /// Create a reference frame that is offset from a parent frame.
        /// </summary>
        /// <param name="referenceFrame">The parent reference frame.</param>
        /// <param name="position">The position of the origin in the parent frame, in meters.
        /// Defaults to <c>(0, 0, 0)</c>.</param>
        /// <param name="rotation">The rotation of the frame relative to the parent frame.
        /// Defaults to <c>(0, 0, 0, 1)</c>, no rotation.</param>
        /// <param name="velocity">The velocity of the origin in the parent frame, in meters
        /// per second. Defaults to <c>(0, 0, 0)</c>.</param>
        /// <param name="angularVelocity">The angular velocity of the frame relative to the
        /// parent frame, in radians per second, in the axes of the parent frame. Defaults to
        /// <c>(0, 0, 0)</c>.</param>
        [KRPCMethod]
        public static ReferenceFrame CreateRelative (
            ReferenceFrame referenceFrame,
            [KRPCDefaultValue (typeof (VectorZero))] Tuple<double, double, double> position,
            [KRPCDefaultValue (typeof (QuaternionIdentity))] Tuple<double, double, double, double> rotation,
            [KRPCDefaultValue (typeof (VectorZero))] Tuple<double, double, double> velocity,
            [KRPCDefaultValue (typeof (VectorZero))] Tuple<double, double, double> angularVelocity)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return new ReferenceFrame (
                referenceFrame, Vectors.ToVector (position), Vectors.ToRotation (rotation),
                Vectors.ToVector (velocity), Vectors.ToVector (angularVelocity));
        }

        /// <summary>
        /// Create a reference frame that takes its components from other frames.
        /// </summary>
        /// <param name="position">The reference frame that gives the origin.</param>
        /// <param name="rotation">The reference frame that gives the axes. Defaults to
        /// <paramref name="position"/>.</param>
        /// <param name="velocity">The reference frame that gives the velocity of the origin.
        /// Defaults to <paramref name="position"/>.</param>
        /// <param name="angularVelocity">The reference frame that gives the angular velocity.
        /// Defaults to <paramref name="position"/>.</param>
        [KRPCMethod]
        public static ReferenceFrame CreateHybrid (
            ReferenceFrame position, ReferenceFrame rotation = null,
            ReferenceFrame velocity = null, ReferenceFrame angularVelocity = null)
        {
            if (ReferenceEquals (position, null))
                throw new ArgumentNullException (nameof (position));
            return new ReferenceFrame (position, rotation ?? position, velocity ?? position, angularVelocity ?? position);
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ReferenceFrame other)
        {
            return !ReferenceEquals (other, null) && kind == other.kind &&
                ReferenceEquals (body, other.body) && ReferenceEquals (vehicle, other.vehicle) &&
                Equals (burn, other.burn) &&
                Equals (parent, other.parent) && position == other.position &&
                rotation == other.rotation && velocity == other.velocity &&
                angularVelocity == other.angularVelocity &&
                Equals (positionFrame, other.positionFrame) && Equals (rotationFrame, other.rotationFrame) &&
                Equals (velocityFrame, other.velocityFrame) &&
                Equals (angularVelocityFrame, other.angularVelocityFrame);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return State; }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            var hash = new HashCode ();
            hash.Add (kind);
            hash.Add (body);
            hash.Add (vehicle);
            hash.Add (burn);
            hash.Add (parent);
            hash.Add (position);
            hash.Add (rotation);
            hash.Add (velocity);
            hash.Add (angularVelocity);
            hash.Add (positionFrame);
            hash.Add (rotationFrame);
            hash.Add (velocityFrame);
            hash.Add (angularVelocityFrame);
            return hash.ToHashCode ();
        }

        GameObjectState State {
            get {
                switch (kind) {
                case Kind.Relative:
                    return parent.GameObjectState;
                case Kind.Hybrid:
                    return positionFrame.GameObjectState.LeastAlive (rotationFrame.GameObjectState)
                        .LeastAlive (velocityFrame.GameObjectState).LeastAlive (angularVelocityFrame.GameObjectState);
                case Kind.Burn:
                    return burn.GameObjectState;
                default:
                    return body != null ? Vehicles.StateOf (body) : Vehicles.StateOf (vehicle);
                }
            }
        }

        global::KSA.Vehicle LiveVehicle {
            get { return new Vehicle (vehicle).InternalVehicle; }
        }

        // The frame's origin, velocity, rotation and angular velocity in the game's ecliptic
        // frame. Ecliptic axes are shared by every body-centered ecliptic (Cce) frame.

        internal double3 OriginEcl {
            get {
                switch (kind) {
                case Kind.Relative:
                    return parent.PositionToEcl (position);
                case Kind.Hybrid:
                    return positionFrame.OriginEcl;
                case Kind.Burn:
                    return burn.PositionEcl;
                default:
                    return body != null ? body.GetPositionEcl () : LiveVehicle.GetPositionEcl ();
                }
            }
        }

        internal double3 VelocityEcl {
            get {
                switch (kind) {
                case Kind.Relative:
                    return parent.VelocityToEcl (parent.PositionToEcl (position), velocity);
                case Kind.Hybrid:
                    return velocityFrame.VelocityEcl;
                case Kind.Burn:
                    return burn.Body.GetVelocityEcl ();
                default:
                    return body != null ? body.GetVelocityEcl () : LiveVehicle.GetVelocityEcl ();
                }
            }
        }

        internal doubleQuat Rotation2Ecl {
            get {
                switch (kind) {
                case Kind.Relative:
                    return parent.RotationToEcl (rotation);
                case Kind.Hybrid:
                    return rotationFrame.Rotation2Ecl;
                case Kind.Burn:
                    return burn.Vlf2Ecl;
                case Kind.Body:
                    return body.GetCcf2Cce ();
                case Kind.BodyNonRotating:
                    return body.GetCci2Cce ();
                case Kind.Vehicle:
                    return LiveVehicle.GetHelm2Cce ();
                case Kind.VehicleSurface: {
                    var v = LiveVehicle;
                    var enu2Cci = VehicleReferenceFrameEx.GetEnu2Cci (v.GetPositionCci ())
                        ?? throw new InvalidOperationException ("The vehicle is at the center of the body");
                    return doubleQuat.Concatenate (enu2Cci, v.Orbit.Parent.GetCci2Cce ());
                }
                default: {
                    var v = LiveVehicle;
                    var vlf2Cci = VehicleReferenceFrameEx.GetVlf2Cci (v.GetPositionCci (), v.GetVelocityCci ())
                        ?? throw new InvalidOperationException ("The vehicle has no orbital direction");
                    return doubleQuat.Concatenate (vlf2Cci, v.Orbit.Parent.GetCci2Cce ());
                }
                }
            }
        }

        internal double3 AngularVelocityEcl {
            get {
                switch (kind) {
                case Kind.Relative:
                    return parent.AngularVelocityEcl + parent.DirectionToEcl (angularVelocity);
                case Kind.Hybrid:
                    return angularVelocityFrame.AngularVelocityEcl;
                case Kind.Burn:
                    return double3.Zero;
                case Kind.Body:
                    return body.GetAngularVelocityCce ();
                case Kind.BodyNonRotating:
                    return double3.Zero;
                case Kind.Vehicle:
                    return Vectors.AngularVelocityEcl (LiveVehicle);
                case Kind.VehicleSurface:
                    return LiveVehicle.Orbit.Parent.GetAngularVelocityCce ();
                default: {
                    // The orbital direction turns at the vehicle's orbital angular rate
                    var v = LiveVehicle;
                    var position = v.GetPositionCci ();
                    var rate = double3.Cross (position, v.GetVelocityCci ()) / position.LengthSquared ();
                    return rate.Transform (v.Orbit.Parent.GetCci2Cce ());
                }
                }
            }
        }

        internal double3 PositionFromEcl (double3 positionEcl)
        {
            return (positionEcl - OriginEcl).Transform (Rotation2Ecl.Inverse ());
        }

        internal double3 PositionToEcl (double3 position)
        {
            return position.Transform (Rotation2Ecl) + OriginEcl;
        }

        internal double3 DirectionFromEcl (double3 directionEcl)
        {
            return directionEcl.Transform (Rotation2Ecl.Inverse ());
        }

        internal double3 DirectionToEcl (double3 direction)
        {
            return direction.Transform (Rotation2Ecl);
        }

        internal double3 VelocityFromEcl (double3 positionEcl, double3 velocityEcl)
        {
            var relative = velocityEcl - VelocityEcl - double3.Cross (AngularVelocityEcl, positionEcl - OriginEcl);
            return relative.Transform (Rotation2Ecl.Inverse ());
        }

        internal double3 VelocityToEcl (double3 positionEcl, double3 velocity)
        {
            return velocity.Transform (Rotation2Ecl) + VelocityEcl + double3.Cross (AngularVelocityEcl, positionEcl - OriginEcl);
        }

        internal doubleQuat RotationFromEcl (doubleQuat rotation2Ecl)
        {
            return doubleQuat.Concatenate (rotation2Ecl, Rotation2Ecl.Inverse ());
        }

        internal doubleQuat RotationToEcl (doubleQuat rotation)
        {
            return doubleQuat.Concatenate (rotation, Rotation2Ecl);
        }

        internal double3 AngularVelocityFromEcl (double3 angularVelocityEcl)
        {
            return (angularVelocityEcl - AngularVelocityEcl).Transform (Rotation2Ecl.Inverse ());
        }
    }
}
