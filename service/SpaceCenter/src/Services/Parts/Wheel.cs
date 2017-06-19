using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A wheel. Includes landing gear and rover wheels. Obtained by calling <see cref="Part.Wheel"/>.
    /// Can be used to control the motors, steering and deployment of wheels, among other things.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public class Wheel : Equatable<Wheel>
    {
        readonly ModuleWheelBase wheel;
        readonly ModuleWheels.ModuleWheelBrakes brakes;
        readonly ModuleWheels.ModuleWheelDamage damage;
        readonly ModuleWheels.ModuleWheelDeployment deployment;
        readonly ModuleWheels.ModuleWheelMotor motor;
        readonly ModuleWheels.ModuleWheelSteering steering;
        readonly ModuleWheels.ModuleWheelSuspension suspension;

        internal static bool Is(Part part)
        {
            var internalPart = part.InternalPart;
            return internalPart.HasModule<ModuleWheelBase>() &&
                   internalPart.Module<ModuleWheelBase>().wheelType != WheelType.LEG;
        }

        internal Wheel(Part part)
        {
            if (!Is(part))
                throw new ArgumentException("Part is not a wheel");
            Part = part;
            var internalPart = part.InternalPart;
            wheel = internalPart.Module<ModuleWheelBase>();
            brakes = internalPart.Module<ModuleWheels.ModuleWheelBrakes>();
            damage = internalPart.Module<ModuleWheels.ModuleWheelDamage>();
            deployment = internalPart.Module<ModuleWheels.ModuleWheelDeployment>();
            motor = internalPart.Module<ModuleWheels.ModuleWheelMotor> ();
            steering = internalPart.Module<ModuleWheels.ModuleWheelSteering>();
            suspension = internalPart.Module<ModuleWheels.ModuleWheelSuspension>();

        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(Wheel other)
        {
            return !ReferenceEquals(other, null) && Part == other.Part && wheel == other.wheel;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return Part.GetHashCode() ^ wheel.GetHashCode();
        }

        void CheckBrakes()
        {
            if (brakes == null)
                throw new InvalidOperationException("Wheel does not have brakes");
        }

        void CheckDeployment()
        {
            if (deployment == null)
                throw new InvalidOperationException("Wheel is not deployable");
        }

        void CheckMotor()
        {
            if (motor == null)
                throw new InvalidOperationException("Wheel is not powered");
        }

        void CheckSteering()
        {
            if (steering == null)
                throw new InvalidOperationException("Wheel is not steerable");
        }

        void CheckSuspension()
        {
            if (suspension == null)
                throw new InvalidOperationException("Wheel does not have suspension");
        }

        void CheckDamage()
        {
            if (damage == null)
                throw new InvalidOperationException("Wheel is not breakable");
        }

        /// <summary>
        /// The part object for this wheel.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// The current state of the wheel.
        /// </summary>
        [KRPCProperty]
        public WheelState State {
            get {
                if (damage != null && damage.isDamaged)
                    return WheelState.Broken;
                if (deployment != null) {
                    if (Math.Abs(deployment.position - deployment.deployedPosition) < 0.0001)
                        return WheelState.Deployed;
                    else if (Math.Abs(deployment.position - deployment.retractedPosition) < 0.0001)
                        return WheelState.Retracted;
                    else if (deployment.stateString.Equals(deployment.st_deploying.name))
                        return WheelState.Deploying;
                    else if (deployment.stateString.Equals(deployment.st_retracting.name))
                        return WheelState.Retracting;
                    throw new InvalidOperationException();
                }
                return WheelState.Deployed;
            }
        }

        /// <summary>
        /// Radius of the wheel, in meters.
        /// </summary>
        [KRPCProperty]
        public float Radius {
            get { return wheel.radius; }
        }

        /// <summary>
        /// Whether the wheel is touching the ground.
        /// </summary>
        [KRPCProperty]
        public bool Grounded {
            get { return wheel.isGrounded; }
        }

        /// <summary>
        /// Whether the wheel has brakes.
        /// </summary>
        [KRPCProperty]
        public bool HasBrakes {
            get { return brakes != null; }
        }

        /// <summary>
        /// The braking force, as a percentage of maximum, when the brakes are applied.
        /// </summary>
        [KRPCProperty]
        public float Brakes {
            get {
                CheckBrakes();
                return brakes.brakeTweakable;
            }
            set {
                CheckBrakes();
                brakes.brakeTweakable = value;
            }
        }

        /// <summary>
        /// Whether automatic friction control is enabled.
        /// </summary>
        [KRPCProperty]
        public bool AutoFrictionControl {
            get { return wheel.autoFriction; }
            set { wheel.autoFriction = value; }
        }

        /// <summary>
        /// Manual friction control value. Only has an effect if automatic friction control is disabled.
        /// A value between 0 and 5 inclusive.
        /// </summary>
        [KRPCProperty]
        public float ManualFrictionControl {
            get { return wheel.frictionMultiplier; }
            set { wheel.frictionMultiplier = value.Clamp(0, 5); }
        }

        /// <summary>
        /// Whether the wheel is deployable.
        /// </summary>
        [KRPCProperty]
        public bool Deployable {
            get { return deployment != null; }
        }

        /// <summary>
        /// Whether the wheel is deployed.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get { return State == WheelState.Deployed; }
            set {
                CheckDeployment();
                if (value) {
                    var extend = deployment.Events.FirstOrDefault(x => x.guiName == "Extend");
                    if (extend != null)
                        extend.Invoke();
                } else {
                    var retract = deployment.Events.FirstOrDefault(x => x.guiName == "Retract");
                    if (retract != null)
                        retract.Invoke();
                }
            }
        }

        /// <summary>
        /// Whether the wheel is powered by a motor.
        /// </summary>
        [KRPCProperty]
        public bool Powered {
            get { return motor != null; }
        }

        /// <summary>
        /// Whether the motor is enabled.
        /// </summary>
        [KRPCProperty]
        public bool MotorEnabled {
            get {
                CheckMotor();
                return motor.motorEnabled;
            }
            set {
                CheckMotor();
                motor.motorEnabled = value;
            }
        }

        /// <summary>
        /// Whether the direction of the motor is inverted.
        /// </summary>
        [KRPCProperty]
        public bool MotorInverted {
            get {
                CheckMotor();
                return motor.motorInverted;
            }
            set {
                CheckMotor();
                motor.motorInverted = value;
            }
        }

        /// <summary>
        /// Whether the direction of the motor is inverted.
        /// </summary>
        [KRPCProperty]
        public MotorState MotorState {
            get {
                CheckMotor();
                return motor.state.ToMotorState();
            }
        }

        /// <summary>
        /// The output of the motor. This is the torque currently being generated, in Newton meters.
        /// </summary>
        [KRPCProperty]
        public float MotorOutput {
            get {
                CheckMotor();
                return motor.driveOutput;
            }
        }

        /// <summary>
        /// Whether automatic traction control is enabled.
        /// A wheel only has traction control if it is powered.
        /// </summary>
        [KRPCProperty]
        public bool TractionControlEnabled {
            get {
                CheckMotor();
                return motor.autoTorque;
            }
            set {
                CheckMotor();
                motor.autoTorque = value;
            }
        }

        /// <summary>
        /// Setting for the traction control.
        /// Only takes effect if the wheel has automatic traction control enabled.
        /// A value between 0 and 5 inclusive.
        /// </summary>
        [KRPCProperty]
        public float TractionControl {
            get {
                CheckMotor();
                return motor.tractionControlScale;
            }
            set {
                CheckMotor();
                motor.tractionControlScale = value.Clamp(0, 5);
            }
        }

        /// <summary>
        /// Manual setting for the motor limiter.
        /// Only takes effect if the wheel has automatic traction control disabled.
        /// A value between 0 and 100 inclusive.
        /// </summary>
        [KRPCProperty]
        public float DriveLimiter {
            get {
                CheckMotor();
                return motor.driveLimiter;
            }
            set {
                CheckMotor();
                motor.driveLimiter = value.Clamp(0, 100);
            }
        }

        /// <summary>
        /// Whether the wheel has steering.
        /// </summary>
        [KRPCProperty]
        public bool Steerable {
            get { return steering != null; }
        }

        /// <summary>
        /// Whether the wheel steering is enabled.
        /// </summary>
        [KRPCProperty]
        public bool SteeringEnabled {
            get {
                CheckSteering();
                return steering.steeringEnabled;
            }
            set {
                CheckSteering();
                steering.steeringEnabled = value;
            }
        }

        /// <summary>
        /// Whether the wheel steering is inverted.
        /// </summary>
        [KRPCProperty]
        public bool SteeringInverted {
            get {
                CheckSteering();
                return steering.steeringInvert;
            }
            set {
                CheckSteering();
                steering.steeringInvert = value;
            }
        }

        /// <summary>
        /// Whether the wheel has suspension.
        /// </summary>
        [KRPCProperty]
        public bool HasSuspension {
            get { return suspension != null; }
        }

        /// <summary>
        /// Suspension spring strength, as set in the editor.
        /// </summary>
        [KRPCProperty]
        public float SuspensionSpringStrength {
            get {
                CheckSuspension();
                return suspension.springTweakable;
            }
        }

        /// <summary>
        /// Suspension damper strength, as set in the editor.
        /// </summary>
        [KRPCProperty]
        public float SuspensionDamperStrength {
            get {
                CheckSuspension();
                return suspension.damperTweakable;
            }
        }

        /// <summary>
        /// Whether the wheel is broken.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public bool Broken {
            get { return damage != null && damage.isDamaged; }
        }

        /// <summary>
        /// Whether the wheel is repairable.
        /// </summary>
        [KRPCProperty]
        public bool Repairable {
            get { return damage != null && damage.isRepairable; }
        }

        /// <summary>
        /// Current stress on the wheel.
        /// </summary>
        [KRPCProperty]
        public float Stress {
            get {
                CheckDamage();
                return damage.totalStress;
            }
        }

        /// <summary>
        /// Stress tolerance of the wheel.
        /// </summary>
        [KRPCProperty]
        public float StressTolerance {
            get {
                CheckDamage();
                return damage.stressTolerance;
            }
        }

        /// <summary>
        /// Current stress on the wheel as a percentage of its stress tolerance.
        /// </summary>
        [KRPCProperty]
        public float StressPercentage {
            get {
                CheckDamage();
                return damage.stressPercent;
            }
        }

        /// <summary>
        /// Current deflection of the wheel.
        /// </summary>
        [KRPCProperty]
        public float Deflection {
            get {
                CheckDamage();
                return damage.currentDeflection;
            }
        }

        /// <summary>
        /// Current slip of the wheel.
        /// </summary>
        [KRPCProperty]
        public float Slip {
            get {
                CheckDamage();
                return damage.currentSlip;
            }
        }
    }
}
