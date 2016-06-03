using System;
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

        internal global::Part InternalPart {
            get { return FlightGlobals.FindPartByID (partId); }
        }

        internal global::Propellant InternalPropellant {
            get { 
                var engineModule = InternalPart.GetComponent<ModuleEngines> ();
                if (engineModule == null)
                    throw new InvalidOperationException ("Part is not an engine");
                return engineModule.propellants.Find (propellant => propellant.id == resourceId);
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
        /// The current amount of the propellant.
        /// </summary>
        [KRPCProperty]
        public double CurrentAmount {
            get { return InternalPropellant.currentAmount; }
        }

        /// <summary>
        /// The current required amount of propellant.
        /// </summary>
        [KRPCProperty]
        public double CurrentRequirement {
            get { return InternalPropellant.currentRequirement; }
        }

        /// <summary>
        /// The total propellant resource available.
        /// </summary>
        [KRPCProperty]
        public double TotalResourceAvailable {
            get { return InternalPropellant.totalResourceAvailable; }
        }

        /// <summary>
        /// The total propellant resource capacity.
        /// </summary>
        [KRPCProperty]
        public double TotalResourceCapacity {
            get { return InternalPropellant.totalResourceCapacity; }
        }

        /// <summary>
        /// Whether the propellant should be ignored for specific impulse calculations.
        /// </summary>
        [KRPCProperty]
        public bool IgnoreForISP {
            get { return InternalPropellant.ignoreForIsp; }
        }

        /// <summary>
        /// Whether the propellant should be ignored for thrust curve calculations.
        /// </summary>
        [KRPCProperty]
        public bool IgnoreForThrustCurve {
            get { return InternalPropellant.ignoreForThrustCurve; }
        }

        /// <summary>
        /// Whether this propellant has a stack gauge.
        /// </summary>
        [KRPCProperty]
        public bool DrawStackGauge {
            get { return InternalPropellant.drawStackGauge; }
        }

        /// <summary>
        /// Whether the propellant is deprived.
        /// </summary>
        [KRPCProperty]
        public bool IsDeprived {
            get { return InternalPropellant.isDeprived; }
        }

        /// <summary>
        /// The propellant ratio.
        /// </summary>
        [KRPCProperty]
        public float Ratio {
            get { return InternalPropellant.ratio; }
        }

        /// <summary>
        /// The connected resources.
        /// </summary>
        [KRPCProperty]
        public IList<Resource> ConnectedResources {
            get {
                return InternalPropellant.connectedResources.Select (resource => new Resource (resource)).ToList ();
            }
        }
    }
}
