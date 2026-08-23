package krpc.client;

import com.google.protobuf.ByteString;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.SocketAddress;
import java.nio.channels.Channels;
import java.nio.channels.ServerSocketChannel;
import java.nio.channels.SocketChannel;
import krpc.schema.KRPC;

/**
 * A stand-in for a kRPC server, which answers the connection handshake and then sends back a
 * response for every request. These tests are about how a transport carries the messages, so
 * nothing the server would do with them has to be understood to answer them. The socket it
 * listens on is supplied by whichever test case is running.
 */
class StandInServer implements AutoCloseable {

  private final ServerSocketChannel listener;
  private final Thread thread;
  private volatile boolean stopping;
  private volatile KRPC.ConnectionResponse.Status status = KRPC.ConnectionResponse.Status.OK;
  private volatile String message = "";

  StandInServer(ServerSocketChannel listeningChannel) {
    listener = listeningChannel;
    thread = new Thread(this::run);
    thread.setDaemon(true);
    thread.start();
  }

  /** Where a client connects to reach this server. */
  public SocketAddress getAddress() throws IOException {
    return listener.getLocalAddress();
  }

  /**
   * Answer the handshake with the given status and message, so that a test can have a
   * connection refused as the server refuses one made to the wrong endpoint.
   */
  public void refuseWith(KRPC.ConnectionResponse.Status refusedStatus, String refusedMessage) {
    status = refusedStatus;
    message = refusedMessage;
  }

  @Override
  public void close() throws IOException {
    stopping = true;
    listener.close();
    try {
      thread.join(5000);
    } catch (InterruptedException exn) {
      Thread.currentThread().interrupt();
    }
  }

  private void run() {
    while (!stopping) {
      SocketChannel channel;
      try {
        channel = listener.accept();
      } catch (IOException exn) {
        // The listening channel has been closed, so the server is stopping
        return;
      }
      Thread serve = new Thread(() -> serve(channel));
      serve.setDaemon(true);
      serve.start();
    }
  }

  private void serve(SocketChannel channel) {
    // A message is a size varint followed by the message itself, which is what protobuf's
    // delimited form is, so the messages are read and written through that
    try (SocketChannel socket = channel) {
      InputStream input = Channels.newInputStream(socket);
      OutputStream output = Channels.newOutputStream(socket);
      KRPC.ConnectionRequest.parseDelimitedFrom(input);
      KRPC.ConnectionResponse.newBuilder()
          .setStatus(status)
          .setMessage(message)
          .setClientIdentifier(ByteString.copyFrom(new byte[16]))
          .build()
          .writeDelimitedTo(output);
      output.flush();
      if (status != KRPC.ConnectionResponse.Status.OK) {
        return;
      }
      // Every request is answered with an empty result, which is what a call returning
      // nothing gets back from a real server
      while (!stopping) {
        if (KRPC.Request.parseDelimitedFrom(input) == null) {
          return;
        }
        KRPC.Response.newBuilder()
            .addResults(KRPC.ProcedureResult.newBuilder().build())
            .build()
            .writeDelimitedTo(output);
        output.flush();
      }
    } catch (IOException exn) {
      // The client has gone, which is how a connection ends here
    }
  }
}
