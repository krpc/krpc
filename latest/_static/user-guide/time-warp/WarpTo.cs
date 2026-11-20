using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class WarpTo {
    public static void Main () {
        using (var connection = new Connection ()) {
            var spaceCenter = connection.SpaceCenter ();
            var vessel = spaceCenter.ActiveVessel;
            // [docs]
            var apoapsisUT = spaceCenter.UT + vessel.Orbit.TimeToApoapsis;
            spaceCenter.WarpTo (apoapsisUT - 10);
            // [/docs]
        }
    }
}
