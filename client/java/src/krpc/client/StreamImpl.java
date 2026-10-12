package krpc.client;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.function.Consumer;
import krpc.client.services.KRPC;
import krpc.schema.KRPC.Type;

/**
 * The state shared by every handle of one stream. It is changed under the stream's condition,
 * and then the update lock.
 */
class StreamImpl {
  private Connection connection;
  private long id;
  private Type returnType;
  private volatile Object value;
  private Object updateLock;
  private Object condition = new Object();
  private Map<Integer, Consumer<Object>> callbacks = new HashMap<Integer, Consumer<Object>>();
  private int nextCallbackTag = 0;
  private Map<Integer, Consumer<Exception>> endCallbacks =
      new HashMap<Integer, Consumer<Exception>>();
  private int nextEndCallbackTag = 0;
  private boolean started = false;
  private volatile boolean updated = false;
  private long version = 0;
  private long fired = 0;
  private volatile Exception ending = null;
  private float rate = 0;

  StreamImpl(Connection connection, long id, Type returnType, Object updateLock) {
    this.connection = connection;
    this.id = id;
    this.returnType = returnType;
    this.updateLock = updateLock;
  }

  public long getId() {
    return id;
  }

  public Type getReturnType() {
    return returnType;
  }

  public void start() throws RPCException {
    if (!started) {
      KRPC.newInstance(connection).startStream(id);
      started = true;
    }
  }

  public boolean getStarted() {
    return started;
  }

  public float getRate() {
    return rate;
  }

  public void setRate(float rate) throws RPCException {
    // The server has no stream for the id once it ends
    if (ending == null) {
      KRPC.newInstance(connection).setStreamRate(id, rate);
    }
    this.rate = rate;
  }

  public Object getValue() throws RPCException, StreamException {
    throwIfEnded();
    if (!updated) {
      throw new StreamException("Stream has no value");
    }
    return value;
  }

  /** Stores a value. The caller holds the condition and then the update lock. */
  public void setValue(Object value) {
    // Set the value before the flag that says there is one. A reader checks the flag first and
    // takes no lock.
    this.value = value;
    updated = true;
    version++;
    if (Boolean.TRUE.equals(value)) {
      fired++;
    }
  }

  public boolean getUpdated() {
    return updated;
  }

  /** The number of values received. Read with the condition held. */
  public long getVersion() {
    return version;
  }

  /** The number of true values received, which for an event is its firings. */
  public long getFired() {
    return fired;
  }

  /** The exception that ended the stream, or null while it is live. */
  public Exception getEnding() {
    return ending;
  }

  /** Throws the exception that ended the stream, if it has ended. */
  public void throwIfEnded() throws RPCException, StreamException {
    Exception exn = ending;
    if (exn == null) {
      return;
    }
    if (exn instanceof RPCException) {
      throw (RPCException) exn;
    }
    if (exn instanceof StreamException) {
      throw (StreamException) exn;
    }
    if (exn instanceof RuntimeException) {
      throw (RuntimeException) exn;
    }
    throw new StreamException(exn.getMessage(), exn);
  }

  /**
   * Ends the stream and wakes its waiters. Returns the end callbacks to call, with no lock held.
   * Returns none if the stream had already ended.
   */
  List<Consumer<Exception>> end(Exception ending) {
    List<Consumer<Exception>> result;
    synchronized (condition) {
      synchronized (updateLock) {
        if (this.ending != null) {
          return new ArrayList<Consumer<Exception>>();
        }
        this.ending = ending;
        result = new ArrayList<Consumer<Exception>>(endCallbacks.values());
        endCallbacks.clear();
      }
      condition.notifyAll();
    }
    return result;
  }

  public Object getCondition() {
    return condition;
  }

  public Map<Integer, Consumer<Object>> getCallbacks() {
    return callbacks;
  }

  public int addCallback(Consumer<Object> callback) {
    synchronized (updateLock) {
      int tag = nextCallbackTag;
      nextCallbackTag++;
      callbacks.put(tag, callback);
      return tag;
    }
  }

  public void removeCallback(int tag) {
    synchronized (updateLock) {
      callbacks.remove(tag);
    }
  }

  public int addEndCallback(Consumer<Exception> callback) {
    int tag;
    Exception exn;
    synchronized (updateLock) {
      tag = nextEndCallbackTag;
      nextEndCallbackTag++;
      exn = ending;
      if (exn == null) {
        endCallbacks.put(tag, callback);
      }
    }
    // A stream that has already ended calls the callback here, as no ending is to come
    if (exn != null) {
      StreamManager.invokeCallback(() -> callback.accept(exn));
    }
    return tag;
  }

  public void removeEndCallback(int tag) {
    synchronized (updateLock) {
      endCallbacks.remove(tag);
    }
  }

  public void remove() throws RPCException {
    connection.streamManager.removeStream(this);
  }
}
