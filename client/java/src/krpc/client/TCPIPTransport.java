package krpc.client;

import java.io.IOException;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.nio.channels.SocketChannel;
import java.time.Duration;

/** A transport to a server over TCP/IP. */
@SuppressWarnings("checkstyle:abbreviationaswordinname")
final class TCPIPTransport extends Transport {
  /** Connect. A zero timeout waits for as long as the operating system does. */
  TCPIPTransport(InetAddress address, int port, Duration timeout) throws IOException {
    super(open(address, port, timeout));
  }

  private static SocketChannel open(InetAddress address, int port, Duration timeout)
      throws IOException {
    if (timeout.isZero()) {
      return SocketChannel.open(new InetSocketAddress(address, port));
    }
    // A network that drops a connection attempt instead of refusing it leaves the client
    // waiting, so bound the wait where one was asked for.
    SocketChannel channel = SocketChannel.open();
    try {
      channel.socket().connect(new InetSocketAddress(address, port), (int) timeout.toMillis());
    } catch (IOException exn) {
      channel.close();
      throw exn;
    }
    return channel;
  }
}
