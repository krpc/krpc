package krpc.client;

import krpc.client.StreamException;
import krpc.client.services.KRPC;
import krpc.schema.KRPC.Type;

import java.util.ArrayList;
import java.util.List;
import java.util.function.Consumer;

class StreamImpl {
  private Connection connection;
  private long id;
  private Type returnType;
  private Object value;
  private Object updateLock;
  private Object condition = new Object();
  private List<Consumer<Object>> callbacks = new ArrayList<Consumer<Object>>();
  private boolean started = false;
  private boolean updated = false;

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

  public boolean getUpdated() {
    return updated;
  }

  public Object getCondition() {
    return condition;
  }

  public List<Consumer<Object>> getCallbacks() {
    return callbacks;
  }

  public void addCallback(Consumer<Object> callback) {
    callbacks.add(callback);
  }

  public void remove() throws RPCException {
    connection.streamManager.removeStream(id);
    setValue(new StreamException("Stream does not exist"));
  }
}
