package krpc.client;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.Map;
import java.util.Queue;
import java.util.Set;
import java.util.WeakHashMap;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentLinkedQueue;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicLong;
import java.util.function.Consumer;
import krpc.client.services.KRPC;
import krpc.schema.KRPC.ProcedureCall;
import krpc.schema.KRPC.ProcedureResult;
import krpc.schema.KRPC.StreamResult;
import krpc.schema.KRPC.Type;

/**
 * The streams of a connection. The thread reading the socket registers streams and stores their
 * values and endings, and the dispatcher thread wakes their waiters and calls their callbacks.
 */
class StreamManager {
  private Connection connection;
  private Session session;
  private KRPC krpc;
  final Object updateLock;
  // Concurrent, so the session can ask whether it is empty without the update lock
  private final Map<Long, StreamImpl> streams = new ConcurrentHashMap<Long, StreamImpl>();
  // Volatile, so a thread holding a stream's condition reads it without the update lock
  private volatile boolean closed = false;
  // Whether a thread is closing the connection
  private volatile boolean closeRequested = false;
  // Every registered stream, to find one whose condition the closing thread holds
  private final Set<StreamImpl> registered =
      Collections.newSetFromMap(new WeakHashMap<StreamImpl, Boolean>());

  StreamManager(Connection connection, Session session) {
    this.connection = connection;
    this.session = session;
    krpc = KRPC.newInstance(connection);
    updateLock = new Object();
  }

  /**
   * Notes that the connection is closing. The dispatcher thread then stops trying to take a
   * callback's lock, and the closing thread ends the streams.
   */
  void requestClose() {
    closeRequested = true;
  }

  /** Whether the connection has closed and every stream has ended. */
  boolean isClosed() {
    return closed;
  }

  /** Whether a thread is closing the connection. */
  boolean isCloseRequested() {
    return closeRequested;
  }

  /** Whether this thread holds the condition of a stream. */
  boolean holdsCondition() {
    synchronized (updateLock) {
      for (StreamImpl stream : registered) {
        if (Thread.holdsLock(stream.getCondition())) {
          return true;
        }
      }
    }
    return false;
  }

  /** Whether any stream is open. */
  boolean hasStreams() {
    return !streams.isEmpty();
  }

  /**
   * Registers a stream the server has added for this client. Called by the reader, before it reads
   * the stream's first update.
   */
  void register(long id) {
    synchronized (updateLock) {
      if (closed || streams.containsKey(id)) {
        return;
      }
      StreamImpl stream = new StreamImpl(connection, this, id, null, updateLock);
      streams.put(id, stream);
      registered.add(stream);
    }
  }

  /** Whether a stream is registered for the id. */
  boolean isRegistered(long id) {
    synchronized (updateLock) {
      return streams.containsKey(id);
    }
  }

  StreamImpl addStream(Type returnType, ProcedureCall call) throws RPCException {
    long id = krpc.addStream(call).getId();
    return getStream(returnType, id);
  }

  StreamImpl getStream(Type returnType, long id) {
    boolean added = false;
    StreamImpl stream;
    synchronized (updateLock) {
      stream = streams.get(id);
      if (stream != null) {
        if (stream.getReturnType() != null) {
          return stream;
        }
        stream.setReturnType(returnType);
        ProcedureResult raw = stream.raw;
        stream.raw = null;
        if (raw != null) {
          // No handle has been given out, so there is no waiter to wake or callback to call
          Decoded decoded = decode(stream, raw);
          if (decoded.undecodable) {
            // Dropped, so the stream has no value yet
          } else if (decoded.ending != null) {
            stream.end(decoded.ending, true, false);
          } else {
            stream.setValue(decoded.value);
            stream.announceLocked(decoded.value, stream.getVersion());
          }
        }
      } else {
        stream = new StreamImpl(connection, this, id, returnType, updateLock);
        if (closed) {
          stream.end(closedError(null));
          return stream;
        }
        streams.put(id, stream);
        registered.add(stream);
        added = true;
      }
    }
    if (added) {
      session.streamAdded();
    }
    return stream;
  }

