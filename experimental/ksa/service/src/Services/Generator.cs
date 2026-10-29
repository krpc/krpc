using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A generator, which produces electrical power. Obtained from <see cref="Part.Generators"/>.
    /// </summary>
    /// <remarks>
    /// A generator runs while its <see cref="Engine"/> is active, and produces power in
    /// proportion to the engine's throttle.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class Generator : Equatable<Generator>, IGameObjectState
    {
        readonly global::KSA.Generator module;

        internal Generator (Part part, global::KSA.Generator module)
        {
            Part = part;
            this.module = module;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Generator other)
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
        /// The part the generator is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        ref readonly GeneratorState State {
            get {
                if (!ModuleStateful<global::KSA.Generator, GeneratorState, EmptyStruct, EmptyStruct>.TryGetFrom (
                    Part.InternalVehicle.Parts.States, out var states))
                    throw new InvalidOperationException ("The vehicle has no generators");
                return ref states.GetState (module);
            }
        }

        /// <summary>
        /// The engine that drives the generator, or <c>null</c> if it has none.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Engine Engine {
            get {
                foreach (var engine in Part.InternalPart.SubtreeModules.Get<EngineController> ()) {
                    if (engine.InstanceId == module.EngineInstanceId)
                        return new Engine (Part, engine);
                }
                return null;
            }
        }

        /// <summary>
        /// Whether the generator is producing power.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return State.Active; }
        }

        /// <summary>
        /// The power the generator produces, in watts.
        /// </summary>
        [KRPCProperty]
        public double Power {
            get { return State.Produced.Value (); }
        }
    }
}
