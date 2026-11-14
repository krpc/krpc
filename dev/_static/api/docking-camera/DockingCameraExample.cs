using System.IO;
using KRPC.Client;
using KRPC.Client.Services.DockingCamera;
using KRPC.Client.Services.SpaceCenter;

class DockingCameraExample
{
    public static void Main ()
    {
        using (var connection = new Connection ("Docking Camera Example")) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            var part = vessel.Parts.WithName ("OnboardCamera") [0];
            var camera = connection.DockingCamera ().Camera (part);
            File.WriteAllBytes ("camera.png", camera.Image ());
        }
    }
}
