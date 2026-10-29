using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A power consumer, which uses electrical power, such as a light. Obtained from
    /// <see cref="Part.PowerConsumers"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class PowerConsumer : Equatable<PowerConsumer>, IGameObjectState
    {
        readonly global::KSA.PowerConsumer module;

        internal PowerConsumer (Part part, global::KSA.PowerConsumer module)
        {
            Part = part;
            this.module = module;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (PowerConsumer other)
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
        /// The part the power consumer is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        ref readonly PowerConsumerState State {
            get {
                if (!ModuleStateful<global::KSA.PowerConsumer, PowerConsumerState, EmptyStruct, EmptyStruct>.TryGetFrom (
                    Part.InternalVehicle.Parts.States, out var states))
                    throw new InvalidOperationException ("The vehicle has no power consumers");
                return ref states.GetState (module);
            }
        }

        /// <summary>
        /// Whether the part is using power.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return State.Active; }
        }

        /// <summary>
        /// The power the part uses, in watts.
        /// </summary>
        [KRPCProperty]
        public double Power {
            get { return State.Consumed.Value (); }
        }
    }
}
