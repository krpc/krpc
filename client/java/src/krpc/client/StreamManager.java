package krpc.client;

import com.google.protobuf.ByteString;
import com.google.protobuf.CodedInputStream;
import java.io.IOException;
import java.net.Socket;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.function.Consumer;
import krpc.client.services.KRPC;
import krpc.schema.KRPC.Error;
import krpc.schema.KRPC.ProcedureCall;
import krpc.schema.KRPC.ProcedureResult;
import krpc.schema.KRPC.Response;
import krpc.schema.KRPC.StreamResult;
import krpc.schema.KRPC.StreamUpdate;
import krpc.schema.KRPC.Type;

class StreamManager {
  private Connection connection;
  private Socket socket;
  private KRPC krpc;
  private Object updateLock;
  private Map<Long, StreamImpl> streams = new HashMap<Long, StreamImpl>();
  private Thread updateThread;

  StreamManager(Connection connection, Socket socket) {
    this.connection = connection;
    this.socket = socket;
    krpc = KRPC.newInstance(connection);
    updateLock = new Object();
    updateThread = new Thread(new UpdateThread(this));
    updateThread.start();
  }

  void close() throws IOException {
    socket.close();
  }

  StreamImpl addStream(Type returnType, ProcedureCall call) throws RPCException {
    long id = krpc.addStream(call, false).getId();
    synchronized (updateLock) {
      if (!streams.containsKey(id)) {
        streams.put(id, new StreamImpl(connection, id, returnType, updateLock));
      }
      return streams.get(id);
    }
  }

  StreamImpl getStream(Type returnType, long id) {
    synchronized (updateLock) {
      if (!streams.containsKey(id)) {
        streams.put(id, new StreamImpl(connection, id, returnType, updateLock));
      }
      return streams.get(id);
    }
  }

  void removeStream(long id) throws RPCException {
    synchronized (updateLock) {
      if (streams.containsKey(id)) {
        krpc.removeStream(id);
        streams.remove(id);
      }
    }
  }

  void update(long id, ProcedureResult result) throws StreamException {
    StreamImpl stream;
    Object value;
    List<Consumer<Object>> callbacks;
    // The update lock is held only to find the stream and decode its new value, and is released
    // before the stream's condition is taken. A thread waiting for an update holds the condition
    // and then needs the update lock -- Event.waitFor resets the stream value while holding it,
    // as its documented use requires -- so taking the two in the opposite order here deadlocks.
    // The callbacks are copied for the same reason: they run below without the lock held.
    synchronized (updateLock) {
      if (!streams.containsKey(id)) {
        return;
      }
      stream = streams.get(id);
      if (!result.hasError()) {
        value = Encoder.decode(result.getValue(), stream.getReturnType(), connection);
      } else {
        value = result.getError();
      }
      callbacks = new ArrayList<Consumer<Object>>(stream.getCallbacks().values());
    }
    Object condition = stream.getCondition();
    synchronized (condition) {
      stream.setValue(value);
      condition.notifyAll();
    }
    for (Consumer<Object> callback : callbacks) {
      try {
        callback.accept(value);
      } catch (RuntimeException exn) {
        // A callback that throws must not stop the remaining callbacks running, nor escape
        // and end the update thread, which would silently stop every stream on the
        // connection. There is no caller to propagate it to, so hand it to the thread's
        // uncaught exception handler, which by default reports it on stderr.
        Thread thread = Thread.currentThread();
        thread.getUncaughtExceptionHandler().uncaughtException(thread, exn);
      }
    }
  }

  private static class UpdateThread implements Runnable {
    StreamManager manager;

    public UpdateThread(StreamManager manager) {
      this.manager = manager;
    }

    @Override
    public void run() {
      try {
        CodedInputStream inputStream =
            CodedInputStream.newInstance(manager.socket.getInputStream());
        while (true) {
          int size = inputStream.readRawVarint32();
          byte[] data = inputStream.readRawBytes(size);
          StreamUpdate update = StreamUpdate.parseFrom(data);
          for (StreamResult result : update.getResultsList()) {
            manager.update(result.getId(), result.getResult());
          }
        }
        // A closed connection (e.g. on disconnect) surfaces as one of these; end the update thread.
      } catch (StreamException exn) {
        return;
      } catch (IOException exn) {
        return;
      }
    }
  }
}
