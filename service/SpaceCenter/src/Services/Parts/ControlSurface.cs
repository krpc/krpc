using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

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
        /// The authority limiter for the control surface, which controls how far the control surface will move.
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
        /// The available torque in the positive pitch, roll and yaw axes and
        /// negative pitch, roll and yaw axes of the vessel, in Newton meters.
        /// These axes correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame" />.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        public Tuple<Tuple3, Tuple3> AvailableTorque {
            get { return AvailableTorqueVectors.ToTuple (); }
        }

        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        internal Tuple<Vector3d, Vector3d> AvailableTorqueVectors {
            get { return controlSurface.GetPotentialTorque (); }
        }
    }
}
