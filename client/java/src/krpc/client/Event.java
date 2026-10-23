package krpc.client;

import java.lang.Runnable;
import java.util.function.Consumer;
import krpc.client.Stream;
import krpc.schema.KRPC;
import krpc.schema.KRPC.Type;
import krpc.schema.KRPC.Type.TypeCode;

/** Represents a server-side event that can be waited on. */
public class Event implements Comparable<Event> {
  private Stream<Boolean> stream;
  // The number of firings when a wait on this handle last returned
  private volatile long seen = 0;

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
   * Condition variable that is notified when the event occurs, and when the event ends.
   */
  public Object getCondition() {
    return stream.getCondition();
  }

  /**
   * Wait until the event occurs. Returns straight away if the event has occurred since the last
   * wait on this event object. Throws the exception that ended the event, if it ends before or
   * during the wait.
   */
  public void waitFor() throws RPCException, StreamException {
    waitForUntil(Stream.deadline(Double.POSITIVE_INFINITY));
  }

  /**
   * Wait until the event occurs, with a timeout in seconds. Returns straight away if the event
   * has occurred since the last wait on this event object. Returns true if the event occurred,
   * and false on a timeout. Throws the exception that ended the event, if it ends before or
   * during the wait. Throws IllegalArgumentException if the timeout is negative or NaN.
   */
  public boolean waitForWithTimeout(double timeout) throws RPCException, StreamException {
    return waitForUntil(Stream.deadline(timeout));
  }

  /** A deadline of zero waits indefinitely. */
  private boolean waitForUntil(long deadline) throws RPCException, StreamException {
    StreamImpl impl = stream.stream;
    // A remove or a close discards an unseen firing
    if (impl.isEndedByClient()) {
      impl.throwIfEnded();
    }
    long seenOnEntry = seen;
    while (impl.getFired() <= seenOnEntry) {
      impl.throwIfEnded();
      if (!Stream.waitOn(impl.getCondition(), deadline)) {
        return false;
      }
    }
    seen = impl.getFired();
    return true;
  }

  /**
   * Add a callback that is invoked whenever the event occurs. It is invoked straight away if the
   * event has occurred since the last wait on this event object, and no callback has been invoked
   * for it. A callback added after the event is removed or its connection closes is not invoked
   * for an earlier firing. An
   * error ends the event and does not invoke the callback.
   * Returns an integer tag for the callback which uniquely identifies it,
   * and allows it to be removed using removeCallback()
   */
  public int addCallback(Runnable callback) {
    long seenNow;
    synchronized (stream.getCondition()) {
      seenNow = seen;
    }
    return stream.stream.addEventCallback(callback, seenNow);
  }

  /**
   * Remove a callback, based on its tag.
   */
  public void removeCallback(int tag) {
    stream.removeCallback(tag);
  }

  /**
   * Add a callback that is invoked once when the event ends, with the exception that ended it.
   * It is invoked straight away if the event has already ended. Returns an integer tag for the
   * callback, which allows it to be removed using removeEndCallback()
   */
  public int addEndCallback(Consumer<Exception> callback) {
    return stream.addEndCallback(callback);
  }

  /**
   * Remove an end callback based on its tag.
   */
  public void removeEndCallback(int tag) {
    stream.removeEndCallback(tag);
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
