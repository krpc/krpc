package krpc.client;

import java.io.IOException;
import java.net.UnixDomainSocketAddress;
import java.nio.channels.SocketChannel;
import java.nio.charset.Charset;

/** A transport to a server on the same machine, over a unix domain socket. */
final class LocalTransport extends Transport {
  // The size of the path field of a socket address, with its terminating NUL, which is smaller on
  // macOS and the BSDs
  private static final int SOCKET_PATH_SIZE = isBsd() ? 104 : 108;

  /**
   * Connect to the socket at the given path. Throws IllegalArgumentException for a path too long
   * for a socket address.
   */
  LocalTransport(String path) throws IOException {
    super(SocketChannel.open(address(path)));
  }

  private static UnixDomainSocketAddress address(String path) {
    if (path.getBytes(Charset.defaultCharset()).length >= SOCKET_PATH_SIZE) {
      throw new IllegalArgumentException(
          "The socket path is longer than the " + (SOCKET_PATH_SIZE - 1)
          + " bytes a socket path may be");
    }
    return UnixDomainSocketAddress.of(path);
  }

  private static boolean isBsd() {
    String name = System.getProperty("os.name", "");
    return name.startsWith("Mac") || name.endsWith("BSD");
  }
}
