package krpc.client;

import com.google.protobuf.CodedInputStream;
import java.io.IOException;
import java.nio.channels.Channels;
import java.nio.channels.SocketChannel;
import java.util.ArrayList;
import java.util.Collection;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.function.Consumer;
import krpc.client.services.KRPC;
import krpc.schema.KRPC.ProcedureCall;
import krpc.schema.KRPC.ProcedureResult;
import krpc.schema.KRPC.StreamResult;
import krpc.schema.KRPC.StreamUpdate;
import krpc.schema.KRPC.Type;

class StreamManager {
  private Connection connection;
  SocketChannel channel;
  private KRPC krpc;
  private Object updateLock;
  private Map<Long, StreamImpl> streams = new HashMap<Long, StreamImpl>();
  private boolean closed = false;
  private Thread updateThread;

  StreamManager(Connection connection, SocketChannel channel) {
    this.connection = connection;
    this.channel = channel;
    krpc = KRPC.newInstance(connection);
    updateLock = new Object();
    updateThread = new Thread(new UpdateThread(this));
    updateThread.start();
  }

  void close() throws IOException {
    try {
      channel.close();
    } finally {
      notifyClosed();
    }
  }

  StreamImpl addStream(Type returnType, ProcedureCall call) throws RPCException {
    long id = krpc.addStream(call, false).getId();
    return getStream(returnType, id);
  }

  StreamImpl getStream(Type returnType, long id) {
    StreamImpl stream;
    synchronized (updateLock) {
      stream = streams.get(id);
      if (stream != null) {
        return stream;
      }
      stream = new StreamImpl(connection, id, returnType, updateLock);
      if (!closed) {
        streams.put(id, stream);
        return stream;
      }
    }
    endStreams(Collections.singletonList(stream), closedError());
    return stream;
  }

  void removeStream(StreamImpl stream) throws RPCException {
    long id = stream.getId();
    synchronized (updateLock) {
      if (streams.get(id) != stream) {
        return;
      }
      streams.remove(id);
    }
    // End the stream before the call, so a value decoded meanwhile is dropped
    endStreams(Collections.singletonList(stream), new StreamException("Stream does not exist"));
    krpc.removeStream(id);
  }

  /** Ends every stream, after the connection has closed and no further update can arrive. */
  void notifyClosed() {
    List<StreamImpl> ended;
    synchronized (updateLock) {
      closed = true;
      ended = new ArrayList<StreamImpl>(streams.values());
      streams.clear();
    }
    endStreams(ended, closedError());
  }

  void update(long id, ProcedureResult result) {
    StreamImpl stream;
    Object value = null;
    Exception ending = null;
    List<Consumer<Object>> callbacks;
    // Locks are taken as the condition, then the update lock. The update lock is released here
    // before the condition is taken, and the callbacks run with neither held.
    synchronized (updateLock) {
      stream = streams.get(id);
      if (stream == null) {
        return;
      }
      if (result.hasError()) {
        ending = connection.buildException(result.getError());
      } else if (!result.getIsNull()) {
        try {
          value = Encoder.decode(result.getValue(), stream.getReturnType(), connection);
        } catch (RuntimeException exn) {
          // A value that fails to decode ends only its stream
          ending = exn;
        }
      }
      // A final result ends the stream, and the server has already removed it
      if (ending != null) {
        streams.remove(id);
      }
      callbacks = new ArrayList<Consumer<Object>>(stream.getCallbacks().values());
    }
    if (ending != null) {
      endStreams(Collections.singletonList(stream), ending);
      return;
    }
    Object condition = stream.getCondition();
    synchronized (condition) {
      synchronized (updateLock) {
        // The stream can be removed while its new value is being decoded. The ending it stored
        // is kept.
        if (stream.getEnding() != null) {
          return;
        }
        stream.setValue(value);
      }
      condition.notifyAll();
    }
    final Object callbackValue = value;
    for (Consumer<Object> callback : callbacks) {
      invokeCallback(() -> callback.accept(callbackValue));
    }
  }

  private static void endStreams(Collection<StreamImpl> ended, Exception ending) {
    for (StreamImpl stream : ended) {
      for (Consumer<Exception> callback : stream.end(ending)) {
        invokeCallback(() -> callback.accept(ending));
      }
    }
  }

  private static Exception closedError() {
    return new StreamException("Connection closed", new ConnectionException("Connection closed"));
  }

  /**
   * Runs a user callback. An exception it throws goes to the thread's uncaught exception handler,
   * which by default reports it on stderr, and the caller carries on.
   */
  static void invokeCallback(Runnable callback) {
    try {
      callback.run();
    } catch (Throwable exn) {
      Thread thread = Thread.currentThread();
      thread.getUncaughtExceptionHandler().uncaughtException(thread, exn);
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
            CodedInputStream.newInstance(Channels.newInputStream(manager.channel));
        while (true) {
          int size = inputStream.readRawVarint32();
          byte[] data = inputStream.readRawBytes(size);
          StreamUpdate update = StreamUpdate.parseFrom(data);
          for (StreamResult result : update.getResultsList()) {
            manager.update(result.getId(), result.getResult());
          }
        }
      } catch (IOException exn) {
        // The connection closed or failed, or carried a message that cannot be parsed
      } finally {
        // The connection is unusable, so every stream ends
        try {
          manager.close();
        } catch (IOException closeExn) {
          // The channel is closed or failed already, and every stream ends regardless
        }
      }
    }
  }
}
