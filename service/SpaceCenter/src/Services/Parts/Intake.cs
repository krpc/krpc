using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.Intake"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Intake : Equatable<Intake>
    {
        readonly Part part;
        readonly ModuleResourceIntake intake;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<ModuleResourceIntake> ();
        }

        internal Intake (Part part)
        {
            this.part = part;
            intake = part.InternalPart.Module<ModuleResourceIntake> ();
            if (intake == null)
                throw new ArgumentException ("Part is not an intake");
        }

        /// <summary>
        /// Check if intakes are equal.
        /// </summary>
        public override bool Equals (Intake obj)
        {
            return part == obj.part && intake == obj.intake;
        }

        /// <summary>
        /// Hash the intake.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode () ^ intake.GetHashCode ();
        }

        /// <summary>
        /// The part object for this intake.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// Whether the intake is open.
        /// </summary>
        [KRPCProperty]
        public bool Open {
            get { return intake.intakeEnabled; }
            set {
                if (value)
                    intake.Activate ();
                else
                    intake.Deactivate ();
            }
        }

        /// <summary>
        /// Speed of the flow into the intake, in <math>m/s</math>.
        /// </summary>
        [KRPCProperty]
        public float Speed {
            get { return Open ? (float)intake.intakeSpeed : 0f; }
        }

        /// <summary>
        /// The rate of flow into the intake, in units of resource per second.
        /// </summary>
        [KRPCProperty]
        public float Flow {
            get { return Open ? intake.airFlow : 0f; }
        }

        /// <summary>
        /// The area of the intake's opening, in square meters.
        /// </summary>
        [KRPCProperty]
        public float Area {
            get { return (float)intake.area; }
        }
    }
}
