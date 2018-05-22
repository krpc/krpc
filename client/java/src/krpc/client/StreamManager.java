package krpc.client;

import com.google.protobuf.ByteString;
import com.google.protobuf.CodedInputStream;

import krpc.client.services.KRPC;
import krpc.schema.KRPC.Error;
import krpc.schema.KRPC.ProcedureCall;
import krpc.schema.KRPC.ProcedureResult;
import krpc.schema.KRPC.Response;
import krpc.schema.KRPC.StreamResult;
import krpc.schema.KRPC.StreamUpdate;
import krpc.schema.KRPC.Type;

import java.io.IOException;
import java.net.Socket;
import java.util.HashMap;
import java.util.Map;
import java.util.function.Consumer;

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
    synchronized (updateLock) {
      if (!streams.containsKey(id)) {
        return;
      }
      StreamImpl stream = streams.get(id);
      Object value;
      if (!result.hasError()) {
        value = Encoder.decode(result.getValue(), stream.getReturnType(), connection);
      } else {
        value = result.getError();
      }
      Object condition = stream.getCondition();
      synchronized (condition) {
        stream.setValue(value);
        condition.notifyAll();
      }
      for (Consumer<Object> callback : stream.getCallbacks().values()) {
        callback.accept(value);
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
        // TODO: handle these exceptions properly
      } catch (StreamException exn) {
        return;
      } catch (IOException exn) {
        return;
      }
    }
  }
}
