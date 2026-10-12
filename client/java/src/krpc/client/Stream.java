package krpc.client;

import java.util.function.Consumer;
import krpc.schema.KRPC.ProcedureCall;
import krpc.schema.KRPC.Type;

/** A stream that receives live updates from the server. */
public class Stream<T> implements Comparable<Stream<T>> {
  private Connection connection;
  StreamImpl stream;

  Stream(Connection connection, Type returnType, long id) {
    this.connection = connection;
    this.stream = connection.streamManager.getStream(returnType, id);
  }

  Stream(Connection connection, Type returnType, ProcedureCall call) throws RPCException {
    this.connection = connection;
    this.stream = connection.streamManager.addStream(returnType, call);
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
    final Stream<T> other = (Stream<T>) obj;
    return this.stream.getId() == other.stream.getId();
  }

  @Override
    public int compareTo(final Stream<T> obj) {
    return Long.valueOf(this.stream.getId()).compareTo(Long.valueOf(obj.stream.getId()));
  }

  /**
   * Start the stream. Throws the exception that ended the stream, if it has ended.
   */
  public void start() throws RPCException, StreamException {
    stream.throwIfEnded();
    if (stream.getStarted()) {
      return;
    }
    stream.start();
  }

  /**
   * Start the stream and wait for the first update. Throws the exception that ended the stream,
   * if it ends before or during the wait.
   */
  public void startAndWait() throws RPCException, StreamException {
    stream.throwIfEnded();
    if (stream.getStarted()) {
      return;
    }
    Object condition = stream.getCondition();
    synchronized (condition) {
      stream.start();
      while (!stream.getUpdated() && stream.getEnding() == null) {
        waitOn(condition, 0);
      }
      stream.throwIfEnded();
    }
  }

  /**
   * The rate of the stream, in Hertz. Zero if the rate is unlimted.
   */
  public float getRate() throws RPCException {
    return stream.getRate();
  }

  /**
   * Set the rate of the stream, in Hertz. Zero if the rate is unlimted.
   */
  public void setRate(float rate) throws RPCException {
    stream.setRate(rate);
  }

  /**
   * Get the most recent value for the stream. Throws the exception that ended the stream, if it
   * has ended.
   */
  @SuppressWarnings("unchecked")
  public T get() throws RPCException, StreamException {
    stream.throwIfEnded();
    if (!stream.getStarted()) {
      startAndWait();
    }
    return (T) stream.getValue();
  }

  /**
   * Condition variable that is notified when the streams value changes.
   */
  public Object getCondition() {
    return stream.getCondition();
  }

  /**
   * Wait until the next stream update. Throws the exception that ended the stream, if it ends
   * before or during the wait.
   */
  public void waitForUpdate() throws RPCException, StreamException {
    waitForUpdateUntil(deadline(-1));
  }

  /**
   * Wait until the next stream update, with a timeout in seconds. Returns true if the stream
   * updated, and false on a timeout. Throws the exception that ended the stream, if it ends
   * before or during the wait.
   */
  public boolean waitForUpdateWithTimeout(double timeout) throws RPCException, StreamException {
    return waitForUpdateUntil(deadline(timeout));
  }

  /** A deadline of zero waits indefinitely. */
  private boolean waitForUpdateUntil(long deadline) throws RPCException, StreamException {
    stream.throwIfEnded();
    if (!stream.getStarted()) {
      stream.start();
    }
    long version = stream.getVersion();
    while (stream.getVersion() == version) {
      stream.throwIfEnded();
      if (!waitOn(stream.getCondition(), deadline)) {
        return false;
      }
    }
    return true;
  }

  /**
   * The deadline, in System.nanoTime() nanoseconds, for a timeout in seconds. Zero for a
   * negative timeout, which waits indefinitely.
   */
  static long deadline(double timeout) {
    if (timeout < 0) {
      return 0;
    }
    // Zero is reserved for no deadline
    long deadline = System.nanoTime() + (long) (timeout * 1e9);
    return deadline == 0 ? 1 : deadline;
  }

  /**
   * Waits on a condition the caller holds, until notified or the deadline passes. Returns false
   * if the deadline had passed on entry. A deadline of zero waits indefinitely.
   */
  static boolean waitOn(Object condition, long deadline) throws StreamException {
    try {
      if (deadline == 0) {
        condition.wait();
        return true;
      }
      long remaining = deadline - System.nanoTime();
      if (remaining <= 0) {
        return false;
      }
      condition.wait(remaining / 1000000, (int) (remaining % 1000000));
      return true;
    } catch (InterruptedException exn) {
      throw new StreamException("wait was interrupted", exn);
    }
  }

  /**
   * Add a callback that is invoked whenever the stream is updated with a value. An error ends
   * the stream and skips the callbacks.
   * Returns an integer tag for the callback which uniquely identifies it,
   * and allows it to be removed using removeCallback()
   */
  @SuppressWarnings("unchecked")
  public int addCallback(Consumer<T> callback) {
    return stream.addCallback((Object value) -> callback.accept((T) value));
  }

  /**
   * Remove a callback based on its tag.
   */
  public void removeCallback(int tag) {
    stream.removeCallback(tag);
  }

  /**
   * Add a callback that is invoked once when the stream ends, with the exception that ended it.
   * It is invoked straight away if the stream has already ended. Returns an integer tag for the
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
   * Remove the stream from the server. Streams for the same call share one stream on the server,
   * so removing one removes them all.
   */
  public void remove() throws RPCException {
    stream.remove();
  }
}
