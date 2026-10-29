using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A light. Obtained from <see cref="Part.Lights"/>.
    /// </summary>
    /// <remarks>
    /// The vehicle's lights switch on and off together, see <see cref="Control.Lights"/>.
    /// Cabin lights are always on.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class Light : Equatable<Light>, IGameObjectState
    {
        readonly LightModule module;

        internal Light (Part part, LightModule module)
        {
            Part = part;
            this.module = module;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Light other)
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
        /// The part the light is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        /// <summary>
        /// Whether the light is on.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get {
                _ = Part.InternalPart;
                return module.IsActive;
            }
        }
    }
}
