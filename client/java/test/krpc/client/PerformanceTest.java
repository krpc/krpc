package krpc.client;

import java.io.IOException;

import org.junit.Before;
import org.junit.Test;

import krpc.client.services.TestService;

public class PerformanceTest {

    private Connection connection;
    private TestService testService;

    @Before
    public void setup() throws IOException {
        connection = TestUtils.Connect();
        testService = TestService.newInstance(connection);
    }

    @Test
    public void testPerformance() throws RPCException, IOException {
        int n = 100;
        long startTime = System.nanoTime();
        for (int i = 0; i < n; i++)
            testService.floatToString(3.14159f);
        long endTime = System.nanoTime();
        double t = (endTime - startTime) / 1000000000.0;
        System.out.println();
        System.out.println(String.format("Total execution time: %.3f seconds", t));
        System.out.println(String.format("RPC execution rate: %d per second", (int) (n / t)));
        System.out.println(String.format("Latency: %.3f milliseconds", (t * 1000) / n));
    }
}
