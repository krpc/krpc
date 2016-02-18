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
import krpc.schema.KRPC.StreamMessage;
import krpc.schema.KRPC.StreamResponse;

class StreamManager {
    private Connection connection;
    private Socket socket;
    private KRPC krpc;
    private Map<Integer, ByteString> streamData = new HashMap<Integer, ByteString>();
    private Map<Integer, Object> streamValues = new HashMap<Integer, Object>();
    private Map<Integer, TypeSpecification> streamTypeSpecs = new HashMap<Integer, TypeSpecification>();
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

    <T> Stream<T> add(Request request, TypeSpecification typeSpec) throws IOException, RPCException {
        int id = krpc.addStream(request);
        synchronized (streamData) {
            if (!streamTypeSpecs.containsKey(id)) {
                streamData.put(id, connection.invoke(request));
                streamTypeSpecs.put(id, typeSpec);
            }
        }
        return new Stream<T>(this, id);
    }

    void remove(int id) throws IOException, RPCException {
        krpc.removeStream(id);
        synchronized (streamData) {
            streamData.remove(id);
            streamTypeSpecs.remove(id);
        }
    }

    Object get(int id) throws IOException, StreamException {
        Object result;
        synchronized (streamData) {
            if (!streamTypeSpecs.containsKey(id))
                throw new StreamException("Stream does not exist");
            if (streamValues.containsKey(id))
                return streamValues.get(id);
            result = Encoder.decode(streamData.get(id), streamTypeSpecs.get(id), connection);
            streamValues.put(id, result);
        }
        return result;
    }

    void update(int id, Response response) throws StreamException {
        synchronized (streamData) {
            if (!streamData.containsKey(id))
                throw new StreamException("Stream does not exist");
            if (response.getHasError())
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
                    StreamMessage message = StreamMessage.parseFrom(data);
                    for (StreamResponse response : message.getResponsesList())
                        manager.update(response.getId(), response.getResponse());
                }
             // TODO: handle these exceptions properly
            } catch (StreamException e) {
            } catch (IOException e) {
            }
        }
    }
}
