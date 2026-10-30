using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        try {
            connection.SpaceCenter().Load("missing");
        } catch (ArgumentException exception) {
            Console.WriteLine("Failed to load the save: " + exception.Message);
        }
    }
}
