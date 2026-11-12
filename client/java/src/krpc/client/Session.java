package krpc.client;

import com.google.protobuf.InvalidProtocolBufferException;
import java.io.IOException;
import java.io.InterruptedIOException;
import java.net.SocketTimeoutException;
import java.nio.channels.ClosedSelectorException;
import java.time.Duration;
import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.ReentrantLock;
import java.util.function.LongFunction;
import krpc.schema.KRPC;

/**
 * The messages of one connection.
 *
 * <p>One thread at a time reads the socket, the reader. A caller with no reader takes the
 * role and reads until its response arrives, so a call costs no hand-off between threads.
 * While streams are open, the reader thread reads when no call is waiting for a response, so
 * their updates arrive between calls. It waits for data without being the reader, so a caller
 * can take over at once. The reader never runs user code, and takes no lock user code can hold.
 *
 * <p>The dispatcher thread runs callbacks, wakes stream waiters, and makes the writes the reader
 * needs, in the order the reader queues them. The transport is non-blocking once the session
 * starts.
 */
class Session {
  // A partial message's limit without a new byte, none if zero
  private final Duration stallTimeout;
  // The wait for the server to close its side once ours is closed
  private static final long CLOSE_TIMEOUT_MILLISECONDS = 1000;

  /**
   * A call waiting for its response. addsStream is whether the call returns a stream or an event,
   * as the id of the stream it adds. Changed under the state lock.
   */
  private static class Slot {
    final boolean addsStream;
    boolean abandoned;
    volatile boolean done;
    KRPC.Response response;
    ConnectionException error;

    Slot(boolean addsStream, boolean abandoned) {
      this.addsStream = addsStream;
      this.abandoned = abandoned;
    }
  }

  /** Thrown once the connection has ended, to stop the thread reading it. */
  private static class Ended extends Exception {
    private static final long serialVersionUID = 1L;

    Ended() {
      super(null, null, false, false);
    }
  }

  private final Transport transport;
  private final LongFunction<KRPC.Request> removeStreamRequest;
  private StreamManager manager;
  // Held for the whole of a write
  private final ReentrantLock writeLock = new ReentrantLock();
  // Guards the ids, the pending calls, the error, and which thread is reading. Notified when a
  // call completes, when the socket is free to read, and when the reader thread is to read.
  private final Object stateLock = new Object();
  private long nextId = 1;
  private final Map<Long, Slot> pending = new HashMap<Long, Slot>();
  private ConnectionException error;
  // The error that failed the connection before it closed
  private ConnectionException failureBeforeClose;
  private boolean closing;
  private boolean ended;
  // Whether a thread is reading the socket, the number of calls waiting for their response, and
  // the number of those waiting for another thread to read it
  private boolean reading;
  private int callers;
  private int waiting;
  private final CountDownLatch halfClosed = new CountDownLatch(1);
  private final LinkedBlockingQueue<Runnable> queue = new LinkedBlockingQueue<Runnable>();
  private static final Runnable STOP = () -> { };
  private final Thread readerThread;
  private final Thread dispatcherThread;

  Session(Transport transport, LongFunction<KRPC.Request> removeStreamRequest,
          Duration stallTimeout) {
    this.stallTimeout = stallTimeout;
    this.transport = transport;
    this.removeStreamRequest = removeStreamRequest;
    readerThread = new Thread(this::runReader, "krpc-reader");
    readerThread.setDaemon(true);
    dispatcherThread = new Thread(this::runDispatcher, "krpc-dispatcher");
    dispatcherThread.setDaemon(true);
  }

  void start(StreamManager streamManager) throws IOException {
    manager = streamManager;
    transport.startNonBlocking();
    readerThread.start();
    dispatcherThread.start();
  }

  /** Whether this thread is the dispatcher thread, on which callbacks run. */
  boolean onDispatcherThread() {
    return Thread.currentThread() == dispatcherThread;
  }

  /** Wake the reader thread for a stream added outside a call. */
  void streamAdded() {
    synchronized (stateLock) {
      if (callers == 0) {
        stateLock.notifyAll();
      }
    }
  }

  /** Queue work for the dispatcher thread. */
  void dispatch(Runnable work) {
    queue.add(work);
  }

  private void runDispatcher() {
    StreamManager.markDispatcherThread();
    while (true) {
      Runnable work;
      try {
        work = queue.take();
      } catch (InterruptedException exn) {
        continue;
      }
      if (work == STOP) {
        return;
      }
      try {
        work.run();
      } catch (Throwable exn) {
        Thread thread = Thread.currentThread();
        thread.getUncaughtExceptionHandler().uncaughtException(thread, exn);
      }
    }
  }

