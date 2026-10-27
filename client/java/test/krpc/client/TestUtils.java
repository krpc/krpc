package krpc.client;

import java.io.IOException;

class TestUtils {

  /** Connect to the server the harness started, on the port it passes. */
  static Connection connect() throws IOException {
    String port = System.getenv("KRPC_TEST_TCPIP_PORT");
    return Connection.newInstance(
        "JavaClientTest", "localhost", port == null ? 50000 : Integer.parseInt(port));
  }
}
