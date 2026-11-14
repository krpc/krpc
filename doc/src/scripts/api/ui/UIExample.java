import krpc.client.Connection;
import krpc.client.services.StdLib.ColorRGBA;
import krpc.client.services.UI;
import krpc.client.services.UI.MessagePosition;


public class UIExample {
    public static void main(String[] args) {
        Connection connection = Connection.newInstance("User Interface Example");
        UI.newInstance(connection).message(
            "Hello, World!", 10, MessagePosition.TOP_CENTER,
            new ColorRGBA(1f, 0.92f, 0.016f, 1f), 20);
        connection.close();
    }
}
