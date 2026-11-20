using KRPC.Client;
using KRPC.Client.Services.UI;

class UIExample
{
    public static void Main ()
    {
        using (var connection = new Connection ("User Interface Example")) {
            connection.UI ().Message ("Hello, World!", duration: 10);
        }
    }
}
