using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A solar panel, which turns sunlight into electrical power. Obtained from <see cref="Part.SolarPanels"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class SolarPanel : Equatable<SolarPanel>, IGameObjectState
    {
        readonly global::KSA.SolarPanel module;

        internal SolarPanel (Part part, global::KSA.SolarPanel module)
        {
            Part = part;
            this.module = module;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (SolarPanel other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (module, other.module);
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
            return module.GetHashCode ();
        }

        /// <summary>
        /// The part the solar panel is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        /// <summary>
        /// The deployable section that opens the solar panel, or <c>null</c> for a panel that
        /// does not deploy.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Deployable Deployable {
            get {
                _ = Part.InternalPart;
                var deployable = module.KeyframeAnimationModule;
                return deployable == null ? null : new Deployable (Part, deployable);
            }
        }

        ref readonly SolarPanelState State {
            get {
                if (!ModuleStateful<global::KSA.SolarPanel, SolarPanelState, EmptyStruct, EmptyStruct>.TryGetFrom (
                    Part.InternalVehicle.Parts.States, out var states))
                    throw new InvalidOperationException ("The vehicle has no solar panels");
                return ref states.GetState (module);
            }
        }

        /// <summary>
        /// Whether the solar panel can produce power, because it is deployed or does not deploy.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return State.Active; }
        }

        /// <summary>
        /// The power the solar panel produces, in watts.
        /// </summary>
        [KRPCProperty]
        public float Power {
            get { return State.Produced.Value (); }
        }

        /// <summary>
        /// The angle between the solar panel's face and the direction to the sun, in degrees.
        /// Zero when the panel faces the sun.
        /// </summary>
        [KRPCProperty]
        public float SunAngle {
            get { return (float)(State.SunAoA * (180d / Math.PI)); }
        }

        /// <summary>
        /// Whether a body blocks the sunlight to the solar panel.
        /// </summary>
        [KRPCProperty]
        public bool Occluded {
            get { return State.IsOccluded; }
        }
    }
}
