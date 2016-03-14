using KRPC.Client;
using KRPC.Client.Services.InfernalRobotics;
using System;
using System.Threading;
using System.Net;

class IR {
    public static void Main () {
        var connection = new Connection (name: "InfernalRobotics Example");
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
