using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services.Parts;
using KRPC.Utils;

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
        /// A list of all control surfaces in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<ControlSurface> ControlSurfaces {
            get { return All.Where (ControlSurface.Is).Select (part => new ControlSurface (part)).ToList (); }
        }

        /// <summary>
        /// A list of all cargo bays in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<CargoBay> CargoBays {
            get { return All.Where (CargoBay.Is).Select (part => new CargoBay (part)).ToList (); }
        }

        /// <summary>
        /// A list of all decouplers in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<Decoupler> Decouplers {
            get { return All.Where (Decoupler.Is).Select (part => new Decoupler (part)).ToList (); }
        }

        /// <summary>
        /// A list of all docking ports in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<DockingPort> DockingPorts {
            get { return All.Where (DockingPort.Is).Select (part => new DockingPort (part)).ToList (); }
        }

        /// <summary>
        /// The first docking port in the vessel with the given port name, as returned by <see cref="DockingPort.Name"/>.
        /// Returns <c>null</c> if there are no such docking ports.
        /// </summary>
        /// <param name="name"></param>
        [KRPCMethod]
        public DockingPort DockingPortWithName (string name)
        {
            return All.Where (DockingPort.Is).Select (part => new DockingPort (part)).FirstOrDefault (port => port.Name == name);
        }

        /// <summary>
        /// A list of all engines in the vessel.
        /// </summary>
        /// <remarks>
        /// This includes any part that generates thrust. This covers many different types of engine,
        /// including liquid fuel rockets, solid rocket boosters, jet engines and RCS thrusters.
        /// </remarks>
        [KRPCProperty]
        public IList<Engine> Engines {
            get { return All.Where (Engine.Is).Select (part => new Engine (part)).ToList (); }
        }

        /// <summary>
        /// A list of all fairings in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<Fairing> Fairings {
            get { return All.Where (Fairing.Is).Select (part => new Fairing (part)).ToList (); }
        }

        /// <summary>
        /// A list of all intakes in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<Intake> Intakes {
            get { return All.Where (Intake.Is).Select (part => new Intake (part)).ToList (); }
        }

        /// <summary>
        /// A list of all landing gear attached to the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<LandingGear> LandingGear {
            get { return All.Where (Services.Parts.LandingGear.Is).Select (part => new LandingGear (part)).ToList (); }
        }

        /// <summary>
        /// A list of all landing legs attached to the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<LandingLeg> LandingLegs {
            get { return All.Where (LandingLeg.Is).Select (part => new LandingLeg (part)).ToList (); }
        }

        /// <summary>
        /// A list of all launch clamps attached to the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<LaunchClamp> LaunchClamps {
            get { return All.Where (LaunchClamp.Is).Select (part => new LaunchClamp (part)).ToList (); }
        }

        /// <summary>
        /// A list of all lights in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<Light> Lights {
            get { return All.Where (Light.Is).Select (part => new Light (part)).ToList (); }
        }

        /// <summary>
        /// A list of all parachutes in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<Parachute> Parachutes {
            get { return All.Where (Parachute.Is).Select (part => new Parachute (part)).ToList (); }
        }

        /// <summary>
        /// A list of all radiators in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<Radiator> Radiators {
            get { return All.Where (Radiator.Is).Select (part => new Radiator (part)).ToList (); }
        }

        /// <summary>
        /// A list of all RCS blocks/thrusters in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<RCS> RCS {
            get { return All.Where (Services.Parts.RCS.Is).Select (part => new RCS (part)).ToList (); }
        }

        /// <summary>
        /// A list of all reaction wheels in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<ReactionWheel> ReactionWheels {
            get { return All.Where (ReactionWheel.Is).Select (part => new ReactionWheel (part)).ToList (); }
        }

        /// <summary>
        /// A list of all resource converters in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<ResourceConverter> ResourceConverters {
            get { return All.Where (ResourceConverter.Is).Select (part => new ResourceConverter (part)).ToList (); }
        }

        /// <summary>
        /// A list of all resource harvesters in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<ResourceHarvester> ResourceHarvesters {
            get { return All.Where (ResourceHarvester.Is).Select (part => new ResourceHarvester (part)).ToList (); }
        }

        /// <summary>
        /// A list of all sensors in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<Sensor> Sensors {
            get { return All.Where (Sensor.Is).Select (part => new Sensor (part)).ToList (); }
        }

        /// <summary>
        /// A list of all solar panels in the vessel.
        /// </summary>
        [KRPCProperty]
        public IList<SolarPanel> SolarPanels {
            get { return All.Where (SolarPanel.Is).Select (part => new SolarPanel (part)).ToList (); }
        }
    }
}
