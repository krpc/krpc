package krpc.client;

import krpc.schema.KRPC.Error;
import krpc.schema.KRPC.ProcedureCall;
import krpc.schema.KRPC.Type;

import java.io.IOException;
import java.util.function.Consumer;

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
   * Start the stream.
   */
  public void start() throws RPCException {
    if (stream.getStarted()) {
      return;
    }
    stream.start();
  }

  /**
   * Start the stream and wait for the first update.
   */
  public void startAndWait() throws RPCException, StreamException {
    if (stream.getStarted()) {
      return;
    }
    Object condition = stream.getCondition();
    synchronized (condition) {
      stream.start();
      try {
        condition.wait();
      } catch (InterruptedException exn) {
        throw new StreamException("wait was interrupted", exn);
      }
    }
  }

  /**
   * Get the most recent value for the stream.
   */
  @SuppressWarnings("unchecked")
  public T get() throws RPCException, StreamException {
    if (!stream.getStarted()) {
      startAndWait();
    }
    Object value = stream.getValue();
    if (value instanceof Error) {
      connection.throwException((Error)value);
    } else if (value instanceof RPCException) {
      throw (RPCException) value;
    } else if (value instanceof StreamException) {
      throw (StreamException) value;
    }
    return (T) value;
  }

  /**
   * Condition variable that is notified when the streams value changes.
   */
  public Object getCondition() {
    return stream.getCondition();
  }

  /**
   * Wait until the next stream update.
   */
  public void waitForUpdate() throws RPCException, StreamException {
    if (!stream.getStarted()) {
      stream.start();
    }
    try {
      stream.getCondition().wait();
    } catch (InterruptedException exn) {
      throw new StreamException("wait was interrupted", exn);
    }
  }

  /**
   * Wait until the next stream update, with a timeout in seconds.
   */
  public void waitForUpdateWithTimeout(double timeout) throws RPCException, StreamException {
    if (!stream.getStarted()) {
      stream.start();
    }
    if (timeout == 0) {
      return;
    }
    try {
      stream.getCondition().wait((long)(timeout * 1000));
    } catch (InterruptedException exn) {
      throw new StreamException("wait was interrupted", exn);
    }
  }

  /**
   * Add a callback that is invoked whenever the stream is updated.
   */
  @SuppressWarnings("unchecked")
  public void addCallback(Consumer<T> callback) {
    stream.addCallback((Object value) -> callback.accept((T)value));
  }

  /**
   * Remove the stream from the server.
   */
  public void remove() throws RPCException {
    stream.remove();
  }
}
