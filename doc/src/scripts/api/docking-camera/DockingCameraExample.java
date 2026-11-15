import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Paths;
import krpc.client.Connection;
import krpc.client.services.DockingCamera;
import krpc.client.services.DockingCamera.Camera;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Part;
import krpc.client.services.StdLib.ImageFormat;


public class DockingCameraExample {
    public static void main(String[] args) throws IOException {
        Connection connection =
            Connection.newInstance("Docking Camera Example");
        Part part = SpaceCenter.newInstance(connection).getActiveVessel()
            .getParts().withName("OnboardCamera").get(0);
        Camera camera = DockingCamera.newInstance(connection).camera(part);
        Files.write(
            Paths.get("camera.png"), camera.image(ImageFormat.PNG, 75));
        connection.close();
    }
}
