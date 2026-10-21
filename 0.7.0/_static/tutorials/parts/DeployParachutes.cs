using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class DeployParachutes
{
    public static void Main ()
    {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            foreach (var parachute in vessel.Parts.Parachutes)
                parachute.Deploy ();
        }
    }
}
