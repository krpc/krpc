package krpc.client;

import krpc.client.Stream;
import krpc.schema.KRPC;
import krpc.schema.KRPC.Error;
import krpc.schema.KRPC.ProcedureCall;
import krpc.schema.KRPC.Type;
import krpc.schema.KRPC.Type.TypeCode;

import java.io.IOException;
import java.lang.Runnable;

public class Event implements Comparable<Event> {
  private Connection connection;
  private Stream<Boolean> stream;

  Event(Connection connection, KRPC.Event event) {
    Type returnType = Type.newBuilder().setCode(TypeCode.BOOL).build();
    stream = new Stream<Boolean>(connection, returnType, event.getStream().getId());
  }

  @Override
    public int hashCode() {
    return stream.hashCode();
  }

  @Override
  @SuppressWarnings("unchecked")
  public boolean equals(final Object obj) {
    if (this == obj) {
      return true;
    }
    if (obj == null) {
      return false;
    }
    if (getClass() != obj.getClass()) {
      return false;
    }
    final Event other = (Event) obj;
    return this.stream.equals(other.stream);
  }

  @Override
    public int compareTo(final Event obj) {
    return this.stream.compareTo(obj.stream);
  }

  /**
   * Start the event.
   */
  public void start() throws RPCException {
    stream.start();
  }

  /**
   * Condition variable that is notified when the event occurs.
   */
  public Object getCondition() {
    return stream.getCondition();
  }

  /**
   * Wait until the event occurs.
   */
  public void waitFor() throws RPCException, StreamException {
    if (!stream.stream.getStarted()) {
      start();
    }
    stream.stream.setValue(false);
    while (!stream.get()) {
      stream.waitForUpdate();
    }
  }

  /**
   * Wait until the event occurs, with a timeout in seconds.
   */
  public void waitForWithTimeout(double timeout) throws RPCException, StreamException {
    if (!stream.stream.getStarted()) {
      start();
    }
    stream.stream.setValue(false);
    while (!stream.get()) {
      boolean origValue = stream.get();
      stream.waitForUpdateWithTimeout(timeout);
      if (stream.get() == origValue) {
        // Value did not change, must have timed out
        return;
      }
    }
  }

  /**
   * Add a callback that is invoked whenever the stream is updated.
   */
  public void addCallback(Runnable callback) {
    stream.addCallback(
        (Boolean value) -> {
          if (value) {
            callback.run();
          }
        });
  }

  /**
   * The underlying stream for the event.
   */
  public Stream<Boolean> getStream() {
    return stream;
  }

  /**
   * Remove the event from the server.
   */
  public void remove() throws RPCException {
    stream.remove();
  }
}
