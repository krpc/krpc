package krpc.client;

import java.io.IOException;
import java.net.InetSocketAddress;
import java.nio.channels.ServerSocketChannel;

/**
 * The connection carried over TCP/IP, against a server the test listens on itself rather
 * than a kRPC server.
 */
public class TcpIpConnectionTest extends ConnectionTestCase {

  @Override
  protected ServerSocketChannel listen() throws IOException {
    // Port zero, so the system picks one nothing else is using
    return ServerSocketChannel.open().bind(new InetSocketAddress("localhost", 0));
  }

  @Override
  protected Connection connect(String name) throws IOException {
    int port = ((InetSocketAddress) getServer().getAddress()).getPort();
    return Connection.newInstance(name, "localhost", port, port);
  }
}