  private boolean streamsOpen() {
    return manager != null && manager.hasStreams();
  }

  /**
   * Send a request and wait for its response. Throws the connection error if the connection has
   * failed or fails before the response arrives.
   */
  KRPC.Response call(KRPC.Request.Builder request, boolean addsStream)
      throws ConnectionException {
    Slot slot = new Slot(addsStream, false);
    boolean isReader;
    // The slot is added before the request is written, so a fast response finds it. A caller that
    // finds no thread reading becomes the reader in the same hold of the lock.
    synchronized (stateLock) {
      if (error != null) {
        throw error;
      }
      request.setId(nextId);
      pending.put(nextId, slot);
      nextId++;
      callers++;
      isReader = !reading;
      if (isReader) {
        reading = true;
      }
    }
    boolean returned = false;
    try {
      writeRequest(request.build());
      if (isReader) {
        returned = readUntil(slot, true);
      } else {
        await(slot);
      }
    } catch (InterruptedException | InterruptedIOException exn) {
      // A reader discards the response when it arrives
      synchronized (stateLock) {
        slot.abandoned = true;
      }
      Thread.currentThread().interrupt();
      throw new ConnectionException("Interrupted while waiting for a response", exn);
    } finally {
      if (!returned) {
        synchronized (stateLock) {
          if (isReader) {
            reading = false;
          }
          returned();
        }
      }
    }
    if (slot.error != null) {
      throw slot.error;
    }
    return slot.response;
  }

  /**
   * Note a call has stopped waiting, and wake the thread to read next. Called with the state lock
   * held.
   */
  private void returned() {
    callers--;
    wakeNextReader();
  }

  /** Wake the thread to read next. Called with the state lock held. */
  private void wakeNextReader() {
    if (waiting > 0 || (callers == 0 && streamsOpen())) {
      stateLock.notifyAll();
    }
  }

  /**
   * Read messages until a call's response arrives, as the reader, then handle the whole messages
   * already read. The call's own response is handled here, which is the common case. If returning
   * and no whole message is buffered, the call stops reading and stops waiting in the same hold of
   * the lock, and this returns true.
   */
  private boolean readUntil(Slot slot, boolean returning) throws InterruptedIOException {
    try {
      while (!slot.done) {
        KRPC.Response response = receiveResponse();
        if (response == null) {
          continue;
        }
        synchronized (stateLock) {
          if (pending.get(response.getId()) == slot && !slot.addsStream) {
            pending.remove(response.getId());
            slot.response = response;
            slot.done = true;
            if (returning && !transport.hasMessage()) {
              reading = false;
              returned();
              return true;
            }
            continue;
          }
        }
        handleResponse(response);
      }
      handleBuffered();
    } catch (Ended exn) {
      // The slot has failed with the connection
    } catch (RuntimeException exn) {
      end(new ConnectionException("Connection failed: " + exn.getMessage(), exn));
    } catch (Error exn) {
      end(new ConnectionException("Connection failed: " + exn, new RuntimeException(exn)));
    }
    return false;
  }

  /**
   * Handle the whole messages already read, as the reader, before it stops reading. The buffer
   * then holds at most part of a message, and the rest of it wakes the reader thread's selector.
   * Throws Ended once the connection has ended.
   */
  private void handleBuffered() throws Ended, InterruptedIOException {
    while (transport.hasMessage()) {
      // A failed connection has no pending calls to match responses to
      synchronized (stateLock) {
        if (error != null) {
          return;
        }
      }
      KRPC.Message message = receive();
      if (message.getContentCase() == KRPC.Message.ContentCase.RESPONSE) {
        handleResponse(message.getResponse());
      } else {
        handle(message);
      }
    }
  }

  /** Wait for a call's response, reading the socket while no other thread is. */
  private void await(Slot slot) throws InterruptedException, InterruptedIOException {
    synchronized (stateLock) {
      while (!slot.done && reading) {
        waiting++;
        try {
          stateLock.wait();
        } finally {
          waiting--;
        }
      }
      if (slot.done) {
        return;
      }
      reading = true;
    }
    try {
      readUntil(slot, false);
    } finally {
      stopReading();
    }
  }

  private void stopReading() {
    synchronized (stateLock) {
      reading = false;
      wakeNextReader();
    }
  }

