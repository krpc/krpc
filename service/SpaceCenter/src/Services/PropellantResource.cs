using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class PropellantResource : Equatable<PropellantResource>
    {
        readonly int resourceId;
        readonly int partId;

        internal PropellantResource (Propellant propellantResource, Part underlyingPart)
        {
            resourceId = propellantResource.id;
            partId = underlyingPart.flightID;
        }

        #region implemented abstract members of Equatable

        public override bool Equals (PropellantResource obj)
        {
            return obj.resourceId == resourceId;
        }

        public override int GetHashCode ()
        {
            return resourceId.GetHashCode ();
        }

        #endregion

        /// <summary>
        /// The KSP part.
        /// </summary>
        public Part InternalPart {
            get { return FlightGlobals.FindPartByID (partId); }
        }

        /// <summary>
        /// The KSP propellant resource.
        /// </summary>
        public Propellant InternalPropellant {
            get
            { 
                ModuleEngines engineModule = InternalPart.GetComponent<ModuleEngines> () as ModuleEngines;
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
        /// The current required amount of propellant
        /// </summary>
        [KRPCProperty]
        public double CurrentRequirement {
            get
            { return InternalPropellant.currentRequirement; }
        }

        /// <summary>
        /// The total propellant resource available
        /// </summary>
        [KRPCProperty]
        public double TotalResourceAvailable {
            get
            { return InternalPropellant.totalResourceAvailable; }
        }

        /// <summary>
        /// The total propellant resource capacity
        /// </summary>
        [KRPCProperty]
        public double TotalResourceCapacity {
            get
            { return InternalPropellant.totalResourceCapacity; }
        }

        /// <summary>
        /// If this propellant should be ignored for Isp calculations
        /// </summary>
        [KRPCProperty]
        public bool IgnoreForIsp {
            get
            { return InternalPropellant.ignoreForIsp; }
        }

        /// <summary>
        /// If this propellant should be ignored for Thrust curve calculations
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

        [KRPCProperty]
        public IList<Resource> ConnectedResources {
            get {
                List<PartResource> resources = InternalPropellant.connectedResources;
                List<Resource> connectedResources = new List<Resource> ();
                foreach (PartResource resource in resources)
                {
                    connectedResources.Add (new Resource (resource));
                }
                return connectedResources;
            }
        }

    }
}

