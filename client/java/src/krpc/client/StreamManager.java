package krpc.client;

import com.google.protobuf.CodedInputStream;
import java.io.IOException;
import java.nio.channels.Channels;
import java.nio.channels.SocketChannel;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Queue;
import java.util.Set;
import java.util.concurrent.ConcurrentLinkedQueue;
import java.util.concurrent.atomic.AtomicLong;
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
  final Object updateLock;
  private Map<Long, StreamImpl> streams = new HashMap<Long, StreamImpl>();
  private boolean closed = false;
  // Whether a thread is closing the connection
  private volatile boolean closeRequested = false;
  // Streams out of the registry and not yet ended, with the ending for each. The closing thread
  // ends any the update thread has not yet ended.
  private Map<StreamImpl, Exception> toEnd = new HashMap<StreamImpl, Exception>();
  private Thread updateThread;
  // The highest stream id the server has given this client, and those of its ids not yet
  // registered. A result for one of these, or for a higher id, can arrive before its stream is
  // registered, and is kept until it is.
  private long issued = -1;
  Set<Long> unregistered = new HashSet<Long>();
  Map<Long, Kept> kept = new HashMap<Long, Kept>();

  /**
   * The results kept for a stream not registered yet. A new stream has no callbacks, so only the
   * latest value and the ending are observable once it is registered.
   */
  private static class Kept {
    ProcedureResult value;
    ProcedureResult ending;
  }

  StreamManager(Connection connection, SocketChannel channel) {
    this.connection = connection;
    this.channel = channel;
    krpc = KRPC.newInstance(connection);
    updateLock = new Object();
    updateThread = new Thread(new UpdateThread(this));
    updateThread.start();
  }

  void close() throws IOException {
    closeRequested = true;
    try {
      channel.close();
    } finally {
      notifyClosed();
    }
  }

  /** Whether a thread is closing the connection. */
  boolean isCloseRequested() {
    return closeRequested;
  }

  /** Notes a stream id the server has given this client, in the order its replies arrive. */
  void noteIssued(long id) {
    synchronized (updateLock) {
      issued = Math.max(issued, id);
      if (!streams.containsKey(id)) {
        unregistered.add(id);
      }
    }
  }

  /** Forgets the results kept for a stream id the client has asked the server to remove. */
  void noteRemoved(long id) {
    synchronized (updateLock) {
      issued = Math.max(issued, id);
      unregistered.remove(id);
      kept.remove(id);
    }
  }

  StreamImpl addStream(Type returnType, ProcedureCall call) throws RPCException {
    long id = krpc.addStream(call).getId();
    return getStream(returnType, id);
  }

  StreamImpl getStream(Type returnType, long id) {
    StreamImpl stream = new StreamImpl(connection, this, id, returnType, updateLock);
    boolean ended;
    boolean undecodable = false;
    // The kept results are stored with the condition and then the update lock held, so no update
    // for the id lands between them
    synchronized (stream.getCondition()) {
      synchronized (updateLock) {
        StreamImpl existing = streams.get(id);
        if (existing != null) {
          return existing;
        }
        unregistered.remove(id);
        Kept results = kept.remove(id);
        ended = closed;
        if (!closed) {
          streams.put(id, stream);
          if (results != null) {
            for (ProcedureResult result : new ProcedureResult[] {results.value, results.ending}) {
              if (result != null && stream.getEnding() == null) {
                Decoded decoded = decode(stream, result);
                undecodable = decoded.undecodable;
                if (decoded.ending != null) {
                  // The stream has no end callbacks yet
                  stream.end(decoded.ending);
                  toEnd.remove(stream);
                } else {
                  stream.setValue(decoded.value);
                }
              }
            }
          }
        }
      }
    }
    if (ended) {
      endStream(stream, closedError());
    } else if (undecodable) {
      removeOnServer(id);
    }
    return stream;
  }

  void removeStream(StreamImpl stream) throws RPCException {
    long id = stream.getId();
    Exception serverEnding = null;
    synchronized (updateLock) {
      if (streams.get(id) == stream) {
        streams.remove(id);
      } else {
        // A stream with a decoded final result is ended with that ending. The server has no
        // stream for it to remove.
        serverEnding = toEnd.get(stream);
        if (serverEnding == null) {
          return;
        }
      }
    }
    if (serverEnding != null) {
      endStream(stream, serverEnding);
      return;
    }
    // End the stream before the call, so a value decoded meanwhile is dropped
    Error deferred = null;
    try {
      endStream(stream, new StreamException("The stream has been removed"));
    } catch (Error exn) {
      deferred = exn;
    }
    // An Error from an end callback is thrown once the server has removed the stream
    try {
      krpc.removeStream(id);
    } finally {
      if (deferred != null) {
        throw deferred;
      }
    }
  }

  /**
   * Ends every stream, once the connection is closing and no further update is applied. Called by
   * the closing thread and by the update thread. A stream out of the registry and not yet ended is
   * ended with the ending it was moved out with, so a close keeps a server ending. An Error from
   * an end callback is thrown once every end callback has been called.
   */
  void notifyClosed() {
    List<StreamImpl> ended = new ArrayList<StreamImpl>();
    List<Exception> endings = new ArrayList<Exception>();
    List<List<Consumer<Exception>>> callbacks = new ArrayList<List<Consumer<Exception>>>();
    synchronized (updateLock) {
      closed = true;
      Exception ending = closedError();
      for (StreamImpl stream : streams.values()) {
        toEnd.put(stream, ending);
      }
      streams.clear();
      kept.clear();
      unregistered.clear();
      // An ending needs only the update lock, so every stream ends here
      for (Map.Entry<StreamImpl, Exception> entry : toEnd.entrySet()) {
        List<Consumer<Exception>> streamCallbacks = entry.getKey().end(entry.getValue());
        if (streamCallbacks != null) {
          ended.add(entry.getKey());
          endings.add(entry.getValue());
          callbacks.add(streamCallbacks);
        }
      }
      toEnd.clear();
    }
    wakeAll(ended);
    Error deferred = null;
    for (int i = 0; i < ended.size(); i++) {
      Error error = callEndCallbacks(callbacks.get(i), endings.get(i));
      if (deferred == null) {
        deferred = error;
      }
    }
    if (deferred != null) {
      throw deferred;
    }
  }

  void update(long id, ProcedureResult result) {
    Decoded decoded;
    // Locks are taken as the condition, then the update lock. The update lock is released here
    // before the condition is taken, and the callbacks run with neither held.
    synchronized (updateLock) {
      StreamImpl stream = streams.get(id);
      if (stream == null) {
        // A result for a stream not registered yet is kept. One for an id below those is for a
        // stream that has ended.
        if (closed) {
          return;
        }
        if (id > issued || unregistered.contains(id)) {
          Kept results = kept.computeIfAbsent(id, key -> new Kept());
          if (results.ending != null) {
            return;
          }
          if (result.hasError()) {
            results.ending = result;
          } else {
            results.value = result;
          }
        }
        return;
      }
      decoded = decode(stream, result);
    }
    apply(decoded);
  }

  /** A decoded result, holding either a value or the exception that ends the stream. */
  static class Decoded {
    final StreamImpl stream;
    final Object value;
    final Exception ending;
    // Whether the ending is a value that failed to decode, which the server does not end on
    final boolean undecodable;

    Decoded(StreamImpl stream, Object value, Exception ending, boolean undecodable) {
      this.stream = stream;
      this.value = value;
      this.ending = ending;
      this.undecodable = undecodable;
    }
  }

  /**
   * Decodes a result. A final result moves the stream from the registry to the streams to end, as
   * the server has already removed it. Called with the update lock held.
   */
  Decoded decode(StreamImpl stream, ProcedureResult result) {
    Object value = null;
    Exception ending = null;
    boolean undecodable = false;
    if (result.hasError()) {
      ending = connection.buildException(result.getError());
    } else if (!result.getIsNull()) {
      try {
        value = Encoder.decode(result.getValue(), stream.getReturnType(), connection);
      } catch (RuntimeException exn) {
        // A value that fails to decode ends only its stream
        ending = exn;
        undecodable = true;
      }
    }
    if (ending != null) {
      streams.remove(stream.getId());
      toEnd.put(stream, ending);
    }
    return new Decoded(stream, value, ending, undecodable);
  }

  /**
   * Stores a decoded value or ending, wakes the waiters and calls the callbacks. Called with no
   * lock held.
   */
  void apply(Decoded decoded) {
    StreamImpl stream = decoded.stream;
    if (decoded.ending != null) {
      endStream(stream, decoded.ending);
      if (decoded.undecodable) {
        removeOnServer(stream.getId());
      }
      return;
    }
    List<StreamImpl.Callback> callbacks;
    long version;
    long fired;
    Object condition = stream.getCondition();
    synchronized (condition) {
      synchronized (updateLock) {
        // The stream can be removed while its new value is being decoded. The ending it stored
        // is kept.
        if (stream.getEnding() != null) {
          return;
        }
        stream.setValue(decoded.value);
        // Read with the value stored, so a callback added after this reads the value itself
        callbacks = new ArrayList<StreamImpl.Callback>(stream.getCallbacks().values());
        version = stream.getVersion();
        fired = stream.getFired();
      }
      condition.notifyAll();
    }
    final Object value = decoded.value;
    for (StreamImpl.Callback callback : callbacks) {
      stream.call(callback, callback.event ? fired : version, value);
    }
  }

  /**
   * Removes a stream the client has ended on a value that fails to decode, best effort. The call
   * is made on its own thread, so a slow call on the RPC connection does not hold up the updates.
   */
  private void removeOnServer(long id) {
    Thread thread = new Thread(() -> {
      synchronized (updateLock) {
        // The connection closing also removes the stream
        if (closed) {
          return;
        }
      }
      try {
        krpc.removeStream(id);
      } catch (RPCException | RuntimeException exn) {
        // The connection can have closed, and the stream has ended on this side regardless
      }
    }, "kRPC stream removal");
    thread.setDaemon(true);
    thread.start();
  }

  /**
   * Ends a stream out of the registry, wakes its waiters and calls its end callbacks. Does nothing
   * if the stream has already ended. An Error from an end callback is thrown once every end
   * callback has been called.
   */
  private void endStream(StreamImpl stream, Exception ending) {
    List<Consumer<Exception>> callbacks;
    synchronized (updateLock) {
      callbacks = stream.end(ending);
      toEnd.remove(stream);
    }
    if (callbacks != null) {
      stream.wake();
      Error deferred = callEndCallbacks(callbacks, ending);
      if (deferred != null) {
        throw deferred;
      }
    }
  }

  /** Calls end callbacks. Returns the first Error one throws, or null. */
  private static Error callEndCallbacks(List<Consumer<Exception>> callbacks, Exception ending) {
    Error deferred = null;
    for (Consumer<Exception> callback : callbacks) {
      try {
        invokeCallback(() -> callback.accept(ending));
      } catch (Error exn) {
        if (deferred == null) {
          deferred = exn;
        }
      }
    }
    return deferred;
  }

  // How long a waker makes no progress before the remaining conditions go to another
  private static final long WAKER_STALL_MILLISECONDS = 10;

  /**
   * Wakes the waiters of the streams. A monitor has no timed lock, so each condition is taken by
   * a waker thread. A waker blocked on a held condition is left to finish once it is released,
   * and another takes the rest.
   */
  private static void wakeAll(List<StreamImpl> streams) {
    Queue<Object> conditions = new ConcurrentLinkedQueue<Object>();
    for (StreamImpl stream : streams) {
      Object condition = stream.getCondition();
      if (Thread.holdsLock(condition)) {
        condition.notifyAll();
      } else {
        conditions.add(condition);
      }
    }
    boolean interrupted = false;
    while (!conditions.isEmpty()) {
      Waker waker = new Waker(conditions);
      Thread thread = new Thread(waker, "kRPC stream waker");
      thread.setDaemon(true);
      thread.start();
      long progress = -1;
      while (thread.isAlive() && waker.progress.get() != progress) {
        progress = waker.progress.get();
        try {
          thread.join(WAKER_STALL_MILLISECONDS);
        } catch (InterruptedException exn) {
          interrupted = true;
        }
      }
    }
    if (interrupted) {
      Thread.currentThread().interrupt();
    }
  }

  /** Takes each condition from a queue in turn and wakes its waiters. */
  private static class Waker implements Runnable {
    private final Queue<Object> conditions;
    // The number of conditions woken
    final AtomicLong progress = new AtomicLong();

    Waker(Queue<Object> conditions) {
      this.conditions = conditions;
    }

    @Override
    public void run() {
      Object condition;
      while ((condition = conditions.poll()) != null) {
        synchronized (condition) {
          condition.notifyAll();
        }
        progress.incrementAndGet();
      }
    }
  }

  private static Exception closedError() {
    return new StreamException("Connection closed", new ConnectionException("Connection closed"));
  }

  // Marks the update thread
  private static final ThreadLocal<Boolean> onUpdateThread =
      ThreadLocal.withInitial(() -> false);

  /**
   * Runs a user callback. An exception it throws goes to the thread's uncaught exception handler,
   * which by default reports it on stderr, and the caller carries on. On the update thread every
   * exception is reported, as one that escapes ends the thread and stops every stream. On another
   * thread an Error propagates.
   */
  static void invokeCallback(Runnable callback) {
    try {
      callback.run();
    } catch (Error exn) {
      if (!onUpdateThread.get()) {
        throw exn;
      }
      report(exn);
    } catch (Throwable exn) {
      report(exn);
    }
  }

  private static void report(Throwable exn) {
    Thread thread = Thread.currentThread();
    thread.getUncaughtExceptionHandler().uncaughtException(thread, exn);
  }

  private static class UpdateThread implements Runnable {
    StreamManager manager;

    public UpdateThread(StreamManager manager) {
      this.manager = manager;
    }

    @Override
    public void run() {
      onUpdateThread.set(true);
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
          manager.channel.close();
        } catch (IOException closeExn) {
          // The channel is closed or failed already, and every stream ends regardless
        } finally {
          manager.notifyClosed();
        }
      }
    }
  }
}
