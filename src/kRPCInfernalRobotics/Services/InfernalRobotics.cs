using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace KRPCInfernalRobotics.Services
{
    [KRPCService (GameScene = GameScene.Flight)]
    public static class InfernalRobotics
    {
        [KRPCProperty]
        public static IList<ControlGroup> ServoGroups {
            get {
                if (!IRWrapper.APIReady)
                    throw new InvalidOperationException ("InfernalRobotics is not available");
                return IRWrapper.IRController.ServoGroups.Select (x => new ControlGroup (x)).ToList ();
            }
        }
    }
}
