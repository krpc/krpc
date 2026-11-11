using System;
using KRPC.Client;
using KRPC.Client.Services.MechJeb;
using KRPC.Client.Services.SpaceCenter;

class MechJebExample
{
    public static void Main ()
    {
        using (var connection = new Connection (name: "MechJeb Example")) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            var ascent = connection.MechJeb ().AscentAutopilot;

            ascent.DesiredOrbitAltitude = 100000;
            ascent.DesiredInclination = 6;
            ascent.Autostage = true;
            ascent.Enabled = true;

            vessel.Control.ActivateNextStage ();

            var evnt = connection.AddEvent (() => !ascent.Enabled);
            lock (evnt.Condition) {
                evnt.Wait ();
            }

            Console.WriteLine ("Launch complete");
        }
    }
}
