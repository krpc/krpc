using System;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The vessel that is being constructed in the editor.
    /// Obtained by calling <see cref="Editor.Vessel"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Editor)]
    public class EditorVessel : Equatable<EditorVessel>
    {
        internal EditorVessel ()
        {
        }

        /// <summary>
        /// Returns true if the objects are equal. The editor only ever holds one vessel,
        /// so every editor vessel object refers to it.
        /// </summary>
        public override bool Equals (EditorVessel other)
        {
            return !ReferenceEquals (other, null);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return 0;
        }

        /// <summary>
        /// The KSP ship construct. Looked up on each use, so the object keeps working
        /// after a different vessel is loaded into the editor.
        /// </summary>
        public ShipConstruct InternalShipConstruct {
            get {
                var logic = EditorLogic.fetch;
                var construct = ReferenceEquals (logic, null) ? null : logic.ship;
                if (ReferenceEquals (construct, null))
                    throw new InvalidOperationException ("The editor does not contain a vessel.");
                return construct;
            }
        }

        /// <summary>
        /// The name of the vessel.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return InternalShipConstruct.shipName; }
            set { InternalShipConstruct.shipName = value; }
        }

        /// <summary>
        /// The description of the vessel.
        /// </summary>
        [KRPCProperty]
        public string Description {
            get { return InternalShipConstruct.shipDescription; }
            set { InternalShipConstruct.shipDescription = value; }
        }

        /// <summary>
        /// The editor the vessel is in.
        /// </summary>
        /// <remarks>
        /// This is the editor the vessel is currently loaded into, not the one it was
        /// designed in. A vessel moved from the vehicle assembly building to the space
        /// plane hangar reports the space plane hangar.
        /// </remarks>
        [KRPCProperty]
        public EditorFacility Facility {
            get { return (EditorFacility)InternalShipConstruct.shipFacility; }
        }

        /// <summary>
        /// The dimensions of the vessel's bounding box, in meters.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Size {
            get { return InternalShipConstruct.shipSize.ToTuple (); }
        }

        /// <summary>
        /// The total mass of the vessel, including resources and crew, in kg.
        /// </summary>
        [KRPCProperty]
        public float Mass {
            get {
                float dryMass;
                float fuelMass;
                return InternalShipConstruct.GetShipMass (out dryMass, out fuelMass, ShipConstruction.ShipManifest) * 1000f;
            }
        }

        /// <summary>
        /// The total mass of the vessel, excluding resources, in kg.
        /// </summary>
        [KRPCProperty]
        public float DryMass {
            get {
                float dryMass;
                float fuelMass;
                InternalShipConstruct.GetShipMass (out dryMass, out fuelMass, ShipConstruction.ShipManifest);
                return dryMass * 1000f;
            }
        }

        /// <summary>
        /// The total cost of the vessel, including resources, in funds.
        /// </summary>
        [KRPCProperty]
        public float Cost {
            get {
                float dryCost;
                float fuelCost;
                return InternalShipConstruct.GetShipCosts (out dryCost, out fuelCost, ShipConstruction.ShipManifest);
            }
        }

        /// <summary>
        /// The total number of crew that the vessel can hold.
        /// </summary>
        [KRPCProperty]
        public int CrewCapacity {
            get { return InternalShipConstruct.Parts.Sum (part => part.CrewCapacity); }
        }
    }
}
