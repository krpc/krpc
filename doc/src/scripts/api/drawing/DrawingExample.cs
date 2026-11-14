using System;
using System.Threading;
using KRPC.Client;
using KRPC.Client.Services.Drawing;
using KRPC.Client.Services.SpaceCenter;

class DrawingExample
{
    public static void Main ()
    {
        using (var connection = new Connection ("Drawing Example")) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            connection.Drawing ().AddDirectionFromCom (
                Tuple.Create (0.0, 1.0, 0.0), vessel.ReferenceFrame);
            Thread.Sleep (10000);
        }
    }
}
