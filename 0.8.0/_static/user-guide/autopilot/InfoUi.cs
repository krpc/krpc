using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class InfoUi {
    public static void Main () {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            // [docs]
            vessel.AutoPilot.ShowInfoUI = true;
            // [/docs]
        }
    }
}
