using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A fuel line between two parts of a vehicle, which lets propellant flow between them.
    /// Obtained from <see cref="Vehicle.FuelLinks"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class FuelLink : Equatable<FuelLink>, IGameObjectState
    {
        readonly global::KSA.FuelLink link;

        internal FuelLink (global::KSA.FuelLink link)
        {
            this.link = link;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (FuelLink other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (link, other.link);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return InList ? GameObjectState.Live : GameObjectState.Destroyed; }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return link.GetHashCode ();
        }

        // The links of the vehicle that holds the link, or null if the link is gone. A removed
        // link never returns to the list
        FuelLinkList List {
            get {
                var vehicle = link.PartA.Tree?.OwningVehicle;
                if (vehicle == null || !Utils.Vehicles.Exists (vehicle))
                    return null;
                var links = link.PartA.Tree.FuelLinks;
                foreach (var other in links.Links) {
                    if (ReferenceEquals (other, link))
                        return links;
                }
                return null;
            }
        }

        bool InList {
            get { return List != null; }
        }

        FuelLinkList InternalList {
            get { return List ?? throw new Service.KRPC.ObjectDestroyedException ("The fuel link no longer exists"); }
        }

        /// <summary>
        /// One of the parts the link joins.
        /// </summary>
        [KRPCProperty]
        public Part PartA {
            get {
                _ = InternalList;
                return new Part (link.PartA);
            }
        }

        /// <summary>
        /// The other part the link joins.
        /// </summary>
        [KRPCProperty]
        public Part PartB {
            get {
                _ = InternalList;
                return new Part (link.PartB);
            }
        }

        /// <summary>
        /// The direction propellant flows through the link.
        /// </summary>
        [KRPCProperty]
        public FuelLinkFlow Flow {
            get {
                _ = InternalList;
                return (FuelLinkFlow)link.Flow;
            }
            set {
                if (!Enum.IsDefined (value))
                    throw new ArgumentOutOfRangeException (nameof (value));
                InternalList.SetFlow (link, (global::KSA.FuelLinkFlow)value);
            }
        }

        /// <summary>
        /// Whether propellant can flow through the link.
        /// </summary>
        [KRPCProperty]
        public bool Enabled {
            get {
                _ = InternalList;
                return link.Enabled;
            }
            set { InternalList.SetEnabled (link, value); }
        }
    }
}
