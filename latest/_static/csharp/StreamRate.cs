using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var flightInfo = connection.SpaceCenter().ActiveVessel.Flight();
        var altitude = connection.AddStream(() => flightInfo.MeanAltitude);
        // Update the stream at most 10 times a second
        altitude.Rate = 10;
    }
}
