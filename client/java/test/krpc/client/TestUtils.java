package krpc.client;

import com.google.protobuf.ByteString;
import java.io.IOException;
import java.io.UncheckedIOException;
import java.net.InetAddress;
import java.net.ServerSocket;
import java.time.Duration;

class TestUtils {

  /**
   * How long a connection to a port nothing is listening on is waited for. It is normally
   * refused at once; where the system drops the attempt instead, this bounds the wait rather
   * than leaving the test to hang.
   */
  private static final Duration CONNECT_TIMEOUT = Duration.ofSeconds(10);

  /** One of the endpoints a connection can be made to. */
  public enum Endpoint {
    RPC,
    /** An endpoint with no server listening on it. */
    NONE
  }

  public static int getTcpipPort() {
    String envPort = System.getenv("KRPC_TEST_TCPIP_PORT");
    return envPort == null ? 50000 : Integer.parseInt(envPort);
  }

  public static Connection connect() throws IOException {
    return connect("JavaClientTest", Endpoint.RPC);
  }

  /**
   * Connect over whichever transport the harness started the server with, which it tells us
   * about by port or by socket path, or to somewhere nothing is listening.
   */
  public static Connection connect(String name, Endpoint endpoint) throws IOException {
    if (System.getenv("KRPC_TEST_LOCALSOCKET_PATH") == null) {
      Duration timeout = endpoint == Endpoint.NONE ? CONNECT_TIMEOUT : Duration.ZERO;
      return Connection.newInstance(name, "localhost", getPort(endpoint), timeout);
    }
    return Connection.newLocalInstance(name, getPath(endpoint));
  }

  private static int getPort(Endpoint endpoint) {
    return endpoint == Endpoint.RPC ? getTcpipPort() : unusedPort();
  }

  /**
   * A port nothing is listening on. Binding a port and giving it straight back leaves one that a
   * connection is refused on, and leaves it in the range the system hands out. A port derived from
   * the server's own can land anywhere, including on a low one, and a connection to those is
   * dropped rather than refused on Windows, which leaves the client waiting.
   */
  private static int unusedPort() {
    try (ServerSocket socket = new ServerSocket(0, 1, InetAddress.getLoopbackAddress())) {
      return socket.getLocalPort();
    } catch (IOException exn) {
      throw new UncheckedIOException(exn);
    }
  }

  private static String getPath(Endpoint endpoint) {
    String path = System.getenv("KRPC_TEST_LOCALSOCKET_PATH");
    // A path that no socket was created at
    return endpoint == Endpoint.RPC ? path : path + "-none";
  }

  /** The message for an event over the given stream id. */
  static krpc.schema.KRPC.Event eventMessage(long id) {
    return krpc.schema.KRPC.Event.newBuilder()
        .setStream(krpc.schema.KRPC.Stream.newBuilder().setId(id))
        .build();
  }

  public static String hexlify(byte[] data) {
    StringBuilder builder = new StringBuilder();
    for (byte b : data) {
      builder.append(String.format("%02x", b));
    }
    return builder.toString();
  }

  public static String hexlify(ByteString data) {
    return hexlify(data.toByteArray());
  }

  public static ByteString unhexlify(String data) {
    int length = data.length();
    byte[] result = new byte[length / 2];
    for (int i = 0; i < length; i += 2) {
      result[i / 2] = (byte) ((Character.digit(data.charAt(i), 16) << 4)
                              + Character.digit(data.charAt(i + 1), 16));
    }
    return ByteString.copyFrom(result);
  }

  public static String repeatedString(String string, int numRepeats) {
    return new String(new char[numRepeats]).replace("\0", string);
  }

}
