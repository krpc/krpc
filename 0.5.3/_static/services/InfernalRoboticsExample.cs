using System;
using System.Net;
using System.Threading;
using KRPC.Client;
using KRPC.Client.Services.InfernalRobotics;
using KRPC.Client.Services.SpaceCenter;

class InfernalRoboticsExample
{
    public static void Main ()
    {
        using (var connection = new Connection (
            name: "InfernalRobotics Example")) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            var ir = connection.InfernalRobotics ();

            var group = ir.ServoGroupWithName (vessel, "MyGroup");
            if (group == null) {
                Console.WriteLine ("Group not found");
                return;
            }

            foreach (var servo in group.Servos)
                Console.WriteLine (servo.Name + " " + servo.Position);

            group.MoveRight ();
            Thread.Sleep (1000);
            group.Stop ();
        }
    }
}
