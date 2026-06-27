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
                // Note: GetPotentialTorque does not apply the authority limiter (it is
                // only applied when the surface actually deflects). Scale by it here so
                // the available torque matches what the surface will deliver.
                var torque = controlSurface.GetPotentialTorque ();
                var scale = controlSurface.authorityLimiter * 0.01d;
                return new TupleV3 (torque.Item1 * scale, torque.Item2 * scale);
            }
        }
    }
}
