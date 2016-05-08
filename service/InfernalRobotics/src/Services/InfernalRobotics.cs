using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace KRPC.InfernalRobotics.Services
{
    /// <summary>
    /// This service provides functionality to interact with
    /// <a href="http://forum.kerbalspaceprogram.com/index.php?/topic/104535-105-magic-smoke-industries-infernal-robotics-0214/">Infernal Robotics</a>.
    /// </summary>
    [KRPCService (GameScene = GameScene.Flight)]
    public static class InfernalRobotics
    {
        static void CheckAPI ()
        {
            if (!IRWrapper.APIReady)
                throw new InvalidOperationException ("Infernal Robotics is not available");
        }

        /// <summary>
        /// A list of all the servo groups in the active vessel.
        /// </summary>
        [KRPCProperty]
        public static IList<ServoGroup> ServoGroups {
            get {
                CheckAPI ();
                return IRWrapper.IRController.ServoGroups.Select (x => new ServoGroup (x)).ToList ();
            }
        }

        /// <summary>
        /// Returns the servo group with the given <paramref name="name"/> or <c>null</c> if none
        /// exists. If multiple servo groups have the same name, only one of them is returned.
        /// </summary>
        /// <param name="name">Name of servo group to find.</param>
        [KRPCProcedure]
        public static ServoGroup ServoGroupWithName (string name)
        {
            CheckAPI ();
            var servoGroup = IRWrapper.IRController.ServoGroups.FirstOrDefault (x => x.Name == name);
            return servoGroup != null ? new ServoGroup (servoGroup) : null;
        }

        /// <summary>
        /// Returns the servo with the given <paramref name="name"/>, from all servo groups, or
        /// <c>null</c> if none exists. If multiple servos have the same name, only one of them is returned.
        /// </summary>
        /// <param name="name">Name of the servo to find.</param>
        [KRPCProcedure]
        public static Servo ServoWithName (string name)
        {
            CheckAPI ();
            var servo = IRWrapper.IRController.ServoGroups.SelectMany (x => x.Servos).FirstOrDefault (x => x.Name == name);
            return servo != null ? new Servo (servo) : null;
        }
    }
}
