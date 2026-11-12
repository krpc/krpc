import krpc.client.Connection;
import krpc.client.services.ScanSat;
import krpc.client.services.ScanSat.ScanType;
import krpc.client.services.ScanSat.Scanner;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.CelestialBody;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Vessel;

public class ScanSatExample {
    public static void main(String[] args) {
        Connection connection = Connection.newInstance("SCANsat Example");
        Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
        ScanSat scansat = ScanSat.newInstance(connection);

        for (Scanner scanner : scansat.scanners(vessel)) {
            scanner.setActive(true);
        }

        CelestialBody body = vessel.getOrbit().getBody();
        double coverage = scansat.coverage(body, ScanType.ALTIMETRY);
        System.out.printf("Altimetry coverage of %s: %.1f%%\n", body.getName(), coverage * 100);

        Flight flight = vessel.flight(null);
        Double elevation = scansat.elevation(body, flight.getLatitude(), flight.getLongitude());
        if (elevation == null) {
            System.out.println("The terrain below the vessel is not scanned");
        } else {
            System.out.printf("The terrain below the vessel is at %.0f m\n", elevation);
        }
        connection.close();
    }
}
