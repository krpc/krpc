using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class EditorTorque {
    public static void Main () {
        using (var connection = new Connection ()) {
            var spaceCenter = connection.SpaceCenter ();
            var body = spaceCenter.Bodies ["Kerbin"];
            // [docs]
            var vessel = spaceCenter.Editor.Vessel;
            var flight = vessel.Flight (vessel.ReferenceFrame);
            // The vessel frame's z axis points out of the bottom, so up is
            // its -z axis
            var side = Math.Sqrt (0.5);
            flight.SurfaceReferenceFrame = ReferenceFrame.CreateRelative (
                connection, vessel.ReferenceFrame,
                rotation: Tuple.Create (0.0, side, 0.0, side));
            var torque = flight.BuoyantTorqueAt (
                body.OceanDensity, body.SurfaceGravity);
            // [/docs]
            Console.WriteLine (
                "Torque = ({0:F0}, {1:F0}, {2:F0}) N m",
                torque.Item1, torque.Item2, torque.Item3);
        }
    }
}
