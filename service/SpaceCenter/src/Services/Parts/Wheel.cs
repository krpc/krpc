using System;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A wheel. Includes landing gear and rover wheels.
    /// Obtained by calling <see cref="Part.Wheel"/>.
    /// Can be used to control the motors, steering and deployment of wheels, among other things.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class Wheel : Equatable<Wheel>, IGameObjectState
    {
        ModuleRef wheelRef;
        ModuleRef brakesRef;
        ModuleRef damageRef;
        ModuleRef deploymentRef;
        ModuleRef motorRef;
        ModuleRef steeringRef;
        ModuleRef suspensionRef;

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
            wheelRef = ModuleRef.ForType<ModuleWheelBase> (internalPart);
            brakesRef = ModuleRef.ForType<ModuleWheels.ModuleWheelBrakes> (internalPart);
            damageRef = ModuleRef.ForType<ModuleWheels.ModuleWheelDamage> (internalPart);
            deploymentRef = ModuleRef.ForType<ModuleWheels.ModuleWheelDeployment> (internalPart);
            motorRef = ModuleRef.ForType<ModuleWheels.ModuleWheelMotor> (internalPart);
            steeringRef = ModuleRef.ForType<ModuleWheels.ModuleWheelSteering> (internalPart);
            suspensionRef = ModuleRef.ForType<ModuleWheels.ModuleWheelSuspension> (internalPart);

        }

        ModuleWheelBase InternalWheel {
            get { return (ModuleWheelBase)wheelRef.Get (Part.InternalPart); }
        }

        ModuleWheels.ModuleWheelBrakes InternalBrakes {
            get { return (ModuleWheels.ModuleWheelBrakes)brakesRef.Find (Part.InternalPart); }
        }

        ModuleWheels.ModuleWheelDamage InternalDamage {
            get { return (ModuleWheels.ModuleWheelDamage)damageRef.Find (Part.InternalPart); }
        }

        ModuleWheels.ModuleWheelDeployment InternalDeployment {
            get { return (ModuleWheels.ModuleWheelDeployment)deploymentRef.Find (Part.InternalPart); }
        }

        ModuleWheels.ModuleWheelMotor InternalMotor {
            get { return (ModuleWheels.ModuleWheelMotor)motorRef.Find (Part.InternalPart); }
        }

        ModuleWheels.ModuleWheelSteering InternalSteering {
            get { return (ModuleWheels.ModuleWheelSteering)steeringRef.Find (Part.InternalPart); }
        }

        ModuleWheels.ModuleWheelSuspension InternalSuspension {
            get { return (ModuleWheels.ModuleWheelSuspension)suspensionRef.Find (Part.InternalPart); }
        }

        /// <summary>
        /// The state of the part carrying the wheel, or destroyed once that part loses the
        /// module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return wheelRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(Wheel other)
        {
            return !ReferenceEquals(other, null) && Part == other.Part && wheelRef == other.wheelRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return Hash.Of (Part).And (wheelRef);
        }

        void CheckBrakes()
        {
            if (InternalBrakes == null)
                throw new InvalidOperationException("Wheel does not have brakes");
        }

        void CheckDeployment()
        {
            if (InternalDeployment == null)
                throw new InvalidOperationException("Wheel is not deployable");
        }

        void CheckMotor()
        {
            if (InternalMotor == null)
                throw new InvalidOperationException("Wheel is not powered");
        }

        void CheckSteering()
        {
            if (InternalSteering == null)
                throw new InvalidOperationException("Wheel is not steerable");
        }

        void CheckSuspension()
        {
            if (InternalSuspension == null)
                throw new InvalidOperationException("Wheel does not have suspension");
        }

        void CheckDamage()
        {
            if (InternalDamage == null)
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
        public DeployableState State {
            get { return InternalDeployment.ToDeployableState (InternalDamage); }
        }

        /// <summary>
        /// Radius of the wheel, in meters.
        /// </summary>
        [KRPCProperty]
        public float Radius {
            get { return InternalWheel.radius; }
        }

        /// <summary>
        /// Whether the wheel is touching the ground.
        /// </summary>
        [KRPCProperty]
        public bool Grounded {
            get { return InternalWheel.isGrounded; }
        }

        /// <summary>
        /// Whether the wheel has brakes.
        /// </summary>
        [KRPCProperty]
        public bool HasBrakes {
            get { return InternalBrakes != null; }
        }

        /// <summary>
        /// The braking force, as a percentage of maximum, when the brakes are applied.
        /// </summary>
        [KRPCProperty]
        public float Brakes {
            get {
                CheckBrakes();
                return InternalBrakes.brakeTweakable;
            }
            set {
                CheckBrakes();
                InternalBrakes.brakeTweakable = value;
            }
        }

        /// <summary>
        /// Whether automatic friction control is enabled.
        /// </summary>
        [KRPCProperty]
        public bool AutoFrictionControl {
            get { return InternalWheel.autoFriction; }
            set { InternalWheel.autoFriction = value; }
        }

        /// <summary>
        /// Manual friction control value. Only has an effect if automatic friction control is disabled.
        /// A value between 0 and 5 inclusive.
        /// </summary>
        [KRPCProperty]
        public float ManualFrictionControl {
            get { return InternalWheel.frictionMultiplier; }
            set { InternalWheel.frictionMultiplier = value.Clamp(0, 5); }
        }

        /// <summary>
        /// Whether the wheel is deployable.
        /// </summary>
        [KRPCProperty]
        public bool Deployable {
            get { return InternalDeployment != null; }
        }

        /// <summary>
        /// Whether the wheel is deployed.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get { return State == DeployableState.Deployed; }
            set {
                CheckDeployment();
                InternalDeployment.ActionToggle(new KSPActionParam(0, value ? KSPActionType.Activate : KSPActionType.Deactivate));
            }
        }

        /// <summary>
        /// Whether the wheel is powered by a motor.
        /// </summary>
        [KRPCProperty]
        public bool Powered {
            get { return InternalMotor != null; }
        }

        /// <summary>
        /// Whether the motor is enabled.
        /// </summary>
        [KRPCProperty]
        public bool MotorEnabled {
            get {
                CheckMotor();
                return InternalMotor.motorEnabled;
            }
            set {
                CheckMotor();
                InternalMotor.motorEnabled = value;
            }
        }

        /// <summary>
        /// Whether the direction of the motor is inverted.
        /// </summary>
        [KRPCProperty]
        public bool MotorInverted {
            get {
                CheckMotor();
                return InternalMotor.motorInverted;
            }
            set {
                CheckMotor();
                InternalMotor.motorInverted = value;
            }
        }

        /// <summary>
        /// Whether the direction of the motor is inverted.
        /// </summary>
        [KRPCProperty]
        public MotorState MotorState {
            get {
                CheckMotor();
                return InternalMotor.state.ToMotorState();
            }
        }

        /// <summary>
        /// The output of the motor. This is the torque currently being generated, in Newton meters.
        /// </summary>
        [KRPCProperty]
        public float MotorOutput {
            get {
                CheckMotor();
                return InternalMotor.driveOutput;
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
                return InternalMotor.autoTorque;
            }
            set {
                CheckMotor();
                InternalMotor.autoTorque = value;
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
                return InternalMotor.tractionControlScale;
            }
            set {
                CheckMotor();
                InternalMotor.tractionControlScale = value.Clamp(0, 5);
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
                return InternalMotor.driveLimiter;
            }
            set {
                CheckMotor();
                InternalMotor.driveLimiter = value.Clamp(0, 100);
            }
        }

        /// <summary>
        /// Whether the wheel has steering.
        /// </summary>
        [KRPCProperty]
        public bool Steerable {
            get { return InternalSteering != null; }
        }

        /// <summary>
        /// Whether the wheel steering is enabled.
        /// </summary>
        [KRPCProperty]
        public bool SteeringEnabled {
            get {
                CheckSteering();
                return InternalSteering.steeringEnabled;
            }
            set {
                CheckSteering();
                InternalSteering.steeringEnabled = value;
            }
        }

        /// <summary>
        /// Whether the wheel steering is inverted.
        /// </summary>
        [KRPCProperty]
        public bool SteeringInverted {
            get {
                CheckSteering();
                return InternalSteering.steeringInvert;
            }
            set {
                CheckSteering();
                InternalSteering.steeringInvert = value;
            }
        }

        /// <summary>
        /// Whether the steering angle is automatically limited based on the vessel's speed,
        /// reducing the maximum angle as the vessel moves faster. See also
        /// <see cref="SteeringAngleLimit" />.
        /// </summary>
        [KRPCProperty]
        public bool SteeringAngleAuto {
            get {
                CheckSteering();
                return InternalSteering.autoSteeringAdjust;
            }
            set {
                CheckSteering();
                InternalSteering.autoSteeringAdjust = value;
            }
        }

        /// <summary>
        /// The steering angle limit.
        /// </summary>
        [KRPCProperty]
        public float SteeringAngleLimit
        {
            get
            {
                CheckSteering();
                return InternalSteering.angleTweakable;
            }
            set
            {
                CheckSteering();
                InternalSteering.angleTweakable = value;
            }
        }

        /// <summary>
        /// Steering response time.
        /// </summary>
        [KRPCProperty]
        public float SteeringResponseTime
        {
            get
            {
                CheckSteering();
                return InternalSteering.responseTweakable;
            }
            set
            {
                CheckSteering();
                InternalSteering.responseTweakable = value;
            }
        }

        /// <summary>
        /// Whether the wheel has suspension.
        /// </summary>
        [KRPCProperty]
        public bool HasSuspension {
            get { return InternalSuspension != null; }
        }

        /// <summary>
        /// Suspension spring strength, as set in the editor.
        /// </summary>
        [KRPCProperty]
        public float SuspensionSpringStrength {
            get {
                CheckSuspension();
                return InternalSuspension.springTweakable;
            }
        }

        /// <summary>
        /// Suspension damper strength, as set in the editor.
        /// </summary>
        [KRPCProperty]
        public float SuspensionDamperStrength {
            get {
                CheckSuspension();
                return InternalSuspension.damperTweakable;
            }
        }

        /// <summary>
        /// Whether the wheel is broken.
        /// </summary>
        [KRPCProperty]
        public bool Broken {
            get { return InternalDamage != null && InternalDamage.isDamaged; }
        }

        /// <summary>
        /// Whether the wheel is repairable.
        /// </summary>
        [KRPCProperty]
        public bool Repairable {
            get { return InternalDamage != null && InternalDamage.isRepairable; }
        }

        /// <summary>
        /// Current stress on the wheel.
        /// </summary>
        [KRPCProperty]
        public float Stress {
            get {
                CheckDamage();
                return InternalDamage.totalStress;
            }
        }

        /// <summary>
        /// Stress tolerance of the wheel.
        /// </summary>
        [KRPCProperty]
        public float StressTolerance {
            get {
                CheckDamage();
                return InternalDamage.stressTolerance;
            }
        }

        /// <summary>
        /// Current stress on the wheel as a percentage of its stress tolerance.
        /// </summary>
        [KRPCProperty]
        public float StressPercentage {
            get {
                CheckDamage();
                return InternalDamage.stressPercent;
            }
        }

        /// <summary>
        /// Current deflection of the wheel.
        /// </summary>
        [KRPCProperty]
        public float Deflection {
            get {
                CheckDamage();
                return InternalDamage.currentDeflection;
            }
        }

        /// <summary>
        /// Current slip of the wheel.
        /// </summary>
        [KRPCProperty]
        public float Slip {
            get {
                CheckDamage();
                return InternalDamage.currentSlip;
            }
        }
    }
}
