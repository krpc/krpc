using System;
using System.Collections.Generic;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A docking port. Obtained from <see cref="Part.DockingPort"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class DockingPort : Equatable<DockingPort>, IGameObjectState
    {
        static readonly Action<global::KSA.DockingPort, bool> setHatchOpen =
            Accessor.Method<Action<global::KSA.DockingPort, bool>> (typeof (global::KSA.DockingPort), "SetHatchOpen");

        readonly global::KSA.DockingPort port;

        internal DockingPort (Part part, global::KSA.DockingPort port)
        {
            Part = part;
            this.port = port;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (DockingPort other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (port, other.port);
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
            return port.GetHashCode ();
        }

        /// <summary>
        /// The part the docking port is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        /// <summary>
        /// Whether the port is docked.
        /// </summary>
        [KRPCProperty]
        public bool Docked {
            get {
                _ = Part.InternalPart;
                return port.Docked;
            }
        }

        /// <summary>
        /// The part the port is docked to, or <c>null</c> if it is not docked.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Part DockedPart {
            get {
                var other = port.Connector.Connection?.OtherPart (Part.InternalPart.FullPart);
                return other == null ? null : new Part (other);
            }
        }

        /// <summary>
        /// Returns whether the hatch through the docked ports is open, and opens or closes it.
        /// An open hatch joins the habitat zones on either side.
        /// </summary>
        /// <remarks>
        /// The hatch is closed while the port is not docked. Throws an exception when set on a
        /// port that is not docked.
        /// </remarks>
        [KRPCProperty]
        public bool HatchOpen {
            get {
                _ = Part.InternalPart;
                return port.IsHatchOpen;
            }
            set {
                _ = Part.InternalPart;
                if (!port.Docked)
                    throw new InvalidOperationException ("The docking port is not docked");
                // As the port's part window does, which also sets the port it is docked to
                if (setHatchOpen == null)
                    throw new InvalidOperationException ("The game has no SetHatchOpen method");
                setHatchOpen (port, value);
            }
        }

        /// <summary>
        /// Undock the port, separating the vehicle into two. Returns once it has undocked.
        /// Throws an exception if the port is not docked.
        /// </summary>
        /// <returns>The new vehicles.</returns>
        /// <remarks>
        /// Docking and undocking replace the vehicles involved. See <see cref="Vehicle"/>.
        /// </remarks>
        [KRPCMethod]
        public IList<Vehicle> Undock ()
        {
            var p = Part.InternalPart;
            if (!port.Docked)
                throw new InvalidOperationException ("The docking port is not docked");
            var before = new HashSet<global::KSA.Vehicle> (Vehicles.All);
            InputEvents.VehicleDockingInputBuffer.Add (new InputEvents.VehicleDockingInputData {
                Vehicle = p.Tree.OwningVehicle,
                DockingPort = port,
                Undock = true
            });
            return Vehicles.SeparatedWhen (() => !port.Docked, before);
        }
    }
}
