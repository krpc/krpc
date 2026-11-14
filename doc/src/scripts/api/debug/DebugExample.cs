using KRPC.Client;
using KRPC.Client.Services.Debug;

class DebugExample
{
    public static void Main ()
    {
        using (var connection = new Connection ("Debug Example")) {
            connection.Debug ().InfinitePropellant = true;
        }
    }
}
