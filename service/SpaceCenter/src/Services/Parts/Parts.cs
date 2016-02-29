using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services.Parts;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Instances of this class are used to interact with the parts of a vessel.
    /// An instance can be obtained by calling <see cref="Vessel.Parts"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Parts : Equatable<Parts>
    {
        readonly Guid vesselId;

        internal Parts (global::Vessel vessel)
        {
            vesselId = vessel.id;
        }

        /// <summary>
        /// Check if the parts objects are for the same vessel.
        /// </summary>
        public override bool Equals (Parts obj)
        {
            return vesselId == obj.vesselId;
        }

        /// <summary>
        /// Hash the parts object.
        /// </summary>
        public override int GetHashCode ()
        {
            return vesselId.GetHashCode ();
        }

        /// <summary>
        /// The KSP vessel.
        /// </summary>
        public global::Vessel InternalVessel {
            get { return FlightGlobalsExtensions.GetVesselById (vesselId); }
        }

        /// <summary>
        /// A list of all of the vessels parts.
        /// </summary>
        [KRPCProperty]
        public IList<Part> All {
            get { return InternalVessel.parts.Select (x => new Part (x)).ToList (); }
        }

        /// <summary>
        /// The vessels root part.
        /// </summary>
        [KRPCProperty]
        public Part Root {
            get { return new Part (InternalVessel.rootPart); }
        }

        /// <summary>
        /// The part from which the vessel is controlled.
        /// </summary>
        [KRPCProperty]
        public Part Controlling {
            get { return new Part (InternalVessel.GetReferenceTransformPart () ?? InternalVessel.rootPart); }
            set {
                var part = value.InternalPart;
                if (part.HasModule <ModuleCommand> ()) {
                    part.Module<ModuleCommand> ().MakeReference ();
                } else if (part.HasModule <ModuleDockingNode> ()) {
                    part.Module<ModuleDockingNode> ().MakeReferenceTransform ();
                } else {
                    part.vessel.SetReferenceTransform (part);
                }
            }
        }

        /// <summary>
        /// A list of parts whose <see cref="Part.Name"/> is <paramref name="name"/>.
        /// </summary>
        /// <param name="name"></param>
        [KRPCMethod]
        public IList<Part> WithName (string name)
        {
            return All.Where (part => part.Name == name).ToList ();
        }

        /// <summary>
        /// A list of all parts whose <see cref="Part.Title"/> is <paramref name="title"/>.
        /// </summary>
        /// <param name="title"></param>
        [KRPCMethod]
        public IList<Part> WithTitle (string title)
        {
            return All.Where (part => part.Title == title).ToList ();
        }

        /// <summary>
        /// A list of all parts that contain a <see cref="Module"/> whose
        /// <see cref="Module.Name"/> is <paramref name="moduleName"/>.
        /// </summary>
        /// <param name="moduleName"></param>
        [KRPCMethod]
        public IList<Part> WithModule (string moduleName)
        {
            return All.Where (part => part.Modules.Any (module => module.Name == moduleName)).ToList ();
        }

        /// <summary>
        /// A list of all parts that are activated in the given <paramref name="stage"/>.
        /// </summary>
        /// <param name="stage"></param>
        [KRPCMethod]
        public IList<Part> InStage (int stage)
        {
            return All.Where (part => part.Stage == stage).ToList ();
        }

        /// <summary>
        /// A list of all parts that are decoupled in the given <paramref name="stage"/>.
        /// </summary>
        /// <param name="stage"></param>
        [KRPCMethod]
        public IList<Part> InDecoupleStage (int stage)
        {
            return All.Where (part => part.DecoupleStage == stage).ToList ();
        }

        /// <summary>
        /// A list of modules (combined across all parts in the vessel) whose
        /// <see cref="Module.Name"/> is <paramref name="moduleName"/>.
        /// </summary>
        /// <param name="moduleName"></param>
        [KRPCMethod]
        public IList<Module> ModulesWithName (string moduleName)
        {
            return All.SelectMany (part => part.Modules).Where (module => module.Name == moduleName).ToList ();
        }

        /// <summary>
        /// A list of all decouplers in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<Decoupler> Decouplers {
            get { return All.Where (part => part.IsDecoupler).Select (part => part.Decoupler).ToList (); }
        }

        /// <summary>
        /// A list of all docking ports in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<DockingPort> DockingPorts {
            get { return All.Where (part => part.IsDockingPort).Select (part => part.DockingPort).ToList (); }
        }

        /// <summary>
        /// A list of all resource converters in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<ResourceConverter> ResourceConverters {
            get { return All.Where (part => part.IsResourceConverter).Select (part => part.ResourceConverter).ToList (); }
        }

        /// <summary>
        /// A list of all resource harvesters in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<ResourceHarvester> ResourceHarvesters {
            get { return All.Where (part => part.IsResourceHarvester).Select (part => part.ResourceHarvester).ToList (); }
        }

        /// <summary>
        /// The first docking port in the vessel with the given port name, as returned by <see cref="DockingPort.Name"/>.
        /// Returns <c>null</c> if there are no such docking ports.
        /// </summary>
        /// <param name="name"></param>
        [KRPCMethod]
        public DockingPort DockingPortWithName (string name)
        {
            return All.Where (part => part.IsDockingPort).Select (part => part.DockingPort).FirstOrDefault (port => port.Name == name);
        }

        /// <summary>
        /// A list of all engines in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<Engine> Engines {
            get { return All.Where (part => part.IsEngine).Select (part => part.Engine).ToList (); }
        }

        /// <summary>
        /// A list of all landing gear attached to the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<LandingGear> LandingGear {
            get { return All.Where (part => part.IsLandingGear).Select (part => part.LandingGear).ToList (); }
        }

        /// <summary>
        /// A list of all landing legs attached to the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<LandingLeg> LandingLegs {
            get { return All.Where (part => part.IsLandingLeg).Select (part => part.LandingLeg).ToList (); }
        }

        /// <summary>
        /// A list of all launch clamps attached to the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<LaunchClamp> LaunchClamps {
            get { return All.Where (part => part.IsLaunchClamp).Select (part => part.LaunchClamp).ToList (); }
        }

        /// <summary>
        /// A list of all lights in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<Light> Lights {
            get { return All.Where (part => part.IsLight).Select (part => part.Light).ToList (); }
        }

        /// <summary>
        /// A list of all parachutes in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<Parachute> Parachutes {
            get { return All.Where (part => part.IsParachute).Select (part => part.Parachute).ToList (); }
        }

        /// <summary>
        /// A list of all radiators in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<Radiator> Radiators {
            get { return All.Where (part => part.IsRadiator).Select (part => part.Radiator).ToList (); }
        }

        /// <summary>
        /// A list of all reaction wheels in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<ReactionWheel> ReactionWheels {
            get { return All.Where (part => part.IsReactionWheel).Select (part => part.ReactionWheel).ToList (); }
        }

        /// <summary>
        /// A list of all sensors in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<Sensor> Sensors {
            get { return All.Where (part => part.IsSensor).Select (part => part.Sensor).ToList (); }
        }

        /// <summary>
        /// A list of all solar panels in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<SolarPanel> SolarPanels {
            get { return All.Where (part => part.IsSolarPanel).Select (part => part.SolarPanel).ToList (); }
        }
    }
}
