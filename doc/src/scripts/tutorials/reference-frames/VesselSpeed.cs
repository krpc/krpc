using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class VesselSpeed
{
    public static void Main ()
    {
        var connection = new Connection (name : "Vessel speed");
        var vessel = connection.SpaceCenter ().ActiveVessel;
        var obtFrame = vessel.Orbit.Body.NonRotatingReferenceFrame;
        var srfFrame = vessel.Orbit.Body.ReferenceFrame;
        while (true) {
            var obtSpeed = vessel.Flight (obtFrame).Speed;
            var srfSpeed = vessel.Flight (srfFrame).Speed;
            Console.WriteLine (
                "Orbital speed = {0:F1} m/s, Surface speed = {1:F1} m/s",
                obtSpeed, srfSpeed);
            System.Threading.Thread.Sleep (1000);
        }
    }
}