  void removeStream(StreamImpl stream) throws RPCException {
    long id = stream.getId();
    synchronized (updateLock) {
      if (streams.get(id) != stream) {
        // A stream the server or the connection has already ended
        return;
      }
      streams.remove(id);
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
   * the closing thread, and by the dispatcher thread when the connection fails, with the error that
   * ended it. An Error from an end callback is thrown once every end callback has been called.
   */
  void notifyClosed(Exception cause) {
    List<StreamImpl> ended = new ArrayList<StreamImpl>();
    List<List<Consumer<Exception>>> callbacks = new ArrayList<List<Consumer<Exception>>>();
    Exception ending = closedError(cause);
    synchronized (updateLock) {
      closed = true;
      // An ending needs only the update lock, so every stream ends here
      for (StreamImpl stream : streams.values()) {
        List<Consumer<Exception>> streamCallbacks = stream.end(ending);
        if (streamCallbacks != null) {
          ended.add(stream);
          callbacks.add(streamCallbacks);
        }
      }
      streams.clear();
    }
    wakeAll(ended);
    Error deferred = null;
    for (List<Consumer<Exception>> streamCallbacks : callbacks) {
      Error error = callEndCallbacks(streamCallbacks, ending);
      if (deferred == null) {
        deferred = error;
      }
    }
    if (deferred != null) {
      throw deferred;
    }
  }

  /** A change to a stream for the dispatcher to apply: a new value, or an ending. */
  private static class Change {
    StreamImpl stream;
    List<StreamImpl.Callback> callbacks;
    long version;
    long fired;
    Object value;
    List<Consumer<Exception>> endCallbacks;
    Exception ending;
  }

  /**
   * Stores the values and endings of a stream update, and queues the work of waking waiters and
   * calling callbacks with the dispatcher. Called by the reader. A result for a stream not in the
   * registry is dropped, as the client has removed it.
   */
  void update(List<StreamResult> results) {
    List<Change> changes = new ArrayList<Change>();
    synchronized (updateLock) {
      for (StreamResult result : results) {
        StreamImpl stream = streams.get(result.getId());
        if (stream == null) {
          continue;
        }
        if (stream.getReturnType() == null) {
          // Kept for the first handle, unless an ending is already kept
          if (stream.raw == null || !stream.raw.hasError()) {
            stream.raw = result.getResult();
          }
          continue;
        }
        Decoded decoded = decode(stream, result.getResult());
        if (decoded.undecodable) {
          continue;
        }
        Change change = new Change();
        change.stream = stream;
        if (decoded.ending != null) {
          change.endCallbacks = stream.end(decoded.ending, false, false);
          change.ending = decoded.ending;
        } else {
          stream.setValue(decoded.value);
          // Read with the value stored, so a callback added after this reads the value itself
          change.callbacks = new ArrayList<StreamImpl.Callback>(stream.getCallbacks().values());
          change.version = stream.getVersion();
          change.fired = stream.getFired();
          change.value = decoded.value;
        }
        changes.add(change);
      }
    }
    if (!changes.isEmpty()) {
      session.dispatch(() -> apply(changes));
    }
  }

  /**
   * Stores one result and waits for the dispatcher thread to apply it. For the tests, which stand
   * in for the reader.
   */
  void update(long id, ProcedureResult result) {
    update(Collections.singletonList(
        StreamResult.newBuilder().setId(id).setResult(result).build()));
    CountDownLatch applied = new CountDownLatch(1);
    session.dispatch(applied::countDown);
    try {
      applied.await(10, TimeUnit.SECONDS);
    } catch (InterruptedException exn) {
      Thread.currentThread().interrupt();
    }
  }

  /**
   * Wakes the waiters of streams with a new value or an ending, and calls their callbacks. Runs on
   * the dispatcher thread, with no lock held.
   */
  private void apply(List<Change> changes) {
    for (Change change : changes) {
      StreamImpl stream = change.stream;
      if (change.ending != null) {
        RacePoint.reach(RacePoint.STREAM_ENDING, -1);
        stream.wakeAnnouncingEnding();
        if (change.endCallbacks != null
            && !stream.deferEnding(change.endCallbacks, change.ending)) {
          callEndCallbacks(change.endCallbacks, change.ending);
        }
        continue;
      }
      RacePoint.reach(RacePoint.STREAM_VALUE, -1);
      stream.announce(change.value, change.version);
      RacePoint.reach(RacePoint.STREAM_CALLBACKS, -1);
      for (StreamImpl.Callback callback : change.callbacks) {
        stream.call(callback, callback.event ? change.fired : change.version, change.value);
      }
    }
  }

  /** A decoded result, holding either a value or the exception that ends the stream. */
  static class Decoded {
    final Object value;
    final Exception ending;
    // Whether the value failed to decode, which drops it
    final boolean undecodable;

    Decoded(Object value, Exception ending, boolean undecodable) {
      this.value = value;
      this.ending = ending;
      this.undecodable = undecodable;
    }
  }

  /**
   * Decodes a result. A final result removes the stream from the registry, as the server has
   * already removed it. A value that fails to decode is dropped. Called with the update lock held.
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
        undecodable = true;
      }
    }
    if (ending != null && streams.get(stream.getId()) == stream) {
      streams.remove(stream.getId());
    }
    return new Decoded(value, ending, undecodable);
  }

  /**
   * Ends a stream out of the registry, wakes its waiters and calls its end callbacks. Does nothing
   * if the stream has already ended. An Error from an end callback is thrown once every end
   * callback has been called.
   */
  private void endStream(StreamImpl stream, Exception ending) {
    List<Consumer<Exception>> callbacks = stream.end(ending);
    if (callbacks != null) {
      stream.wake();
      Error deferred = callEndCallbacks(callbacks, ending);
      if (deferred != null) {
        throw deferred;
      }
    }
  }

  /** Calls end callbacks. Returns the first Error one throws, or null. */
  static Error callEndCallbacks(List<Consumer<Exception>> callbacks, Exception ending) {
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

  private static Exception closedError(Exception cause) {
    return new StreamException(
        "Connection closed", cause == null ? new ConnectionException("Connection closed") : cause);
  }

  // Marks the dispatcher thread
  private static final ThreadLocal<Boolean> onDispatcherThread =
      ThreadLocal.withInitial(() -> false);

  static void markDispatcherThread() {
    onDispatcherThread.set(true);
  }

  /**
   * Runs a user callback. An exception it throws goes to the thread's uncaught exception handler,
   * which by default reports it on stderr, and the caller carries on. On the dispatcher thread
   * every exception is reported. On another thread an Error propagates.
   */
  static void invokeCallback(Runnable callback) {
    try {
      callback.run();
    } catch (Error exn) {
      if (!onDispatcherThread.get()) {
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
}
