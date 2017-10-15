using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var spaceCenter = connection.SpaceCenter();
        var control = spaceCenter.ActiveVessel.Control;
        var abort = connection.AddStream(() => control.Abort);
        lock (abort.Condition) {
            while (!abort.Get())
                abort.Wait();
        }
    }
}
