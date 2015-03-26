using KRPC.Service.Attributes;
using KRPC.Utils;
using System.Collections.Generic;
using System.Linq;
using KRPCSpaceCenter.Services.Parts;

namespace KRPCSpaceCenter.Services.Parts
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Parts : Equatable<Parts>
    {
        readonly global::Vessel vessel;

        internal Parts (global::Vessel vessel)
        {
            this.vessel = vessel;
        }

        public override bool Equals (Parts obj)
        {
            return vessel == obj.vessel;
        }

        public override int GetHashCode ()
        {
            return vessel.GetHashCode ();
        }

        [KRPCProperty]
        public IList<Part> All {
            get { return vessel.parts.Select (x => new Part (x)).ToList (); }
        }

        [KRPCProperty]
        public Part Root {
            get { return new Part (vessel.rootPart); }
        }

        [KRPCMethod]
        public IList<Part> WithName (string name)
        {
            return All.Where (part => part.Name == name).ToList ();
        }

        [KRPCMethod]
        public IList<Part> WithTitle (string title)
        {
            return All.Where (part => part.Title == title).ToList ();
        }

        [KRPCMethod]
        public IList<Part> WithModule (string moduleName)
        {
            return All.Where (part => part.Modules.Any (module => module.Name == moduleName)).ToList ();
        }

        [KRPCMethod]
        public IList<Part> InStage (int stage)
        {
            return All.Where (part => part.Stage == stage).ToList ();
        }

        [KRPCMethod]
        public IList<Part> InDecoupleStage (int stage)
        {
            return All.Where (part => part.DecoupleStage == stage).ToList ();
        }

        [KRPCMethod]
        public IList<Module> ModulesWithName (string moduleName)
        {
            return All.SelectMany (part => part.Modules).Where (module => module.Name == moduleName).ToList ();
        }

        [KRPCProperty]
        public IList<Decoupler> Decouplers {
            get { return All.Where (part => part.IsDecoupler).Select (part => part.Decoupler).ToList (); }
        }

        [KRPCProperty]
        public IList<Engine> Engines {
            get { return All.Where (part => part.IsEngine).Select (part => part.Engine).ToList (); }
        }

        [KRPCProperty]
        public IList<LaunchClamp> LaunchClamps {
            get { return All.Where (part => part.IsLaunchClamp).Select (part => part.LaunchClamp).ToList (); }
        }

        [KRPCProperty]
        public IList<Light> Lights {
            get { return All.Where (part => part.IsLight).Select (part => part.Light).ToList (); }
        }

        [KRPCProperty]
        public IList<Parachute> Parachutes {
            get { return All.Where (part => part.IsParachute).Select (part => part.Parachute).ToList (); }
        }

        [KRPCProperty]
        public IList<ReactionWheel> ReactionWheels {
            get { return All.Where (part => part.IsReactionWheel).Select (part => part.ReactionWheel).ToList (); }
        }

        [KRPCProperty]
        public IList<Sensor> Sensors {
            get { return All.Where (part => part.IsSensor).Select (part => part.Sensor).ToList (); }
        }

        [KRPCProperty]
        public IList<SolarPanel> SolarPanels {
            get { return All.Where (part => part.IsSolarPanel).Select (part => part.SolarPanel).ToList (); }
        }
    }
}
