using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// An air intake. Obtained by calling <see cref="Part.Intake"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class Intake : Equatable<Intake>, IGameObjectState
    {
        ModuleRef intakeRef;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<ModuleResourceIntake> ();
        }

        internal Intake (Part part)
        {
            Part = part;
            intakeRef = ModuleRef.ForType<ModuleResourceIntake> (part.InternalPart);
            if (intakeRef.Find (part.InternalPart) == null)
                throw new ArgumentException ("Part is not an intake");
        }

        ModuleResourceIntake InternalIntake {
            get { return (ModuleResourceIntake)intakeRef.Get (Part.InternalPart); }
        }

        /// <summary>
        /// What the game holds for the intake: the state of the part
        /// carrying it, or destroyed once that part no longer has the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return intakeRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Intake other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && intakeRef == other.intakeRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode () ^ intakeRef.GetHashCode ();
        }

        /// <summary>
        /// The part object for this intake.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the intake is open.
        /// </summary>
        [KRPCProperty]
        public bool Open {
            get { return InternalIntake.intakeEnabled; }
            set {
                if (value)
                    InternalIntake.Activate ();
                else
                    InternalIntake.Deactivate ();
            }
        }

        /// <summary>
        /// Speed of the flow into the intake, in <math>m/s</math>.
        /// </summary>
        [KRPCProperty]
        public float Speed {
            get { return Open ? (float)InternalIntake.intakeSpeed : 0f; }
        }

        /// <summary>
        /// The rate of flow into the intake, in units of resource per second.
        /// </summary>
        [KRPCProperty]
        public float Flow {
            get { return Open ? InternalIntake.airFlow : 0f; }
        }

        /// <summary>
        /// The area of the intake's opening, in square meters.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float Area {
            get { return (float)InternalIntake.area; }
        }
    }
}
