using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// An engine propellant. See <see cref="Engine.Propellants"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Propellant : Equatable<Propellant>
    {
        readonly int resourceId;
        readonly uint partId;

        internal Propellant (global::Propellant propellantResource, global::Part underlyingPart)
        {
            resourceId = propellantResource.id;
            partId = underlyingPart.flightID;
        }

        /// <summary>
        /// Check if the propellants are equal.
        /// </summary>
        public override bool Equals (Propellant obj)
        {
            return obj.resourceId == resourceId;
        }

        /// <summary>
        /// Hash the propellant.
        /// </summary>
        public override int GetHashCode ()
        {
            return resourceId.GetHashCode ();
        }

        /// <summary>
        /// The KSP part.
        /// </summary>
        public global::Part InternalPart {
            get { return FlightGlobals.FindPartByID (partId); }
        }

        /// <summary>
        /// The KSP propellant
        /// </summary>
        public global::Propellant InternalPropellant {
            get {
                var engineModule = InternalPart.GetComponent<ModuleEngines> ();
                if (engineModule != null)
                    return engineModule.propellants.Find (p => p.id == resourceId);
                else
                    return null;
            }
        }

        /// <summary>
        /// The name of the propellant.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return InternalPropellant.name; }
        }

        /// <summary>
        /// The current amount of propellant
        /// </summary>
        [KRPCProperty]
        public double CurrentAmount {
            get
            { return InternalPropellant.currentAmount; }
        }

        /// <summary>
        /// The required amount of propellant
        /// </summary>
        [KRPCProperty]
        public double CurrentRequirement {
            get
            { return InternalPropellant.currentRequirement; }
        }

        /// <summary>
        /// The total amount of the underlying resource currently reachable given resource flow rules
        /// </summary>
        [KRPCProperty]
        public double TotalResourceAvailable {
            get
            { return InternalPropellant.totalResourceAvailable; }
        }

        /// <summary>
        /// The total vehicle capacity for the underlying propellant resource, restricted by resource flow rules
        /// </summary>
        [KRPCProperty]
        public double TotalResourceCapacity {
            get
            { return InternalPropellant.totalResourceCapacity; }
        }

        /// <summary>
        /// If this propellant should be ignored when calculating required mass flow given specific impulse
        /// </summary>
        [KRPCProperty]
        public bool IgnoreForIsp {
            get
            { return InternalPropellant.ignoreForIsp; }
        }

        /// <summary>
        /// If this propellant should be ignored for thrust curve calculations
        /// </summary>
        [KRPCProperty]
        public bool IgnoreForThrustCurve {
            get
            { return InternalPropellant.ignoreForThrustCurve; }
        }

        /// <summary>
        /// If this propellant has a stack gauge or not
        /// </summary>
        [KRPCProperty]
        public bool DrawStackGauge {
            get
            { return InternalPropellant.drawStackGauge; }
        }

        /// <summary>
        /// If this propellant is deprived
        /// </summary>
        [KRPCProperty]
        public bool IsDeprived {
            get
            { return InternalPropellant.isDeprived; }
        }

        /// <summary>
        /// The propellant ratio
        /// </summary>
        [KRPCProperty]
        public float Ratio {
            get
            { return InternalPropellant.ratio; }
        }

        /// <summary>
        /// The reachable resources connected to this propellant
        /// </summary>
        [KRPCProperty]
        public IList<Resource> ConnectedResources {
            get {
                return InternalPropellant.connectedResources.Select (resource => new Resource (resource)).ToList ();
            }
        }
    }
}
