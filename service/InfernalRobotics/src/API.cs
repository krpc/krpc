using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KRPC.Utils;

namespace KRPC.InfernalRobotics
{
    // Binds to the members of Infernal Robotics Next 3.1 by reflection. IR's controller holds
    // the servo groups of the active vessel once it has loaded them. The groups of any other
    // loaded vessel are built from its servo modules, which move on any loaded, off-rails vessel.
    sealed class API : ModAPI
    {
        public API () : base (ServiceInfo.Mods.InfernalRobotics.Name, ServiceInfo.Mods.InfernalRobotics.Assembly)
        {
        }

        // The module that makes a part a servo. IServo.UID is the part's craft id, so the mod
        // names a servo by its part, and a part carries at most one.
        const string servoModuleName = "ModuleIRServo_v3";

        static Func<object> controller;
        static Func<bool> controllerReady;
        static Func<object, IList> controllerGroups;
        static Func<global::Vessel, object> vesselServos;

        // The groups a servo module belongs to, encoded as described in SynthesizedServoGroup
        internal static Field<object, string> GroupName;

        protected override void Bind (ModBinder mod)
        {
            var controllerType = mod.Type ("InfernalRobotics_v3.Command.Controller");
            controller = controllerType.StaticGetter<object> ("Instance");
            controllerReady = controllerType.StaticGetter<bool> ("APIReady");
            controllerGroups = controllerType.Getter<object, IList> ("ServoGroups");
            vesselServos = mod.Type ("InfernalRobotics_v3.Utility.ServoHelper").Method<Func<global::Vessel, object>> ("ToServos");
            GroupName = mod.Type ("InfernalRobotics_v3.Module.ModuleIRServo_v3").Field<object, string> ("groupName");
            ControllerServoGroup.Bind (mod);
            IRServo.Bind (mod);
        }

        /// <summary>
        /// The groups IR's controller holds for the vessel, or none if it has yet to load them.
        /// </summary>
        static IList<IServoGroup> ControllerGroups (global::Vessel vessel)
        {
            var result = new List<IServoGroup> ();
            var instance = controller ();
            if (instance == null || !controllerReady ())
                return result;
            var groups = controllerGroups (instance);
            if (groups == null)
                return result;
            foreach (var group in groups) {
                var wrapped = new ControllerServoGroup (group);
                var groupVessel = wrapped.Vessel;
                if (groupVessel != null && groupVessel.id == vessel.id)
                    result.Add (wrapped);
            }
            return result;
        }

        /// <summary>
        /// The servo on a part, or null if it has none.
        /// </summary>
        internal static IRServo ServoOnPart (global::Part part)
        {
            if (part == null || !ModAPI.Get<API> ().IsAvailable)
                return null;
            var modules = part.Modules;
            for (var i = 0; i < modules.Count; i++) {
                var module = modules [i];
                if (module != null && module.moduleName == servoModuleName)
                    return new IRServo (module);
            }
            return null;
        }

        /// <summary>
        /// The servos of a vessel, from IR's controller when it holds the vessel's groups.
        /// </summary>
        internal static IList<IRServo> ServosForVessel (global::Vessel vessel)
        {
            var result = new List<IRServo> ();
            var seen = new HashSet<IRServo> ();
            foreach (var group in ControllerGroups (vessel)) {
                foreach (var servo in group.Servos) {
                    if (seen.Add (servo))
                        result.Add (servo);
                }
            }
            if (result.Count > 0)
                return result;
            return VesselServos (vessel).Select (x => new IRServo (x)).ToList ();
        }

        /// <summary>
        /// The servo groups of a vessel, from IR's controller when it holds them.
        /// </summary>
        internal static IList<IServoGroup> ServoGroupsForVessel (global::Vessel vessel)
        {
            var result = ControllerGroups (vessel);
            return result.Count > 0 ? result : SynthesizedServoGroup.Build (vessel, VesselServos (vessel));
        }

        // The servo modules of a vessel
        static IList<object> VesselServos (global::Vessel vessel)
        {
            var servos = vesselServos (vessel) as IEnumerable;
            return servos == null ? new List<object> () : servos.Cast<object> ().ToList ();
        }
    }
}
