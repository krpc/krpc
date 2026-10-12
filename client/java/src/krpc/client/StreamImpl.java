package krpc.client;

import java.util.ArrayDeque;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Queue;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.ReentrantLock;
import java.util.function.Consumer;
import krpc.client.services.KRPC;
import krpc.schema.KRPC.Type;

/**
 * The state shared by every handle of one stream. A value is stored under the stream's condition
 * and then the update lock, and an ending under the update lock.
 */
class StreamImpl {
  private Connection connection;
  private StreamManager manager;
  private long id;
  private Type returnType;
  private volatile Object value;
  private Object updateLock;
  private Object condition = new Object();
  private Map<Integer, Callback> callbacks = new HashMap<Integer, Callback>();
  private int nextCallbackTag = 0;
  private Map<Integer, Consumer<Exception>> endCallbacks =
      new HashMap<Integer, Consumer<Exception>>();
  private int nextEndCallbackTag = 0;
  private volatile boolean updated = false;
  private long version = 0;
  private long fired = 0;
  // The highest firing count passed to an event callback
  private long callbacksFired = 0;
  // The firing count a replay in flight passes to a late event callback, or zero
  private long replaying = 0;
  private volatile Exception ending = null;
  private float rate = 0;

  StreamImpl(Connection connection, StreamManager manager, long id, Type returnType,
      Object updateLock) {
    this.connection = connection;
    this.manager = manager;
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

  public float getRate() {
    return rate;
  }

  public void setRate(float rate) throws RPCException {
    // The server has no stream for the id once it ends
    if (ending == null) {
      KRPC.newInstance(connection).setStreamRate(id, rate);
    }
    this.rate = rate;
  }

  public Object getValue() throws RPCException, StreamException {
    throwIfEnded();
    if (!updated) {
      throw new StreamException("Stream has no value");
    }
    return value;
  }

  /**
   * Returns the most recent value, waiting for the first. Throws the exception that ended the
   * stream, if it has ended.
   */
  public Object waitForValue() throws RPCException, StreamException {
    if (!updated) {
      synchronized (condition) {
        while (!updated && ending == null) {
          Stream.waitOn(condition, 0);
        }
      }
    }
    return getValue();
  }

  /** Stores a value. The caller holds the condition and then the update lock. */
  public void setValue(Object value) {
    // Set the value before the flag that says there is one. A reader checks the flag first and
    // takes no lock.
    this.value = value;
    updated = true;
    version++;
    if (Boolean.TRUE.equals(value)) {
      fired++;
    }
  }

  public boolean getUpdated() {
    return updated;
  }

  /** The number of values received. Read with the condition held. */
  public long getVersion() {
    return version;
  }

  /** The number of true values received, which for an event is its firings. */
  public long getFired() {
    return fired;
  }

  /** The exception that ended the stream, or null while it is live. */
  public Exception getEnding() {
    return ending;
  }

  /** Throws the exception that ended the stream, if it has ended. */
  public void throwIfEnded() throws RPCException, StreamException {
    Exception exn = ending;
    if (exn == null) {
      return;
    }
    if (exn instanceof RPCException) {
      throw (RPCException) exn;
    }
    if (exn instanceof StreamException) {
      throw (StreamException) exn;
    }
    if (exn instanceof RuntimeException) {
      throw (RuntimeException) exn;
    }
    throw new StreamException(exn.getMessage(), exn);
  }

  /**
   * Ends the stream. Returns the end callbacks to call once its waiters are woken, or null if the
   * stream had already ended. A waiter checks the ending with the condition held, and the waking
   * takes the condition after this, so the ending needs only the update lock.
   */
  List<Consumer<Exception>> end(Exception ending) {
    synchronized (updateLock) {
      if (this.ending != null) {
        return null;
      }
      this.ending = ending;
      List<Consumer<Exception>> result = new ArrayList<Consumer<Exception>>(endCallbacks.values());
      endCallbacks.clear();
      return result;
    }
  }

  /** Wakes the waiters. */
  void wake() {
    synchronized (condition) {
      condition.notifyAll();
    }
  }

  public Object getCondition() {
    return condition;
  }

  /**
   * A value callback, or an event callback. The version is that of the last value passed to a
   * value callback, or the firing count last passed to an event callback. The lock is held while
   * the callback runs, so it runs on one thread at a time. While the adding thread makes the first
   * call, the update thread queues its values in missed for the adding thread to pass on.
   */
  static class Callback {
    final Consumer<Object> function;
    final boolean event;
    final int tag;
    final ReentrantLock lock = new ReentrantLock();
    // Accessed with the update lock held
    long version = 0;
    boolean adding = false;
    final Queue<Missed> missed = new ArrayDeque<Missed>();

    Callback(Consumer<Object> function, boolean event, int tag) {
      this.function = function;
      this.event = event;
      this.tag = tag;
    }
  }

  /** A value the update thread queued for the adding thread, with its version. */
  private static class Missed {
    final long version;
    final Object value;

    Missed(long version, Object value) {
      this.version = version;
      this.value = value;
    }
  }

  /** The callbacks, by tag. Read with the update lock held. */
  public Map<Integer, Callback> getCallbacks() {
    return callbacks;
  }

  /**
   * Calls a callback with a value on the update thread, unless it has been passed a newer one,
   * has been removed, or the stream has ended. Called with no lock held.
   */
  void call(Callback callback, long version, Object value) {
    call(callback, version, value, false);
  }

  /**
   * Calls a callback with a value, unless it has been passed a newer one, has been removed, or
   * the stream has ended. The version is the firing count for an event callback, which is passed
   * only firings. Called with no lock held.
   *
   * <p>The first call, from the adding thread, skips the value if the update thread is calling the
   * callback, as that thread has a newer value. The update thread queues its values for the adding
   * thread while the first call runs.
   */
  private void call(Callback callback, long version, Object value, boolean added) {
    if (callback.event && !Boolean.TRUE.equals(value)) {
      return;
    }
    if (added) {
      if (!callback.lock.tryLock()) {
        return;
      }
    } else if (!enterOnUpdateThread(callback, version, value)) {
      return;
    }
    try {
      synchronized (updateLock) {
        if (ending != null || !notePassed(callback, version)) {
          return;
        }
        callback.adding = added;
      }
      try {
        StreamManager.invokeCallback(() -> callback.function.accept(value));
        if (added) {
          callMissed(callback);
        }
      } catch (Error exn) {
        if (added) {
          // A failed add adds nothing
          synchronized (updateLock) {
            callbacks.remove(callback.tag);
            callback.adding = false;
            callback.missed.clear();
          }
        }
        throw exn;
      }
    } finally {
      callback.lock.unlock();
    }
  }

  // How long the update thread waits for a callback's lock between checks
  private static final long CALLBACK_LOCK_MILLISECONDS = 10;

  /**
   * Takes a callback's lock on the update thread. Returns false, without the lock, once the value
   * is queued for the adding thread or the client is closing.
   */
  private boolean enterOnUpdateThread(Callback callback, long version, Object value) {
    boolean interrupted = false;
    try {
      while (true) {
        try {
          if (callback.lock.tryLock(CALLBACK_LOCK_MILLISECONDS, TimeUnit.MILLISECONDS)) {
            return true;
          }
        } catch (InterruptedException exn) {
          interrupted = true;
        }
        if (manager.isCloseRequested()) {
          return false;
        }
        synchronized (updateLock) {
          if (callback.adding) {
            callback.missed.add(new Missed(version, value));
            return false;
          }
        }
      }
    } finally {
      if (interrupted) {
        Thread.currentThread().interrupt();
      }
    }
  }

  /**
   * Calls a callback, on the adding thread, with the values the update thread queued during its
   * first call. A value queued before the stream ended is still passed on. Clears adding in the
   * same hold of the update lock that finds the queue empty, so the update thread queues no value
   * after the last check. Called with the callback's lock held.
   */
  private void callMissed(Callback callback) {
    while (true) {
      Missed missed;
      synchronized (updateLock) {
        missed = callback.missed.poll();
        if (missed == null) {
          callback.adding = false;
          return;
        }
        if (!notePassed(callback, missed.version)) {
          continue;
        }
      }
      StreamManager.invokeCallback(() -> callback.function.accept(missed.value));
    }
  }

  /**
   * Notes that a callback is passed a version. Returns false, noting nothing, if the callback has
   * been removed or passed that version or a newer one. Called with the update lock held.
   */
  private boolean notePassed(Callback callback, long version) {
    if (version <= callback.version || callbacks.get(callback.tag) != callback) {
      return false;
    }
    callback.version = version;
    if (callback.event) {
      callbacksFired = Math.max(callbacksFired, version);
    }
    return true;
  }

  public int addCallback(Consumer<Object> function) {
    Callback callback;
    boolean hasValue;
    long current;
    Object currentValue;
    synchronized (updateLock) {
      callback = new Callback(function, false, nextCallbackTag);
      nextCallbackTag++;
      callbacks.put(callback.tag, callback);
      hasValue = updated && ending == null;
      current = version;
      currentValue = value;
    }
    // Called with the value the stream already has, which no update will pass to it
    if (hasValue) {
      call(callback, current, currentValue, true);
    }
    return callback.tag;
  }

  /**
   * Adds an event callback. It is called straight away for a firing passed neither to a wait that
   * has seen the given number of firings, nor to the event's callbacks.
   *
   * @param seen The number of firings when a wait on the handle last returned.
   */
  public int addEventCallback(Runnable function, long seen) {
    Callback callback;
    long firing;
    synchronized (updateLock) {
      callback = new Callback((Object value) -> function.run(), true, nextCallbackTag);
      nextCallbackTag++;
      callbacks.put(callback.tag, callback);
      firing = reserveReplay(seen);
    }
    if (firing > 0) {
      replay(callback, firing);
    }
    return callback.tag;
  }

  /**
   * Reserves the latest firing for a late event callback. Returns it, or zero if it was passed to
   * a wait that has seen the given number of firings, passed to an event callback, or reserved by
   * another replay.
   */
  long reserveReplay(long seen) {
    synchronized (updateLock) {
      if (ending != null || fired <= Math.max(seen, Math.max(callbacksFired, replaying))) {
        return 0;
      }
      replaying = fired;
      return fired;
    }
  }

  /**
   * Passes a reserved firing to a late event callback, then releases the reservation. A firing
   * the callback is not passed, as it was removed or the stream ended, is left for the next late
   * callback.
   */
  void replay(Callback callback, long firing) {
    try {
      call(callback, firing, Boolean.TRUE, true);
    } finally {
      synchronized (updateLock) {
        if (replaying == firing) {
          replaying = 0;
        }
      }
    }
  }

  public void removeCallback(int tag) {
    synchronized (updateLock) {
      callbacks.remove(tag);
    }
  }

  public int addEndCallback(Consumer<Exception> callback) {
    int tag;
    Exception exn;
    synchronized (updateLock) {
      tag = nextEndCallbackTag;
      nextEndCallbackTag++;
      exn = ending;
      if (exn == null) {
        endCallbacks.put(tag, callback);
      }
    }
    // A stream that has already ended calls the callback here, as no ending is to come
    if (exn != null) {
      StreamManager.invokeCallback(() -> callback.accept(exn));
    }
    return tag;
  }

  public void removeEndCallback(int tag) {
    synchronized (updateLock) {
      endCallbacks.remove(tag);
    }
  }

  public void remove() throws RPCException {
    manager.removeStream(this);
  }
}
