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

class StreamManager {
  private Connection connection;
  private Socket socket;
  private KRPC krpc;
  private Map<Long, ByteString> streamData = new HashMap<Long, ByteString>();
  private Map<Long, Object> streamValues = new HashMap<Long, Object>();
  private Map<Long, Type> streamTypes = new HashMap<Long, Type>();
  private Map<Long, Error> streamErrors = new HashMap<Long, Error>();
  private Thread updateThread;

  StreamManager(Connection connection, Socket socket) {
    this.connection = connection;
    this.socket = socket;
    krpc = KRPC.newInstance(connection);
    updateThread = new Thread(new UpdateThread(this));
    updateThread.start();
  }

  void close() throws IOException {
    socket.close();
  }

  <T> Stream<T> add(ProcedureCall call, Type type) throws RPCException {
    long id = krpc.addStream(call, true).getId();
    synchronized (streamData) {
      if (!streamTypes.containsKey(id)) {
        streamTypes.put(id, type);
        Response response = connection.invokeInternal(call);
        Error error = connection.getErrorFromResponse(response);
        if (error == null) {
          ByteString value = connection.getReturnValueFromResponse(response);
          streamData.put(id, value);
        } else {
          streamData.put(id, null);
          streamErrors.put(id, error);
        }
      }
    }
    return new Stream<T>(this, id);
  }

  void remove(long id) throws RPCException {
    krpc.removeStream(id);
    synchronized (streamData) {
      streamData.remove(id);
      streamTypes.remove(id);
      streamErrors.remove(id);
    }
  }

  Object get(long id) throws RPCException, StreamException {
    Object result;
    synchronized (streamData) {
      if (!streamTypes.containsKey(id)) {
        throw new StreamException("Stream does not exist");
      }
      if (streamErrors.containsKey(id)) {
        connection.throwException(streamErrors.get(id));
      }
      if (streamValues.containsKey(id)) {
        return streamValues.get(id);
      }
      result = Encoder.decode(streamData.get(id), streamTypes.get(id), connection);
      streamValues.put(id, result);
    }
    return result;
  }

  void update(long id, ProcedureResult result) throws StreamException {
    synchronized (streamData) {
      if (!streamData.containsKey(id)) {
        throw new StreamException("Stream does not exist");
      }
      if (result.hasError()) {
        streamErrors.put(id, result.getError());
        streamData.remove(id);
        streamValues.remove(id);
      } else {
        streamData.put(id, result.getValue());
        streamValues.remove(id);
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
