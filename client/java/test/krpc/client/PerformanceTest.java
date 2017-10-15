package krpc.client;

import krpc.client.services.TestService;

import org.junit.Before;
import org.junit.Test;

import java.io.IOException;

public class PerformanceTest {

  private Connection connection;
  private TestService testService;

  @Before
  public void setup() throws IOException {
    connection = TestUtils.connect();
    testService = TestService.newInstance(connection);
  }

  @Test
  public void testPerformance() throws RPCException {
    int numRepeats = 100;
    long startTime = System.nanoTime();
    for (int i = 0; i < numRepeats; i++) {
      testService.floatToString(3.14159f);
    }
    long endTime = System.nanoTime();
    double time = (endTime - startTime) / 1000000000.0;
    System.out.println();
    System.out.println(String.format("Total execution time: %.3f seconds", time));
    System.out.println(String.format("RPC execution rate: %d per second",
                                     (int) (numRepeats / time)));
    System.out.println(String.format("Latency: %.3f milliseconds", (time * 1000) / numRepeats));
  }
}
