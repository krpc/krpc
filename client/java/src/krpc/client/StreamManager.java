package krpc.client;

import java.io.IOException;
import java.net.Socket;
import java.util.HashMap;
import java.util.Map;

import com.google.protobuf.ByteString;
import com.google.protobuf.CodedInputStream;

import krpc.client.services.KRPC;
import krpc.schema.KRPC.Request;
import krpc.schema.KRPC.Response;
import krpc.schema.KRPC.StreamResult;
import krpc.schema.KRPC.StreamUpdate;
import krpc.schema.KRPC.Type;

class StreamManager {
    private Connection connection;
    private Socket socket;
    private KRPC krpc;
    private Map<Long, ByteString> streamData = new HashMap<Long, ByteString>();
    private Map<Long, Object> streamValues = new HashMap<Long, Object>();
    private Map<Long, Type> streamTypes = new HashMap<Long, Type>();
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

    <T> Stream<T> add(Request request, Type type) throws IOException, RPCException {
        long id = krpc.addStream(request).getId();
        synchronized (streamData) {
            if (!streamTypes.containsKey(id)) {
                streamData.put(id, connection.invoke(request));
                streamTypes.put(id, type);
            }
        }
        return new Stream<T>(this, id);
    }

    void remove(long id) throws IOException, RPCException {
        krpc.removeStream(id);
        synchronized (streamData) {
            streamData.remove(id);
            streamTypes.remove(id);
        }
    }

    Object get(long id) throws IOException, StreamException {
        Object result;
        synchronized (streamData) {
            if (!streamTypes.containsKey(id))
                throw new StreamException("Stream does not exist");
            if (streamValues.containsKey(id))
                return streamValues.get(id);
            result = Encoder.decode(streamData.get(id), streamTypes.get(id), connection);
            streamValues.put(id, result);
        }
        return result;
    }

    void update(long id, Response response) throws StreamException {
        synchronized (streamData) {
            if (!streamData.containsKey(id))
                throw new StreamException("Stream does not exist");
            if (!response.getError().isEmpty())
                return; // TODO: do something with the error
            streamData.put(id, response.getReturnValue());
            streamValues.remove(id);
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
                CodedInputStream inputStream = CodedInputStream.newInstance(manager.socket.getInputStream());
                while (true) {
                    int size = inputStream.readRawVarint32();
                    byte[] data = inputStream.readRawBytes(size);
                    StreamUpdate update = StreamUpdate.parseFrom(data);
                    for (StreamResult result : update.getResultsList())
                        manager.update(result.getId(), result.getResponse());
                }
             // TODO: handle these exceptions properly
            } catch (StreamException e) {
            } catch (IOException e) {
            }
        }
    }
}
