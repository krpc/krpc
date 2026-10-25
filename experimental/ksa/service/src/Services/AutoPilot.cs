using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The vehicle's flight computer, which steers the vehicle with its reaction control
    /// thrusters and engine gimbals. Obtained from <see cref="Vehicle.AutoPilot"/>.
    /// </summary>
    /// <remarks>
    /// The auto-pilot points the vehicle in the direction set by <see cref="Mode"/>, once
    /// engaged. It is the game's own flight computer, so the game's flight computer
    /// controls change the same settings.
    /// </remarks>
    [KRPCClass (Service = "KSA", GameScene = GameScene.Flight)]
    public class AutoPilot : Equatable<AutoPilot>
    {
        // The number of updates Wait lets pass before reading the pointing error. The
        // error is computed by the solver step that follows a server update, so after a
        // change of target the first update reads the error for the previous target
        const int WaitSettleUpdates = 2;

        readonly global::KSA.Vehicle vehicle;

        internal AutoPilot (global::KSA.Vehicle vehicle)
        {
            this.vehicle = vehicle;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (AutoPilot other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (vehicle, other.vehicle);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return vehicle.GetHashCode ();
        }

        // Writes take effect from the next solver step, as the server runs between the
        // previous step's results being applied and the next step being queued
        FlightComputer Computer {
            get { return new Vehicle (vehicle).InternalVehicle.FlightComputer; }
        }

        /// <summary>
        /// Engage the auto-pilot.
        /// </summary>
        [KRPCMethod]
        public void Engage ()
        {
            Computer.AttitudeMode = FlightComputerAttitudeMode.Auto;
        }

        /// <summary>
        /// Disengage the auto-pilot.
        /// </summary>
        [KRPCMethod]
        public void Disengage ()
        {
            Computer.AttitudeMode = FlightComputerAttitudeMode.Manual;
        }

        /// <summary>
        /// Whether the auto-pilot is engaged.
        /// </summary>
        /// <remarks>
        /// The same setting as <see cref="Control.Stabilization"/>.
        /// </remarks>
        [KRPCProperty]
        public bool Engaged {
            get { return Computer.AttitudeMode == FlightComputerAttitudeMode.Auto; }
        }

        /// <summary>
        /// The direction the auto-pilot points the vehicle in.
        /// </summary>
        /// <remarks>
        /// Setting <see cref="AutoPilotMode.Attitude"/> targets the vehicle's current pitch,
        /// heading and roll. Setting <see cref="AutoPilotMode.Other"/> throws an exception.
        /// </remarks>
        [KRPCProperty]
        public AutoPilotMode Mode {
            get { return GetMode (Computer); }
            set {
                var computer = Computer;
                switch (value) {
                case AutoPilotMode.Attitude:
                    if (GetMode (computer) != AutoPilotMode.Attitude)
                        SetAttitude (computer, CurrentAttitude ());
                    break;
                case AutoPilotMode.Hold:
                    computer.SetInertialNullRot ();
                    break;
                case AutoPilotMode.Other:
                    throw new ArgumentException ("The mode cannot be set to Other");
                default:
                    computer.AttitudeTrackTarget = ToTrackTarget (value);
                    break;
                }
            }
        }

        /// <summary>
        /// The target pitch, in degrees, between -90° and +90°.
        /// </summary>
        /// <remarks>
        /// Setting it sets <see cref="Mode"/> to <see cref="AutoPilotMode.Attitude"/>.
        /// Returns NaN in any other mode.
        /// </remarks>
        [KRPCProperty]
        public double TargetPitch {
            get { return TargetAngle (1); }
            set {
                CheckPitch (value);
                SetTargetAngle (1, value);
            }
        }

        /// <summary>
        /// The target heading, in degrees, between 0° and 360°.
        /// </summary>
        /// <remarks>
        /// Setting it sets <see cref="Mode"/> to <see cref="AutoPilotMode.Attitude"/>.
        /// Returns NaN in any other mode.
        /// </remarks>
        [KRPCProperty]
        public double TargetHeading {
            get {
                if (GetMode (Computer) != AutoPilotMode.Attitude)
                    return double.NaN;
                return Flight.YawToHeading (Computer.CustomAttitudeTarget.Z);
            }
            set {
                CheckFinite (value);
                var computer = Computer;
                var target = GetMode (computer) == AutoPilotMode.Attitude
                    ? computer.CustomAttitudeTarget : CurrentAttitude ();
                target.Z = Flight.HeadingToYaw (value);
                SetAttitude (computer, target);
            }
        }

        /// <summary>
        /// The target roll, in degrees. NaN when the auto-pilot does not control roll.
        /// </summary>
        /// <remarks>
        /// Setting a number sets <see cref="Mode"/> to <see cref="AutoPilotMode.Attitude"/>.
        /// Setting NaN leaves roll free, in every mode. Returns NaN in modes other than
        /// <see cref="AutoPilotMode.Attitude"/>.
        /// </remarks>
        [KRPCProperty]
        public double TargetRoll {
            get {
                if (Computer.RollMode == FlightComputerRollMode.Decoupled)
                    return double.NaN;
                return TargetAngle (0);
            }
            set {
                if (double.IsNaN (value)) {
                    Computer.RollMode = FlightComputerRollMode.Decoupled;
                    return;
                }
                SetTargetAngle (0, value);
                Computer.RollMode = FlightComputerRollMode.Up;
            }
        }

        /// <summary>
        /// Set the target pitch and heading, and the mode to
        /// <see cref="AutoPilotMode.Attitude"/>.
        /// </summary>
        /// <param name="pitch">Target pitch, in degrees, between -90° and +90°.</param>
        /// <param name="heading">Target heading, in degrees, between 0° and 360°.</param>
        [KRPCMethod]
        public void TargetPitchAndHeading (double pitch, double heading)
        {
            CheckPitch (pitch);
            CheckFinite (heading);
            var computer = Computer;
            var target = GetMode (computer) == AutoPilotMode.Attitude
                ? computer.CustomAttitudeTarget : CurrentAttitude ();
            target.Y = DegreesToRadians (pitch);
            target.Z = Flight.HeadingToYaw (heading);
            SetAttitude (computer, target);
        }

        /// <summary>
        /// The angle between the direction the vehicle points in and the target direction,
        /// in degrees. NaN when the auto-pilot is not engaged.
        /// </summary>
        [KRPCProperty]
        public double Error {
            get {
                var computer = Computer;
                if (computer.AttitudeMode != FlightComputerAttitudeMode.Auto)
                    return double.NaN;
                var errors = computer.ErrorAngles;
                return RadiansToDegrees (Math.Sqrt (errors.Y * errors.Y + errors.Z * errors.Z));
            }
        }

        /// <summary>
        /// The angle between the vehicle's roll and the target roll, in degrees.
        /// NaN when the auto-pilot is not engaged.
        /// </summary>
        [KRPCProperty]
        public double RollError {
            get {
                var computer = Computer;
                if (computer.AttitudeMode != FlightComputerAttitudeMode.Auto)
                    return double.NaN;
                return RadiansToDegrees (Math.Abs (computer.ErrorAngles.X));
            }
        }

        /// <summary>
        /// Wait until the vehicle points in the target direction, and has the target roll
        /// if it has one. Returns when the errors are within twice the flight computer's
        /// angle deadband, which defaults to 1°.
        /// Throws an exception if the auto-pilot is not engaged.
        /// </summary>
        [KRPCMethod]
        public void Wait ()
        {
            WaitForTarget (WaitSettleUpdates);
        }

        void WaitForTarget (int updates)
        {
            var computer = Computer;
            if (computer.AttitudeMode != FlightComputerAttitudeMode.Auto)
                throw new InvalidOperationException ("The auto-pilot is not engaged");
            var tolerance = RadiansToDegrees (2 * computer.AngleDeadband);
            var rollDone = computer.RollMode == FlightComputerRollMode.Decoupled || RollError <= tolerance;
            if (updates > 0 || !(Error <= tolerance) || !rollDone)
                throw new YieldException<Action> (() => WaitForTarget (Math.Max (0, updates - 1)));
        }

        static AutoPilotMode GetMode (FlightComputer computer)
        {
            switch (computer.AttitudeTrackTarget) {
            case FlightComputerAttitudeTrackTarget.Custom:
                return computer.AttitudeFrame == VehicleReferenceFrame.EnuBody
                    ? AutoPilotMode.Attitude : AutoPilotMode.Other;
            case FlightComputerAttitudeTrackTarget.None:
                return AutoPilotMode.Hold;
            case FlightComputerAttitudeTrackTarget.Prograde:
                return AutoPilotMode.Prograde;
            case FlightComputerAttitudeTrackTarget.Retrograde:
                return AutoPilotMode.Retrograde;
            case FlightComputerAttitudeTrackTarget.Normal:
                return AutoPilotMode.Normal;
            case FlightComputerAttitudeTrackTarget.AntiNormal:
                return AutoPilotMode.AntiNormal;
            case FlightComputerAttitudeTrackTarget.RadialOut:
                return AutoPilotMode.RadialOut;
            case FlightComputerAttitudeTrackTarget.RadialIn:
                return AutoPilotMode.RadialIn;
            case FlightComputerAttitudeTrackTarget.Forward:
                return AutoPilotMode.SurfacePrograde;
            case FlightComputerAttitudeTrackTarget.Backward:
                return AutoPilotMode.SurfaceRetrograde;
            default:
                return AutoPilotMode.Other;
            }
        }

        static FlightComputerAttitudeTrackTarget ToTrackTarget (AutoPilotMode mode)
        {
            switch (mode) {
            case AutoPilotMode.Prograde:
                return FlightComputerAttitudeTrackTarget.Prograde;
            case AutoPilotMode.Retrograde:
                return FlightComputerAttitudeTrackTarget.Retrograde;
            case AutoPilotMode.Normal:
                return FlightComputerAttitudeTrackTarget.Normal;
            case AutoPilotMode.AntiNormal:
                return FlightComputerAttitudeTrackTarget.AntiNormal;
            case AutoPilotMode.RadialOut:
                return FlightComputerAttitudeTrackTarget.RadialOut;
            case AutoPilotMode.RadialIn:
                return FlightComputerAttitudeTrackTarget.RadialIn;
            case AutoPilotMode.SurfacePrograde:
                return FlightComputerAttitudeTrackTarget.Forward;
            case AutoPilotMode.SurfaceRetrograde:
                return FlightComputerAttitudeTrackTarget.Backward;
            default:
                throw new ArgumentOutOfRangeException (nameof (mode));
            }
        }

        // The custom attitude target is (roll, pitch, yaw) in radians, in the game's attitude
        // frame. This is the convention of Flight.AttitudeOf
        static void SetAttitude (FlightComputer computer, Brutal.Numerics.double3 target)
        {
            computer.AttitudeFrame = VehicleReferenceFrame.EnuBody;
            computer.AttitudeTrackTarget = FlightComputerAttitudeTrackTarget.Custom;
            computer.CustomAttitudeTarget = target;
        }

        Brutal.Numerics.double3 CurrentAttitude ()
        {
            var attitude = Flight.AttitudeOf (new Vehicle (vehicle).InternalVehicle);
            if (double.IsNaN (attitude.X))
                throw new InvalidOperationException ("The vehicle has no attitude relative to the surface");
            return attitude;
        }

        double TargetAngle (int axis)
        {
            var computer = Computer;
            if (GetMode (computer) != AutoPilotMode.Attitude)
                return double.NaN;
            var target = computer.CustomAttitudeTarget;
            return RadiansToDegrees (axis == 0 ? target.X : axis == 1 ? target.Y : target.Z);
        }

        void SetTargetAngle (int axis, double degrees)
        {
            CheckFinite (degrees);
            var computer = Computer;
            var target = GetMode (computer) == AutoPilotMode.Attitude
                ? computer.CustomAttitudeTarget : CurrentAttitude ();
            var radians = DegreesToRadians (degrees);
            if (axis == 0)
                target.X = radians;
            else if (axis == 1)
                target.Y = radians;
            else
                target.Z = radians;
            SetAttitude (computer, target);
        }

        static void CheckPitch (double degrees)
        {
            if (!(Math.Abs (degrees) <= 90))
                throw new ArgumentOutOfRangeException (nameof (degrees), "The pitch is not between -90 and +90 degrees");
        }

        static void CheckFinite (double degrees)
        {
            if (!double.IsFinite (degrees))
                throw new ArgumentException ("The angle is not a finite number");
        }

        static double DegreesToRadians (double degrees)
        {
            return degrees * (Math.PI / 180d);
        }

        static double RadiansToDegrees (double radians)
        {
            return radians * (180d / Math.PI);
        }
    }
}
