using System;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A gimbal that steers an engine nozzle. Obtained from <see cref="Part.Gimbals"/> or
    /// <see cref="Engine.Gimbals"/>.
    /// </summary>
    /// <remarks>
    /// A gimbal turns about the y and z axes of its mount, which <see cref="Rotation"/> gives.
    /// The flight computer steers it while an engine it belongs to is <see cref="Active"/>.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class Gimbal : Equatable<Gimbal>, IGameObjectState
    {
        readonly global::KSA.Gimbal gimbal;

        internal Gimbal (Part part, global::KSA.Gimbal gimbal)
        {
            Part = part;
            this.gimbal = gimbal;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Gimbal other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (gimbal, other.gimbal);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return Part.GameObjectState; }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return gimbal.GetHashCode ();
        }

        /// <summary>
        /// The part the gimbal is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        ref readonly GimbalState State {
            get {
                var vehicle = Part.InternalVehicle;
                if (!ModuleStateful<global::KSA.Gimbal, GimbalState, EmptyStruct, EmptyStruct>.TryGetFrom (
                    vehicle.Parts.States, out var states))
                    throw new InvalidOperationException ("The vehicle has no gimbals");
                return ref states.GetState (gimbal);
            }
        }

        /// <summary>
        /// The angle of the gimbal about its y axis, in degrees.
        /// </summary>
        [KRPCProperty]
        public double AngleY {
            get { return Degrees (State.AngleY); }
        }

        /// <summary>
        /// The angle of the gimbal about its z axis, in degrees.
        /// </summary>
        [KRPCProperty]
        public double AngleZ {
            get { return Degrees (State.AngleZ); }
        }

        /// <summary>
        /// The largest angle of the gimbal about its y axis, in degrees. Zero if it does not
        /// turn about that axis.
        /// </summary>
        [KRPCProperty]
        public double MaxAngleY {
            get {
                _ = Part.InternalPart;
                return Degrees (gimbal.AxisY.MaxAngle);
            }
        }

        /// <summary>
        /// The largest angle of the gimbal about its z axis, in degrees. Zero if it does not
        /// turn about that axis.
        /// </summary>
        [KRPCProperty]
        public double MaxAngleZ {
            get {
                _ = Part.InternalPart;
                return Degrees (gimbal.AxisZ.MaxAngle);
            }
        }

        /// <summary>
        /// Whether an engine the gimbal steers is active.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get {
                _ = Part.InternalPart;
                return gimbal.Controller?.HasActiveEngine () ?? false;
            }
        }

        /// <summary>
        /// The rotation of the gimbal's mount, whose y and z axes the gimbal turns about.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the rotation.</param>
        [KRPCMethod]
        public Tuple<double, double, double, double> Rotation (ReferenceFrame referenceFrame)
        {
            var vehicle = Part.InternalVehicle;
            var gimbal2Vehicle = doubleQuat.Concatenate (gimbal.Gimbal2Asmb, gimbal.Parent.Asmb2VehicleAsmb);
            var gimbal2Ecl = doubleQuat.Concatenate (gimbal2Vehicle, vehicle.GetBodyFixed2Ecl ());
            return Vectors.ToTuple (referenceFrame.RotationFromEcl (gimbal2Ecl));
        }

        // Whether the gimbal steers a nozzle of the engine
        internal bool Steers (EngineController engine)
        {
            var controller = gimbal.Controller;
            if (controller == null)
                return false;
            foreach (var nozzle in controller.Data.Nozzles) {
                if (ReferenceEquals (nozzle.Rocket?.Core.Controller, engine))
                    return true;
            }
            return false;
        }

        static double Degrees (double radians)
        {
            return radians * (180d / Math.PI);
        }
    }
}
