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
    /// <see cref="Body"/>.
    /// </summary>
    /// <remarks>
    /// Every reference frame is right-handed. Positions are in meters, velocities in meters
    /// per second, and angular velocities in radians per second. A rotation is a quaternion
    /// <c>(x, y, z, w)</c>.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class ReferenceFrame : Equatable<ReferenceFrame>
    {
        internal enum Kind
        {
            Body,
            BodyNonRotating,
            Vehicle,
            VehicleSurface,
            VehicleOrbital
        }

        readonly Kind kind;
        readonly IParentBody body;
        readonly global::KSA.Vehicle vehicle;

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

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ReferenceFrame other)
        {
            return !ReferenceEquals (other, null) && kind == other.kind &&
                ReferenceEquals (body, other.body) && ReferenceEquals (vehicle, other.vehicle);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return HashCode.Combine (kind, body, vehicle);
        }

        global::KSA.Vehicle LiveVehicle {
            get { return new Vehicle (vehicle).InternalVehicle; }
        }

        // The frame's origin, velocity, rotation and angular velocity in the game's ecliptic
        // frame. Ecliptic axes are shared by every body-centered ecliptic (Cce) frame.

        internal double3 OriginEcl {
            get { return body != null ? body.GetPositionEcl () : LiveVehicle.GetPositionEcl (); }
        }

        internal double3 VelocityEcl {
            get { return body != null ? body.GetVelocityEcl () : LiveVehicle.GetVelocityEcl (); }
        }

        internal doubleQuat Rotation2Ecl {
            get {
                switch (kind) {
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
