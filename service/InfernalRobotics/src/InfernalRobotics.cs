using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.InfernalRobotics
{
    /// <summary>
    /// Provides functionality to interact with Infernal Robotics Next.
    /// </summary>
    [KRPCService (Id = ServiceInfo.Id, GameScene = GameScene.Flight)]
    [RequiresMod (typeof (API))]
    public static class InfernalRobotics
    {
        /// <summary>
        /// Whether Infernal Robotics is installed.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.All)]
        public static bool Available {
            get { return ModAPI.Get<API> ().IsAvailable; }
        }

        /// <summary>
        /// A list of all the servo groups in the given <paramref name="vessel"/>.
        /// </summary>
        /// <remarks>
        /// Works for any loaded vessel, not just the active one. Groups on a non-active vessel
        /// support movement and per-servo control, but not preset, key, speed-factor or
        /// expanded state, which are only tracked by Infernal Robotics for the active vessel.
        /// </remarks>
        [KRPCProcedure]
        public static IList<ServoGroup> ServoGroups (SpaceCenter.Services.Vessel vessel)
        {
            return API.ServoGroupsForVessel (vessel.InternalVessel)
                .Select (x => new ServoGroup (vessel, x.Name)).ToList ();
        }

        /// <summary>
        /// Returns the servo group in the given <paramref name="vessel"/> with the given <paramref name="name"/>,
        /// or <c>null</c> if none exists. If multiple servo groups have the same name, only one of them is returned.
        /// </summary>
        /// <param name="vessel">Vessel to check.</param>
        /// <param name="name">Name of servo group to find.</param>
        [KRPCProcedure (Nullable = true)]
        public static ServoGroup ServoGroupWithName (SpaceCenter.Services.Vessel vessel, string name)
        {
            var servoGroup = API.ServoGroupsForVessel (vessel.InternalVessel).FirstOrDefault (x => x.Name == name);
            return servoGroup != null ? new ServoGroup (vessel, servoGroup.Name) : null;
        }

        /// <summary>
        /// Returns the servo in the given <paramref name="vessel"/> with the given <paramref name="name"/> or
        /// <c>null</c> if none exists. If multiple servos have the same name, only one of them is returned.
        /// </summary>
        /// <param name="vessel">Vessel to check.</param>
        /// <param name="name">Name of the servo to find.</param>
        [KRPCProcedure (Nullable = true)]
        public static Servo ServoWithName (SpaceCenter.Services.Vessel vessel, string name)
        {
            var servo = API.ServosForVessel (vessel.InternalVessel).FirstOrDefault (x => x.Name == name);
            return servo != null ? new Servo (servo) : null;
        }
    }
}
