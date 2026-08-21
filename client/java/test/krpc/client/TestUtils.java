package krpc.client;

import com.google.protobuf.ByteString;
import java.io.IOException;

class TestUtils {

  /** One of the endpoints a connection can be made to. */
  public enum Endpoint {
    RPC,
    STREAM,
    /** An endpoint with no server listening on it. */
    NONE
  }

  public static int getRpcPort() {
    String envRpcPort = System.getenv("RPC_PORT");
    return envRpcPort == null ? 50000 : Integer.parseInt(envRpcPort);
  }

  public static int getStreamPort() {
    String envStreamPort = System.getenv("STREAM_PORT");
    return envStreamPort == null ? 50001 : Integer.parseInt(envStreamPort);
  }

  public static Connection connect() throws IOException {
    return connect("JavaClientTest", Endpoint.RPC, Endpoint.STREAM);
  }

  /**
   * Connect over whichever transport the harness started the server with, which it tells us
   * about by port or by socket path. The rpc and stream arguments name which of the server's
   * endpoints each connection should go to, so a test can deliberately connect them the wrong
   * way round, or to somewhere nothing is listening.
   */
  public static Connection connect(String name, Endpoint rpc, Endpoint stream)
      throws IOException {
    if (System.getenv("RPC_PATH") == null) {
      return Connection.newInstance(name, "localhost", getPort(rpc), getPort(stream));
    }
    return Connection.newLocalInstance(name, getPath(rpc), getPath(stream));
  }

  private static int getPort(Endpoint endpoint) {
    switch (endpoint) {
      case RPC:
        return getRpcPort();
      case STREAM:
        return getStreamPort();
      default:
        // A port number that is neither of the server's, which nothing is listening on
        return getRpcPort() ^ getStreamPort();
    }
  }

  private static String getPath(Endpoint endpoint) {
    String rpcPath = System.getenv("RPC_PATH");
    switch (endpoint) {
      case RPC:
        return rpcPath;
      case STREAM:
        return System.getenv("STREAM_PATH");
      default:
        // A path that no socket was created at
        return rpcPath + "-none";
    }
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
