using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The RCS thrusters of a part. Obtained from <see cref="Part.RCS"/>.
    /// </summary>
    /// <remarks>
    /// Active thrusters fire when the vehicle's RCS is on. See <see cref="Control.RCS"/>.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class RCS : Equatable<RCS>
    {
        readonly ThrusterController thruster;

        internal RCS (Part part, ThrusterController thruster)
        {
            Part = part;
            this.thruster = thruster;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (RCS other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (thruster, other.thruster);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return thruster.GetHashCode ();
        }

        /// <summary>
        /// The part the thrusters are in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        /// <summary>
        /// Whether the thrusters are active. Setting it returns once the change has applied.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get {
                _ = Part.InternalPart;
                return thruster.IsActive;
            }
            set {
                thruster.SetIsActive (Part.InternalVehicle, value);
                Waits.Until (() => thruster.IsActive == value);
            }
        }
    }
}
