using KRPC.Client;
using KRPC.Client.Services.InfernalRobotics;
using System;
using System.Threading;
using System.Net;

class InfernalRoboticsExample
{
    public static void Main ()
    {
        var connection = new KRPC.Client.Connection (name: "InfernalRobotics Example", address: IPAddress.Parse ("10.0.2.2"));
        var ir = connection.InfernalRobotics ();

        var group = ir.ServoGroupWithName ("MyGroup");
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
