using System;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;

class SubOrbitalFlight
{
    public static void Main ()
    {
        var conn = new Connection ("Sub-orbital flight");

        var vessel = conn.SpaceCenter ().ActiveVessel;

        vessel.AutoPilot.TargetPitchAndHeading (90, 90);
        vessel.AutoPilot.Engage ();
        vessel.Control.Throttle = 1;
        System.Threading.Thread.Sleep (1000);

        Console.WriteLine ("Launch!");
        vessel.Control.ActivateNextStage ();

        {
            var solidFuel = Connection.GetCall(() => vessel.Resources.Amount("SolidFuel"));
            var expr = Expression.LessThan(
                conn, Expression.Call(conn, solidFuel), Expression.ConstantFloat(conn, 0.1f));
            var evnt = conn.KRPC().AddEvent(expr);
            lock (evnt.Condition) {
                evnt.Wait();
            }
        }

        Console.WriteLine ("Booster separation");
        vessel.Control.ActivateNextStage ();

        {
            var meanAltitude = Connection.GetCall(() => vessel.Flight(null).MeanAltitude);
            var expr = Expression.GreaterThan(
                conn, Expression.Call(conn, meanAltitude), Expression.ConstantDouble(conn, 10000));
            var evnt = conn.KRPC().AddEvent(expr);
            lock (evnt.Condition) {
                evnt.Wait();
            }
        }

        Console.WriteLine ("Gravity turn");
        vessel.AutoPilot.TargetPitchAndHeading (60, 90);

        {
            var apoapsisAltitude = Connection.GetCall(() => vessel.Orbit.ApoapsisAltitude);
            var expr = Expression.GreaterThan(
                conn, Expression.Call(conn, apoapsisAltitude), Expression.ConstantDouble(conn, 100000));
            var evnt = conn.KRPC().AddEvent(expr);
            lock (evnt.Condition) {
                evnt.Wait();
            }
        }

        Console.WriteLine ("Launch stage separation");
        vessel.Control.Throttle = 0;
        System.Threading.Thread.Sleep (1000);
        vessel.Control.ActivateNextStage ();
        vessel.AutoPilot.Disengage ();

        {
            var srfAltitude = Connection.GetCall(() => vessel.Flight(null).SurfaceAltitude);
            var expr = Expression.LessThan(
                conn, Expression.Call(conn, srfAltitude), Expression.ConstantDouble(conn, 1000));
            var evnt = conn.KRPC().AddEvent(expr);
            lock (evnt.Condition) {
                evnt.Wait();
            }
        }

        vessel.Control.ActivateNextStage ();

        while (vessel.Flight (vessel.Orbit.Body.ReferenceFrame).VerticalSpeed < -0.1) {
            Console.WriteLine ("Altitude = {0:F1} meters", vessel.Flight ().SurfaceAltitude);
            System.Threading.Thread.Sleep (1000);
        }
        Console.WriteLine ("Landed!");
        conn.Dispose();
    }
}
