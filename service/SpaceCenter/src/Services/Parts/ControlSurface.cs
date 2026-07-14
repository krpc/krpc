using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Tuple3 = System.Tuple<double, double, double>;
using TupleV3 = System.Tuple<Vector3d, Vector3d>;
using TupleT3 = System.Tuple<System.Tuple<double, double, double>, System.Tuple<double, double, double>>;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// An aerodynamic control surface. Obtained by calling <see cref="Part.ControlSurface"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class ControlSurface : Equatable<ControlSurface>
    {
        readonly ModuleControlSurface controlSurface;

        internal static bool Is (Part part)
        {
            return Is (part.InternalPart);
        }

        internal static bool Is (global::Part part)
        {
            return part.HasModule<ModuleControlSurface> ();
        }

        internal ControlSurface (Part part)
        {
            Part = part;
            controlSurface = part.InternalPart.Module<ModuleControlSurface> ();
            if (controlSurface == null)
                throw new ArgumentException ("Part does not have a ModuleControlSurface PartModule");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ControlSurface other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && controlSurface.Equals (other.controlSurface);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode () ^ controlSurface.GetHashCode ();
        }

        /// <summary>
        /// The part object for this control surface.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the control surface has pitch control enabled.
        /// </summary>
        [KRPCProperty]
        public bool PitchEnabled {
            get { return !controlSurface.ignorePitch; }
            set { controlSurface.ignorePitch = !value; }
        }

        /// <summary>
        /// Whether the control surface has yaw control enabled.
        /// </summary>
        [KRPCProperty]
        public bool YawEnabled {
            get { return !controlSurface.ignoreYaw; }
            set { controlSurface.ignoreYaw = !value; }
        }

        /// <summary>
        /// Whether the control surface has roll control enabled.
        /// </summary>
        [KRPCProperty]
        public bool RollEnabled {
            get { return !controlSurface.ignoreRoll; }
            set { controlSurface.ignoreRoll = !value; }
        }

        /// <summary>
        /// The authority limiter for the control surface, which controls how far the
        /// control surface will move.
        /// </summary>
        [KRPCProperty]
        public float AuthorityLimiter {
            get { return controlSurface.authorityLimiter; }
            set { controlSurface.authorityLimiter = value; }
        }

        /// <summary>
        /// Whether the control surface movement is inverted.
        /// </summary>
        [KRPCProperty]
        public bool Inverted {
            get { return controlSurface.deployInvert; }
            set { controlSurface.deployInvert = value; }
        }

        /// <summary>
        /// Whether the control surface has been fully deployed.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get { return controlSurface.deploy; }
            set { controlSurface.deploy = value; }
        }

        /// <summary>
        /// Whether the control surface deflection is being set directly, bypassing the vessel's
        /// normal flight control. When enabled, the surface holds the deflection set by
        /// <see cref="Deflection"/> instead of responding to pitch, yaw and roll control inputs.
        /// The prior state is restored when the override is released, when the
        /// controlling client disconnects, or when the vessel changes.
        /// </summary>
        [KRPCProperty]
        public bool DeflectionOverride {
            get { return ActuatorControlAddon.GetControlSurfaceOverride (controlSurface); }
            set { ActuatorControlAddon.SetControlSurfaceOverride (controlSurface, value); }
        }

        /// <summary>
        /// The deflection command applied when <see cref="DeflectionOverride"/> is enabled, as a
        /// value between -1 and 1, mapped onto the surface's deploy angle range.
        /// </summary>
        [KRPCProperty]
        public float Deflection {
            get { return ActuatorControlAddon.GetControlSurfaceDeflection (controlSurface); }
            set { ActuatorControlAddon.SetControlSurfaceDeflection (controlSurface, value); }
        }

        /// <summary>
        /// Surface area of the control surface in <math>m^2</math>.
        /// </summary>
        [KRPCProperty]
        public float SurfaceArea {
            get { return controlSurface.ctrlSurfaceArea; }
        }

        /// <summary>
        /// The available torque, in Newton meters, that can be produced by this control surface,
        /// in the positive and negative pitch, roll and yaw axes of the vessel. These axes
        /// correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame"/>.
        /// </summary>
        [KRPCProperty]
        public TupleT3 AvailableTorque {
            get { return AvailableTorqueVectors.ToTuple (); }
        }

        internal TupleV3 AvailableTorqueVectors {
            get {
                // GetPotentialTorque already applies the authority limiter (via
                // ModuleControlSurface.GetPotentialLift, which scales the deflection by
                // authorityLimiter * 0.01), so no further scaling is needed here.
                var torque = controlSurface.GetPotentialTorque ();
                // ModuleControlSurface.GetPotentialTorque negates the roll (y) axis of both the
                // positive and negative torque vectors, unlike other ITorqueProvider
                // implementations. Normalise to the kRPC convention (positive torque >= 0,
                // negative torque <= 0) with Math.Abs, matching ITorqueProviderExtensions.Sum.
                return new TupleV3 (
                    new Vector3d (Math.Abs (torque.Item1.x), Math.Abs (torque.Item1.y), Math.Abs (torque.Item1.z)),
                    new Vector3d (-Math.Abs (torque.Item2.x), -Math.Abs (torque.Item2.y), -Math.Abs (torque.Item2.z)));
            }
        }
    }
}
