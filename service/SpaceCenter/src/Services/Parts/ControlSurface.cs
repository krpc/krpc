using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.ControlSurface"/>.
    /// Provides functionality to interact with aerodynamic control surfaces.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class ControlSurface : Equatable<ControlSurface>
    {
        readonly Part part;
        readonly ModuleControlSurface controlSurface;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<ModuleControlSurface> ();
        }

        internal ControlSurface (Part part)
        {
            this.part = part;
            controlSurface = part.InternalPart.Module<ModuleControlSurface> ();
            if (controlSurface == null)
                throw new ArgumentException ("Part does not have a ModuleControlSurface PartModule");
        }

        /// <summary>
        /// Check the control surfaces are equal.
        /// </summary>
        public override bool Equals (ControlSurface obj)
        {
            return part == obj.part && controlSurface == obj.controlSurface;
        }

        /// <summary>
        /// Hash the control surface.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode () ^ controlSurface.GetHashCode ();
        }

        /// <summary>
        /// The part object for this control surface.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

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
        /// The available torque in the pitch, roll and yaw axes of the vessel, in Newton meters.
        /// These axes correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame" />.
        /// </summary>
        [KRPCProperty]
        public Tuple3 AvailableTorque {
            get { return AvailableTorqueVector.ToTuple (); }
        }

        internal Vector3d AvailableTorqueVector {
            get { return controlSurface.GetPotentialTorque () * 1000f; }
        }
    }
}
