package krpc.client;

import java.util.HashMap;
import java.util.Map;
import java.util.function.Consumer;
import krpc.client.StreamException;
import krpc.client.services.KRPC;
import krpc.schema.KRPC.Type;

class StreamImpl {
  private Connection connection;
  private long id;
  private Type returnType;
  private Object value;
  private Object updateLock;
  private Object condition = new Object();
  private Map<Integer, Consumer<Object>> callbacks = new HashMap<Integer, Consumer<Object>>();
  private int nextCallbackTag = 0;
  private boolean started = false;
  private boolean updated = false;
  private volatile boolean removed = false;
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

  public boolean getRemoved() {
    return removed;
  }

  public float getRate() {
    return rate;
  }

  public void setRate(float rate) throws RPCException {
    KRPC.newInstance(connection).setStreamRate(id, rate);
    this.rate = rate;
  }

  public Object getValue() throws StreamException {
    if (!updated) {
      throw new StreamException("Stream has no value");
    }
    return value;
  }

  public void setValue(Object value) {
    synchronized (updateLock) {
      this.value = value;
      updated = true;
    }
  }

  /**
   * Sets the value, unless the stream has been removed. The error remove() stores is kept.
   */
  public void setValueIfNotRemoved(Object value) {
    synchronized (updateLock) {
      if (!removed) {
        this.value = value;
        updated = true;
      }
    }
  }

  public boolean getUpdated() {
    return updated;
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

  public void remove() throws RPCException {
    synchronized (updateLock) {
      // A handle sharing this stream can remove it again after a new stream reuses its id
      if (removed) {
        return;
      }
      connection.streamManager.removeStream(id);
      removed = true;
    }
    setValue(new StreamException("Stream does not exist"));
    // No update arrives for a removed stream, so a waiting thread is woken here
    synchronized (condition) {
      condition.notifyAll();
    }
  }
}