  /** Queue a request to remove a stream on the server, whose response is discarded. */
  void removeStream(long id) {
    dispatch(() -> {
      KRPC.Request.Builder request = removeStreamRequest.apply(id).toBuilder();
      synchronized (stateLock) {
        if (error != null) {
          return;
        }
        request.setId(nextId);
        pending.put(nextId, new Slot(false, true));
        nextId++;
      }
      writeRequest(request.build());
    });
  }

  /** Write a call's request. A failed write fails the connection. */
  private void writeRequest(KRPC.Request request) {
    writeLock.lock();
    try {
      transport.send(request);
    } catch (IOException exn) {
      end(new ConnectionException("Connection failed: " + exn.getMessage(), exn));
    } finally {
      writeLock.unlock();
    }
  }

  /**
   * The error that failed the connection before it closed, or null if it had not failed. The
   * streams end with this as their ending's cause.
   */
  ConnectionException failure() {
    synchronized (stateLock) {
      return failureBeforeClose;
    }
  }

  /**
   * Tell the server the connection is closing, then close it. Waits for the server to close its
   * side, for up to a second, discarding what arrives meanwhile. Skips telling the server if the
   * write lock is held for longer than that, such as by a caller blocked on a full send buffer.
   */
  void close() {
    // Cleared for the waits, which return at once while the thread is interrupted
    boolean interrupted = Thread.interrupted();
    try {
      closeUninterrupted();
    } finally {
      if (interrupted) {
        Thread.currentThread().interrupt();
      }
    }
  }

  private void closeUninterrupted() {
    boolean failed;
    synchronized (stateLock) {
      closing = true;
      failed = error != null;
      failureBeforeClose = error;
      // A response that arrives after the disconnect does not reach its call
      fail(new ConnectionException("Connection closed"));
    }
    boolean locked = false;
    try {
      locked = !failed && writeLock.tryLock(CLOSE_TIMEOUT_MILLISECONDS, TimeUnit.MILLISECONDS);
    } catch (InterruptedException exn) {
      Thread.currentThread().interrupt();
    }
    if (locked) {
      try {
        transport.send(KRPC.Message.newBuilder()
              .setDisconnect(KRPC.Disconnect.newBuilder().setReason(KRPC.Disconnect.Reason.CLOSED))
              .build());
        transport.shutdownOutput();
      } catch (IOException exn) {
        // The connection can already have failed
      } finally {
        writeLock.unlock();
      }
    }
    long deadline = System.currentTimeMillis() + CLOSE_TIMEOUT_MILLISECONDS;
    boolean drain;
    // A thread already reading finds the server's close itself
    synchronized (stateLock) {
      waiting++;
      try {
        while (reading && !ended) {
          long remaining = deadline - System.currentTimeMillis();
          if (remaining <= 0) {
            break;
          }
          stateLock.wait(remaining);
        }
      } catch (InterruptedException exn) {
        Thread.currentThread().interrupt();
      } finally {
        waiting--;
      }
      drain = !reading && !ended;
      if (drain) {
        reading = true;
      }
    }
    if (drain) {
      transport.discard(deadline);
    }
    transport.close();
    fail(new ConnectionException("Connection closed"));
    synchronized (stateLock) {
      if (drain) {
        reading = false;
      }
      ended = true;
      stateLock.notifyAll();
    }
  }

  /** Stop the threads, and wait for them except the one this runs on. */
  void join(boolean joinDispatcher) {
    queue.add(STOP);
    Thread current = Thread.currentThread();
    try {
      if (current != readerThread) {
        readerThread.join();
      }
      if (joinDispatcher && current != dispatcherThread) {
        dispatcherThread.join();
      }
    } catch (InterruptedException exn) {
      current.interrupt();
    }
  }

  /** Fail the connection, and every call waiting for a response. */
  private void fail(ConnectionException exn) {
    synchronized (stateLock) {
      if (error == null) {
        error = exn;
      }
      for (Slot slot : pending.values()) {
        slot.error = error;
        slot.done = true;
      }
      pending.clear();
      stateLock.notifyAll();
    }
  }

  /** End the connection, once a thread reading or writing it finds it has ended. */
  private Ended end(ConnectionException cause) {
    if (cause == null) {
      cause = new ConnectionException("Connection closed");
    }
    boolean wasClosing;
    // Failed in the same hold, so a thread that finds the connection ended finds its call failed
    synchronized (stateLock) {
      if (ended) {
        return new Ended();
      }
      ended = true;
      wasClosing = closing;
      fail(cause);
      // The first failure, which the calls throw
      cause = error;
    }
    // The closing thread ends the streams itself. Otherwise their ending's cause is what ended
    // the connection.
    if (!wasClosing && manager != null) {
      final ConnectionException endingCause = cause;
      dispatch(() -> manager.notifyClosed(endingCause));
      queue.add(STOP);
    }
    transport.close();
    synchronized (stateLock) {
      stateLock.notifyAll();
    }
    return new Ended();
  }

