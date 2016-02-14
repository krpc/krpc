import java.io.IOException;

import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.InfernalRobotics;
import krpc.client.services.InfernalRobotics.ControlGroup;
import krpc.client.services.InfernalRobotics.Servo;

public class InfernalRoboticsExample {
    public static void main(String[] args) throws IOException, RPCException, InterruptedException {

        Connection connection = Connection.newInstance("InfernalRobotics Example");
        InfernalRobotics ir = InfernalRobotics.newInstance(connection);

        ControlGroup group = ir.servoGroupWithName("MyGroup");
        if (group == null) {
            System.out.println("Group not found");
            return;
        }

        for (Servo servo : group.getServos())
            System.out.println(servo.getName() + " " + servo.getPosition());

        group.moveRight();
        Thread.sleep(1000);
        group.stop();
    }
}