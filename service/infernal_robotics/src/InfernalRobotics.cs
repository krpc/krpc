using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace KRPC.InfernalRobotics
{
    /// <summary>
    /// This service provides functionality to interact with
    /// <a href="https://forum.kerbalspaceprogram.com/index.php?/topic/184787-infernal-robotics-next/">Infernal Robotics</a>.
    /// </summary>
    [KRPCService (Id = 4, GameScene = GameScene.Flight)]
    public static class InfernalRobotics
    {
        static void CheckAPI ()
        {
            if (!Ready)
                throw new InvalidOperationException(
                    "Infernal Robotics is not ready. Does the vessel have an Infernal Robotics part?");
        }

        /// <summary>
        /// Whether Infernal Robotics is installed.
        /// </summary>
        [KRPCProperty]
        public static bool Available {
            get { return IRWrapper.AssemblyExists; }
        }

        /// <summary>
        /// Whether Infernal Robotics API is ready.
        /// </summary>
        [KRPCProperty]
        public static bool Ready{
            get { return IRWrapper.APIReady; }
        }

        /// <summary>
        /// A list of all the servo groups in the given <paramref name="vessel"/>.
        /// </summary>
        [KRPCProcedure]
        public static IList<ServoGroup> ServoGroups (SpaceCenter.Services.Vessel vessel)
        {
            CheckAPI ();
            return IRWrapper.IRController.ServoGroups.Where (x => x.Vessel.id == vessel.Id).Select (x => new ServoGroup (x)).ToList ();
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
            CheckAPI ();
            var servoGroup = IRWrapper.IRController.ServoGroups.FirstOrDefault (x => x.Vessel.id == vessel.Id && x.Name == name);
            return servoGroup != null ? new ServoGroup (servoGroup) : null;
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
            CheckAPI ();
            var servo = IRWrapper.IRController.ServoGroups
                .Where (x => x.Vessel.id == vessel.Id)
                .SelectMany (x => x.Servos)
                .FirstOrDefault (x => x.Name == name);
            return servo != null ? new Servo (servo) : null;
        }
    }
}