  /** Read messages while streams are open and no call is waiting for a response. */
  private void runReader() {
    while (true) {
      synchronized (stateLock) {
        while ((callers > 0 || reading || closing || !streamsOpen()) && !ended) {
          try {
            stateLock.wait();
          } catch (InterruptedException exn) {
            // The reader thread stops only once the connection has ended
          }
        }
        if (ended) {
          return;
        }
      }
      // Wait for data without being the reader, so a call can take over. The buffer holds at
      // most part of a message, as the reader handles the whole ones before it stops reading.
      boolean readable = true;
      try {
        readable = transport.waitReadable();
      } catch (IOException | ClosedSelectorException exn) {
        // The connection has closed
      }
      synchronized (stateLock) {
        if (ended) {
          return;
        }
        if (callers > 0 || reading || closing) {
          continue;
        }
        reading = true;
      }
      try {
        // A wait that finds no data reads only to time a partial message
        if (!readable && (stallTimeout.isZero() || !transport.hasBuffered())) {
          stopReading();
          continue;
        }
        KRPC.Message message = receive();
        if (message.getContentCase() == KRPC.Message.ContentCase.RESPONSE) {
          handleResponse(message.getResponse());
        } else {
          handle(message);
        }
        handleBuffered();
      } catch (Ended exn) {
        stopReading();
        return;
      } catch (InterruptedIOException exn) {
        // The reader thread is not interrupted
      } catch (RuntimeException exn) {
        stopReading();
        end(new ConnectionException("Connection failed: " + exn.getMessage(), exn));
        return;
      } catch (Error exn) {
        // Such as running out of memory for a corrupt message length
        stopReading();
        end(new ConnectionException("Connection failed: " + exn, new RuntimeException(exn)));
        return;
      }
      stopReading();
    }
  }

  /**
   * Read one message, and return the response it holds, or handle it and return null if it holds
   * anything else. Throws Ended once the connection has ended.
   */
  private KRPC.Response receiveResponse() throws Ended, InterruptedIOException {
    byte[] data = receiveData();
    // A Message holding a response is the tag of field 2, then the response's size and bytes, so
    // the response is parsed without the Message
    if (data.length >= 2 && data[0] == 0x12 && data[1] >= 0 && data[1] == data.length - 2) {
      try {
        return KRPC.Response.parser().parseFrom(data, 2, data.length - 2);
      } catch (InvalidProtocolBufferException exn) {
        throw end(protocolError(
            KRPC.Disconnect.Reason.MALFORMED_MESSAGE,
            "Failed to parse a message from the server: " + exn.getMessage(), exn));
      }
    }
    KRPC.Message message = parse(data);
    if (message.getContentCase() == KRPC.Message.ContentCase.RESPONSE) {
      return message.getResponse();
    }
    handle(message);
    return null;
  }

  /** Read one message. Throws Ended once the connection has ended. */
  private KRPC.Message receive() throws Ended, InterruptedIOException {
    return parse(receiveData());
  }

  /** Parse a message. Throws Ended if it is malformed, which ends the connection. */
  private KRPC.Message parse(byte[] data) throws Ended {
    try {
      return KRPC.Message.parseFrom(data);
    } catch (InvalidProtocolBufferException exn) {
      throw end(protocolError(
          KRPC.Disconnect.Reason.MALFORMED_MESSAGE,
          "Failed to parse a message from the server: " + exn.getMessage(), exn));
    }
  }

  /** Read the bytes of one message. Throws Ended once the connection has ended. */
  private byte[] receiveData() throws Ended, InterruptedIOException {
    ConnectionException cause;
    try {
      return transport.readNonBlocking(stallTimeout);
    } catch (InterruptedIOException exn) {
      if (exn instanceof SocketTimeoutException) {
        cause = protocolError(
            KRPC.Disconnect.Reason.TIMEOUT,
            "A message from the server stalled for more than "
            + stallTimeout.toMillis() / 1000.0 + " seconds", null);
      } else {
        throw exn;
      }
    } catch (InvalidProtocolBufferException exn) {
      cause = protocolError(
          KRPC.Disconnect.Reason.MALFORMED_MESSAGE,
          "Failed to parse a message from the server: " + exn.getMessage(), exn);
    } catch (IOException | ClosedSelectorException exn) {
      synchronized (stateLock) {
        cause = closing
            ? null : new ConnectionException("Connection failed: " + exn.getMessage(), exn);
      }
    }
    throw end(cause);
  }

