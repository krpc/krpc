using System;
using System.Linq;
using System.Text;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services
{
    [KRPCClass(Service = "SpaceCenter")]
    public class ModuleEngine : PartModule
    {
        global::ModuleEngines moduleEngine;

        internal ModuleEngine(global::ModuleEngines moduleEngine)
            : base(moduleEngine)
        {
            this.moduleEngine = moduleEngine;
        }

        public override int GetHashCode ()
        {
            return moduleEngine.GetHashCode();
        }

        [KRPCProperty]
        public bool AllowRestart
        {
            get { return moduleEngine.allowRestart; }
        }

        [KRPCProperty]
        public bool AllowShutdown
        {
            get { return moduleEngine.allowShutdown; }
        }

        [KRPCProperty]
        public float CurrentThrottle
        {
            get { return moduleEngine.currentThrottle; }
        }

        [KRPCProperty]
        public bool ThrottleLocked
        {
            get { return moduleEngine.throttleLocked; }
        }

        [KRPCProperty]
        public float ThrustLimit
        {
            get { return moduleEngine.thrustPercentage; }
            set { moduleEngine.thrustPercentage = value; }
        }

        [KRPCProperty]
        public bool EngineIgnited
        {
            get { return moduleEngine.EngineIgnited; }
        }

        [KRPCProperty]
        public bool Flameout
        {
            get { return moduleEngine.flameout; }
        }
        
        [KRPCProperty]
        public float Isp
        {
            get { return moduleEngine.realIsp; }
        }

        [KRPCProperty]
        public float MinThrust
        {
            get { return moduleEngine.minThrust; }
        }

        [KRPCProperty]
        public float MaxThrust
        {
            get { return moduleEngine.maxThrust; }
        }

        [KRPCProperty]
        public Vessel Vessel
        {
            get { return new Vessel(moduleEngine.vessel); } 
        }

        [KRPCProperty]
        public bool IsActivated
        {
            get { return moduleEngine.engineShutdown; } 
        }

        [KRPCMethod]
        public void Activate()
        {
            moduleEngine.Activate();
        }

        [KRPCMethod]
        public void Shutdown()
        {
            moduleEngine.Shutdown();
        }
    }
}
