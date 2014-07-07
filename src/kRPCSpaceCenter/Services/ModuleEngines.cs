using System;
using System.Linq;
using System.Text;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services
{
    [KRPCClass(Service = "SpaceCenter")]
    public class ModuleEngines : PartModule
    {
        global::ModuleEngines moduleEngines;

        internal ModuleEngines(global::ModuleEngines moduleEngines)
            : base(moduleEngines)
        {
            this.moduleEngines = moduleEngines;
        }

        public override int GetHashCode ()
        {
            return moduleEngines.GetHashCode();
        }

        [KRPCProperty]
        public bool AllowRestart
        {
            get { return moduleEngines.allowRestart; }
        }

        [KRPCProperty]
        public bool AllowShutdown
        {
            get { return moduleEngines.allowShutdown; }
        }

        [KRPCProperty]
        public float CurrentThrottle
        {
            get { return moduleEngines.currentThrottle; }
        }

        [KRPCProperty]
        public bool ThrottleLocked
        {
            get { return moduleEngines.throttleLocked; }
        }

        [KRPCProperty]
        public float ThrustLimit
        {
            get { return moduleEngines.thrustPercentage; }
            set { moduleEngines.thrustPercentage = value; }
        }

        [KRPCProperty]
        public bool EngineIgnited
        {
            get { return moduleEngines.EngineIgnited; }
        }

        [KRPCProperty]
        public bool Flameout
        {
            get { return moduleEngines.flameout; }
        }
        
        [KRPCProperty]
        public float Isp
        {
            get { return moduleEngines.realIsp; }
        }

        [KRPCProperty]
        public float MinThrust
        {
            get { return moduleEngines.minThrust; }
        }

        [KRPCProperty]
        public float MaxThrust
        {
            get { return moduleEngines.maxThrust; }
        }

        [KRPCProperty]
        public Vessel Vessel
        {
            get { return new Vessel(moduleEngines.vessel); } 
        }

        [KRPCProperty]
        public bool IsActivated
        {
            get { return moduleEngines.engineShutdown; } 
        }

        [KRPCMethod]
        public void Activate()
        {
            moduleEngines.Activate();
        }

        [KRPCMethod]
        public void Shutdown()
        {
            moduleEngines.Shutdown();
        }
    }
}
