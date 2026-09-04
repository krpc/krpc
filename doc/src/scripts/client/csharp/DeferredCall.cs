using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main () {
        var connection = new Connection ();
        var spaceCenter = connection.SpaceCenter ();

        // WarpTo pauses execution and resumes on a later tick, so the function
        // starts it with Function.Defer and returns without waiting for it
        connection.RunFunction (
            () => Function.Defer (
                () => spaceCenter.WarpTo (spaceCenter.UT + 60, 100000, 2)));

        Console.WriteLine ("The warp is running, and the call has returned");
    }
}
