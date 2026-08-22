package krpc.client;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNotNull;
import static org.junit.Assert.assertThrows;

import java.io.IOException;
import java.nio.channels.ServerSocketChannel;
import krpc.schema.KRPC;
import org.junit.After;
import org.junit.Before;
import org.junit.Test;

/**
 * What a connection to a server does regardless of what carries it: making the handshake,
 * carrying a call, and reporting a server that turns the connection down. Only opening the
 * socket differs between the transports, so each of them supplies that and shares these.
 */
public abstract class ConnectionTestCase {

  private StandInServer server;

  /**
   * A channel listening on the transport under test, bound to somewhere nothing else is using.
   *
   * @return The listening channel.
   */
  protected abstract ServerSocketChannel listen() throws IOException;

  /**
   * Connect to the running server over the transport under test.
   *
   * @param name The name of the client.
   *
   * @return A connection to the server.
   */
  protected abstract Connection connect(String name) throws IOException;

  /**
   * The running server, for a test that needs to know where it is listening.
   *
   * @return The server.
   */
  protected StandInServer getServer() {
    return server;
  }

  @Before
  @SuppressWarnings("checkstyle:missingjavadocmethod")
  public void setup() throws IOException {
    server = new StandInServer(listen());
  }

  @After
  @SuppressWarnings("checkstyle:missingjavadocmethod")
  public void teardown() throws IOException {
    if (server != null) {
      server.close();
    }
  }

  @Test
  @SuppressWarnings("checkstyle:missingjavadocmethod")
  public void testConnectAndDisconnect() throws IOException {
    try (Connection connection = connect("JavaConnectionTest")) {
      assertNotNull(connection);
    }
  }

  @Test
  @SuppressWarnings("checkstyle:missingjavadocmethod")
  public void testCarriesOneCall() throws IOException, RPCException {
    // The stand-in answers every request, so this is the request reaching it and the response
    // coming back rather than anything the call means
    try (Connection connection = connect("JavaConnectionTest")) {
      assertNotNull(connection.invoke("TestService", "TestProcedure"));
    }
  }

  @Test
  @SuppressWarnings("checkstyle:missingjavadocmethod")
  public void testCarriesManyCalls() throws IOException, RPCException {
    // Calls are read out of a buffer the transport fills, so a run of them covers that buffer
    // being refilled and reused rather than only its first use
    try (Connection connection = connect("JavaConnectionTest")) {
      for (int i = 0; i < 100; i++) {
        assertNotNull(connection.invoke("TestService", "TestProcedure"));
      }
    }
  }

  @Test
  @SuppressWarnings("checkstyle:missingjavadocmethod")
  public void testConnectionRefusedByTheServer() throws IOException {
    server.refuseWith(KRPC.ConnectionResponse.Status.WRONG_TYPE,
        "Connection request was for the wrong server");
    ConnectionException exn =
        assertThrows(ConnectionException.class, () -> connect("JavaConnectionTestRefused"));
    assertEquals("Connection request was for the wrong server", exn.getMessage());
  }
}
