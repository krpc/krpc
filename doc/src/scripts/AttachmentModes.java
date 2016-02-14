import java.io.IOException;
import java.util.ArrayDeque;
import java.util.Deque;

import org.javatuples.Pair;

import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Part;
import krpc.client.services.SpaceCenter.Vessel;

public class AttachmentModes {
    public static void main(String[] args) throws IOException, RPCException {
        Connection connection = Connection.newInstance();
        Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
        Part root = vessel.getParts().getRoot();
        Deque<Pair<Part, Integer>> stack = new ArrayDeque<Pair<Part, Integer>>();
        stack.push(new Pair<Part, Integer>(root, 0));
        while (stack.size() > 0) {
            Pair<Part, Integer> item = stack.pop();
            Part part = item.getValue0();
            int depth = item.getValue1();
            String prefix = "";
            for (int i = 0; i < depth; i++)
                prefix += " ";
            String attachMode = part.getAxiallyAttached() ? "axial" : "radial";
            System.out.println(prefix + part.getTitle() + " - " + attachMode);
            for (Part child : part.getChildren())
                stack.push(new Pair<Part, Integer>(child, depth + 1));
        }
    }
}
