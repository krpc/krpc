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
