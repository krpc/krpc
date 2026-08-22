package krpc.client;

import static org.junit.Assert.assertThrows;

import java.io.IOException;
import java.net.StandardProtocolFamily;
import java.net.UnixDomainSocketAddress;
import java.nio.channels.ServerSocketChannel;
import java.nio.file.Files;
import java.nio.file.Path;
import org.junit.After;
import org.junit.Test;

/**
 * The connection carried over a unix domain socket, against a server the test listens on
 * itself rather than a kRPC server. The tests are those of the TCP/IP connection: what
 * differs is only how the socket is opened.
 */
public class LocalSocketConnectionTest extends ConnectionTestCase {

  private Path directory;
  private Path path;

  /**
   * A directory to put a socket in, short enough for the path of one to fit in a socket address.
   * The directory a test is given for its temporary files is nested far deeper than an address
   * has room for, so the platform's own is used directly. On Windows that is the one TEMP names,
   * rather than java.io.tmpdir, which the test runner points at the deep directory of its own.
   */
  private static Path socketDirectory() {
    if (!System.getProperty("os.name", "").startsWith("Windows")) {
      return Path.of("/tmp");
    }
    for (String name : new String[] {"TEMP", "TMP"}) {
      String directory = System.getenv(name);
      if (directory != null && !directory.isEmpty()) {
        return Path.of(directory);
      }
    }
    return Path.of(System.getProperty("java.io.tmpdir"));
  }

  @Override
  protected ServerSocketChannel listen() throws IOException {
    // A socket path has to fit in the kernel's address structure, which leaves far less room
    // than the directory a test is run in takes up, so it goes in a short directory of its own
    directory = Files.createTempDirectory(socketDirectory(), "krpc-java-test");
    path = directory.resolve("rpc");
    return ServerSocketChannel.open(StandardProtocolFamily.UNIX)
        .bind(UnixDomainSocketAddress.of(path));
  }

  @Override
  protected Connection connect(String name) throws IOException {
    return Connection.newLocalInstance(name, path.toString(), path.toString());
  }

  @After
  @SuppressWarnings("checkstyle:missingjavadocmethod")
  public void removeSocket() throws IOException {
    Files.deleteIfExists(path);
    Files.deleteIfExists(directory);
  }

  @Test
  @SuppressWarnings("checkstyle:missingjavadocmethod")
  public void testConnectWhereNothingIsListening() {
    String missing = path.toString() + "-does-not-exist";
    assertThrows(IOException.class,
        () -> Connection.newLocalInstance("JavaConnectionTestNoServer", missing, missing));
  }
}
