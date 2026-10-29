using System;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A propellant tank. Obtained from <see cref="Part.Tanks"/> or <see cref="Vehicle.Tanks"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Part.Resources"/> gives the substances held by the tanks of a part.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class Tank : Equatable<Tank>, IGameObjectState
    {
        readonly global::KSA.Tank tank;

        internal Tank (Part part, global::KSA.Tank tank)
        {
            Part = part;
            this.tank = tank;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Tank other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (tank, other.tank);
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
            return tank.GetHashCode ();
        }

        /// <summary>
        /// The part the tank is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        global::KSA.Tank InternalTank {
            get {
                _ = Part.InternalPart;
                return tank;
            }
        }

        /// <summary>
        /// The volume of the tank, in cubic meters.
        /// </summary>
        [KRPCProperty]
        public double Volume {
            get { return InternalTank.StorageVolume; }
        }

        /// <summary>
        /// The fraction of the tank's volume that is filled, between 0 and 1.
        /// </summary>
        [KRPCProperty]
        public double FilledFraction {
            get { return InternalTank.FilledFraction (Part.InternalPart.Tree.Moles.States); }
        }

        /// <summary>
        /// Whether engines can draw propellant from the tank.
        /// </summary>
        [KRPCProperty]
        public bool PropellantUseEnabled {
            get { return InternalTank.PropellantUseEnabled; }
            set { InternalTank.PropellantUseEnabled = value; }
        }

        /// <summary>
        /// The direction the tank transfers propellant in.
        /// </summary>
        /// <remarks>
        /// Propellant flows from tanks transferring out to tanks of the same vehicle transferring
        /// in, and the transfer draws battery power. Setting a transfer throws an exception if
        /// the running version of KSA does not support it.
        /// </remarks>
        [KRPCProperty]
        public TankTransferMode TransferMode {
            get { return (TankTransferMode)InternalTank.TransferMode; }
            set {
                if (!Enum.IsDefined (value))
                    throw new ArgumentOutOfRangeException (nameof (value));
                if (value != TankTransferMode.None && !TankTransfers.Patched)
                    throw new InvalidOperationException ("Tank transfers are not supported by this version of KSA");
                var mode = (global::KSA.TankTransferMode)value;
                InternalTank.TransferMode = mode;
                TankTransfers.Hold (tank, mode);
            }
        }
    }
}