  /** Handle a message other than a response. Throws Ended once the connection has ended. */
  private void handle(KRPC.Message message) throws Ended {
    switch (message.getContentCase()) {
      case STREAM_UPDATE:
        manager.update(message.getStreamUpdate().getResultsList());
        break;
      case DISCONNECT:
        throw end(disconnected(message.getDisconnect()));
      case CONTENT_NOT_SET:
        break;
      default:
        throw end(protocolError(
            KRPC.Disconnect.Reason.PROTOCOL_ERROR,
            "Unexpected " + message.getContentCase() + " message from the server", null));
    }
  }

  /** Handle a response. Throws Ended if its id is unknown, which ends the connection. */
  private void handleResponse(KRPC.Response response) throws Ended {
    Slot slot;
    boolean abandoned;
    synchronized (stateLock) {
      slot = pending.remove(response.getId());
      abandoned = slot != null && slot.abandoned;
    }
    if (slot == null) {
      synchronized (stateLock) {
        // The connection failed on another thread, which removed the slot
        if (error != null) {
          throw new Ended();
        }
      }
      throw end(protocolError(
          KRPC.Disconnect.Reason.PROTOCOL_ERROR,
          "Response with an unknown id " + response.getId() + " from the server", null));
    }
    Long id = streamId(slot, response);
    if (id != null) {
      if (abandoned) {
        // The server stream would otherwise never be removed
        removeStream(id);
      } else {
        // Registered before the next message is read, which the stream's first update follows
        manager.register(id);
      }
    }
    if (!abandoned) {
      synchronized (stateLock) {
        slot.response = response;
        slot.done = true;
        if (waiting > 0) {
          stateLock.notifyAll();
        }
      }
    }
  }

  /** The id of the stream a call that returns a stream or an event added, or null. */
  private static Long streamId(Slot slot, KRPC.Response response) {
    if (!slot.addsStream || response.hasError() || response.getResultsCount() == 0) {
      return null;
    }
    KRPC.ProcedureResult result = response.getResults(0);
    if (result.hasError() || result.getIsNull()) {
      return null;
    }
    try {
      return Decoder.decodeUint64(result.getValue());
    } catch (IOException exn) {
      // The call has succeeded, and its caller decodes the value
      return null;
    }
  }

  private static ConnectionException disconnected(KRPC.Disconnect disconnect) {
    String text = "The server ended the connection (" + disconnect.getReason() + ")";
    if (!disconnect.getMessage().isEmpty()) {
      text += ": " + disconnect.getMessage();
    }
    return new ConnectionException(text, null, disconnect.getReason());
  }

  /**
   * Write a disconnect, then close the write side. Skips both if the write lock is held for longer
   * than the close timeout, such as by a caller blocked on a full send buffer.
   */
  private void disconnect(KRPC.Disconnect.Reason reason, String text) {
    try {
      if (!writeLock.tryLock(CLOSE_TIMEOUT_MILLISECONDS, TimeUnit.MILLISECONDS)) {
        return;
      }
      try {
        transport.send(KRPC.Message.newBuilder()
              .setDisconnect(KRPC.Disconnect.newBuilder().setReason(reason).setMessage(text))
              .build());
        transport.shutdownOutput();
      } finally {
        writeLock.unlock();
      }
    } catch (IOException exn) {
      // The connection has failed already
    } catch (InterruptedException exn) {
      Thread.currentThread().interrupt();
    } finally {
      halfClosed.countDown();
    }
  }

  /**
   * Fail the connection, have the dispatcher tell the server why, and drain the socket until the
   * server closes it, for up to a second after the half-close.
   */
  private ConnectionException protocolError(
      KRPC.Disconnect.Reason reason, String text, Exception inner) {
    ConnectionException failure = inner == null
        ? new ConnectionException(text) : new ConnectionException(text, inner);
    fail(failure);
    // A callback reading the socket runs on the dispatcher thread, so makes the write itself
    if (onDispatcherThread()) {
      disconnect(reason, text);
    } else {
      dispatch(() -> disconnect(reason, text));
      try {
        halfClosed.await(CLOSE_TIMEOUT_MILLISECONDS, TimeUnit.MILLISECONDS);
      } catch (InterruptedException exn) {
        Thread.currentThread().interrupt();
      }
    }
    transport.discard(System.currentTimeMillis() + CLOSE_TIMEOUT_MILLISECONDS);
    return failure;
  }
}
