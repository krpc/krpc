package krpc.client;

import java.io.IOException;
import java.net.UnixDomainSocketAddress;
import java.nio.channels.SocketChannel;

/** A transport to a server on the same machine, over a unix domain socket. */
final class LocalTransport extends Transport {
  /** Connect to the socket at the given path. */
  LocalTransport(String path) throws IOException {
    super(SocketChannel.open(UnixDomainSocketAddress.of(path)));
  }
}
