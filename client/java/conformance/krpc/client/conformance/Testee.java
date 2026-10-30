package krpc.client.conformance;

import java.time.Duration;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.Semaphore;
import java.util.function.Consumer;
import java.util.function.IntConsumer;
import java.util.function.Supplier;
import krpc.client.Connection;
import krpc.client.ConnectionException;
import krpc.client.EncodingException;
import krpc.client.Event;
import krpc.client.RPCException;
import krpc.client.RemoteObject;
import krpc.client.StreamException;
import krpc.client.services.TestService;
import krpc.conformance.Testee.AddCallback;
import krpc.conformance.Testee.AddEndCallback;
import krpc.conformance.Testee.AddEventCallback;
import krpc.conformance.Testee.Behavior;
import krpc.conformance.Testee.Command;
import krpc.conformance.Testee.Connect;
import krpc.conformance.Testee.Ending;
import krpc.conformance.Testee.Error;
import krpc.conformance.Testee.Notification;
import krpc.conformance.Testee.Order;
import krpc.conformance.Testee.Output;
import krpc.conformance.Testee.RacePoint;
import krpc.conformance.Testee.Reply;
import krpc.conformance.Testee.Value;
import krpc.conformance.Testee.WaitOnCondition;

/** Runs the commands from the driver against its connections. */
final class Testee {
  private final Main.Writer output;
  // The connections, by the driver's name for each
  private final Map<String, Connection> connections = new HashMap<>();
  // The name of the connection the command this thread runs is for
  private final ThreadLocal<String> currentConnection = ThreadLocal.withInitial(() -> "");
  // The callbacks the driver added, by its id, each with the action that removes it
  private final Map<Long, Runnable> callbacks = new HashMap<>();
  // The releases of blocked callbacks, by callback id
  private final Map<Long, Semaphore> releases = new HashMap<>();
  // The number of calls of each callback, by callback id
  private final Map<Long, Integer> calls = new HashMap<>();

  Testee(Main.Writer output) {
    this.output = output;
  }

  void run(Command command) {
    output.write(Output.newBuilder().setReply(reply(command)).build());
  }

  /**
   * Runs a command. A HoldCondition holds the condition while it runs the thread's later
   * commands, given by next, until a ReleaseCondition, as a monitor is held by a block.
   */
  void run(Command command, Supplier<Command> next) {
    if (!command.hasHoldCondition() || command.getHoldCondition().getStream() == 0) {
      run(command);
      return;
    }
    synchronized (conditionOf(command.getHoldCondition().getStream())) {
      run(command);
      Command later;
      while ((later = next.get()) != null) {
        if (later.hasReleaseCondition()) {
          run(later);
          return;
        }
        run(later, next);
      }
    }
  }

  private Reply reply(Command command) {
    Reply.Builder reply = Reply.newBuilder().setId(command.getId()).setThread(threadName());
    currentConnection.set(command.getConnection());
    try {
      if (command.getCondition() != 0) {
        synchronized (conditionOf(command.getCondition())) {
          dispatch(command, reply);
        }
      } else {
        dispatch(command, reply);
      }
    } catch (Exception exn) {
      reply.setError(toError(exn));
    } catch (FatalError exn) {
      reply.setError(Error.newBuilder().setKind(Error.Kind.FATAL).setMessage(exn.getMessage()));
    }
    return reply.build();
  }

  private static String threadName() {
    return Long.toString(Thread.currentThread().threadId());
  }

  /** Waits on a condition's monitor, which reports no timeout, so the elapsed time does. */
  private static boolean waitOnCondition(WaitOnCondition command) throws InterruptedException {
    Object condition = conditionOf(command.getStream());
    long timeout = (long) (command.getTimeout() * 1000);
    long start = System.nanoTime();
    synchronized (condition) {
      condition.wait(timeout);
    }
    return System.nanoTime() - start < timeout * 1000000L;
  }

  private static Object conditionOf(long handle) {
    Object target = Values.getHandle(handle);
    if (target instanceof Event) {
      return ((Event) target).getCondition();
    }
    return ((StreamHandle) target).getCondition();
  }

  /** Writes the notification for a call of a callback, then does what its behavior says. */
  private void callBack(long callback, Behavior behavior, Consumer<Notification.Builder> fill) {
    Notification.Builder notification =
        Notification.newBuilder().setCallback(callback).setThread(threadName());
    fill.accept(notification);
    output.write(Output.newBuilder().setNotification(notification).build());
    int count;
    synchronized (calls) {
      count = calls.merge(callback, 1, Integer::sum);
    }
    if (count <= behavior.getSkip() || (behavior.getOnly() > 0 && count > behavior.getOnly())) {
      return;
    }
    if (behavior.getSleep() > 0) {
      try {
        Thread.sleep((long) (behavior.getSleep() * 1000));
      } catch (InterruptedException exn) {
        Thread.currentThread().interrupt();
      }
    }
    try {
      switch (behavior.getActionCase()) {
        case BLOCK:
          release(callback).acquireUninterruptibly();
          break;
        case RUN:
          Reply result = reply(behavior.getRun());
          output.write(Output.newBuilder()
              .setNotification(Notification.newBuilder()
                  .setCallback(callback).setThread(threadName()).setResult(result))
              .build());
          break;
        case FAIL:
          throw new IllegalStateException(behavior.getFail());
        case FAIL_FATALLY:
          throw new FatalError(behavior.getFailFatally());
        default:
          break;
      }
    } finally {
      if (behavior.getReportReturn()) {
        output.write(Output.newBuilder()
            .setNotification(Notification.newBuilder()
                .setCallback(callback).setThread(threadName()).setReturned(true))
            .build());
      }
    }
  }

  private Semaphore release(long callback) {
    synchronized (releases) {
      return releases.computeIfAbsent(callback, x -> new Semaphore(0));
    }
  }

  void close() throws Exception {
    synchronized (connections) {
      for (Connection connection : connections.values()) {
        connection.close();
      }
    }
  }

  private void dispatch(Command command, Reply.Builder reply) throws Exception {
    switch (command.getCommandCase()) {
      case CONNECT:
        connect(command.getConnect());
        reply.setValue(Values.resultNone());
        break;
      case CLOSE:
        if (command.getClose().getInterrupted()) {
          Thread.currentThread().interrupt();
          connected().close();
          reply.setValue(Values.resultBool(Thread.interrupted()));
        } else {
          connected().close();
          reply.setValue(Values.resultNone());
        }
        break;
      case INVOKE:
        if (command.getInvoke().hasThrough()) {
          throw new UnsupportedException("A static method is called through its class");
        }
        reply.setValue(Dispatch.invoke(connected(), command.getInvoke()));
        break;
      case ADD_STREAM:
        if (command.getAddStream().getScoped()) {
          throw new UnsupportedException("A stream has no scope idiom");
        }
        reply.setStream(
            Values.addHandle(Dispatch.addStream(connected(), command.getAddStream().getInvoke())));
        break;
      case CALL:
        reply.setValue(Functions.call(connected(), command.getCall().getInvoke()));
        break;
      case ADD_FUNCTION_STREAM:
        reply.setStream(
            Values.addHandle(Functions.addStream(connected(), command.getAddFunctionStream())));
        break;
      case ADD_FUNCTION_EVENT:
        reply.setValue(Functions.addEvent(connected(), command.getAddFunctionEvent()));
        break;
      case RUN_FUNCTION:
        reply.setValue(Functions.run(connected(), command.getRunFunction()));
        break;
      case WAIT_ON_CONDITION:
        reply.setValue(Values.resultBool(waitOnCondition(command.getWaitOnCondition())));
        break;
      case NOTIFY_CONDITION:
        Object condition = conditionOf(command.getNotifyCondition().getStream());
        synchronized (condition) {
          condition.notifyAll();
        }
        reply.setValue(Values.resultNone());
        break;
      case DROP_HANDLE:
        Values.removeHandle(command.getDropHandle().getStream());
        System.gc();
        reply.setValue(Values.resultNone());
        break;
      case COPY_STREAM:
        reply.setStream(Values.addHandle(Values.getHandle(command.getCopyStream().getStream())));
        break;
      case COMPARE_HANDLES:
        reply.setValue(Values.resultBool(clientObject(command.getCompareHandles().getA())
            .equals(clientObject(command.getCompareHandles().getB()))));
        break;
      case HASH_HANDLE:
        reply.setValue(Values.resultUint64(
            clientObject(command.getHashHandle().getStream()).hashCode()));
        break;
      case SET_RATE:
        stream(command.getSetRate().getStream()).setRate(command.getSetRate().getRate());
        reply.setValue(Values.resultNone());
        break;
      case RATE:
        reply.setValue(Values.resultFloat(stream(command.getRate().getStream()).getRate()));
        break;
      case REMOVE:
        Object removed = Values.getHandle(command.getRemove().getStream());
        if (removed instanceof Event) {
          ((Event) removed).remove();
        } else {
          ((StreamHandle) removed).remove();
        }
        reply.setValue(Values.resultNone());
        break;
      case WAIT_EVENT:
        reply.setValue(Values.resultBool(waitEvent(command.getWaitEvent().getEvent(),
            command.getWaitEvent().hasTimeout() ? command.getWaitEvent().getTimeout() : null)));
        break;
      case ADD_EVENT_CALLBACK:
        addEventCallback(command.getAddEventCallback());
        reply.setValue(Values.resultNone());
        break;
      case EVENT_STREAM:
        reply.setStream(Values.addHandle(new StreamHandle(
            event(command.getEventStream().getEvent()).getStream(),
            x -> Values.resultBool((Boolean) x))));
        break;
      case GET:
        reply.setValue(stream(command.getGet().getStream()).get());
        break;
      case WAIT:
        Double timeout = command.getWait().hasTimeout() ? command.getWait().getTimeout() : null;
        reply.setValue(
            Values.resultBool(stream(command.getWait().getStream()).waitForUpdate(timeout)));
        break;
      case ADD_CALLBACK:
        addCallback(command.getAddCallback());
        reply.setValue(Values.resultNone());
        break;
      case ADD_END_CALLBACK:
        addEndCallback(command.getAddEndCallback());
        reply.setValue(Values.resultNone());
        break;
      case ADD_UPDATE_CALLBACK:
        throw new UnsupportedException("A connection has no stream update callback");
      case REMOVE_CALLBACK:
        removeCallback(command.getRemoveCallback().getCallback());
        reply.setValue(Values.resultNone());
        break;
      case REMOVE_END_CALLBACK:
        removeCallback(command.getRemoveEndCallback().getCallback());
        reply.setValue(Values.resultNone());
        break;
      case RELEASE:
        release(command.getRelease().getCallback()).release();
        reply.setValue(Values.resultNone());
        break;
      case HASH:
        reply.setValue(
            Values.resultUint64(Values.argAny(command.getHash().getA()).hashCode()));
        break;
      case OBJECT_ID:
        reply.setValue(Values.resultUint64(
            RemoteObject.getObjectId(Values.argObject(TestService.TestClass.class)
                .apply(command.getObjectId().getA()))));
        break;
      case MEMBERS:
        // The stubs are compiled from the service definitions
        reply.setValue(Value.newBuilder().setNull(true));
        break;
      case ENCODE:
        reply.setValue(Codec.encode(command.getEncode()));
        break;
      case DECODE:
        reply.setValue(Codec.decode(connected(), command.getDecode()));
        break;
      case COMPARE:
        Object a = Values.argAny(command.getCompare().getA());
        Object b = Values.argAny(command.getCompare().getB());
        reply.setValue(Values.resultBool(a == null ? b == null : a.equals(b)));
        break;
      case ORDER:
        reply.setValue(Values.resultSint32(order(command.getOrder())));
        break;
      case HOLD_CONDITION:
        // Held by run, apart from the update condition, which a connection has none of
        if (command.getHoldCondition().getStream() == 0) {
          throw new UnsupportedException("A connection has no stream update condition");
        }
        reply.setValue(Values.resultNone());
        break;
      case RELEASE_CONDITION:
        reply.setValue(Values.resultNone());
        break;
      case WAIT_FOR_UPDATE:
        throw new UnsupportedException("A connection has no wait for a stream update");
      case PAUSE:
        if (command.getPause().getPoint() == RacePoint.UPDATE_CALLBACKS) {
          throw new UnsupportedException("A connection has no stream update callback");
        }
        RacePause.instance().arm(command.getPause().getPoint().name());
        reply.setValue(Values.resultNone());
        break;
      case AWAIT_PAUSE:
        reply.setValue(Values.resultBool(
            RacePause.instance().await(command.getAwaitPause().getTimeout())));
        break;
      case RESUME:
        RacePause.instance().resume();
        reply.setValue(Values.resultNone());
        break;
      case FAIL_WRITE:
        WriteFaults.arm(command.getFailWrite().getMessage());
        reply.setValue(Values.resultNone());
        break;
      default:
        throw new UnsupportedOperationException("Unknown command " + command.getCommandCase());
    }
  }

  private void addCallback(AddCallback command) {
    long callback = command.getCallback();
    StreamHandle stream = stream(command.getStream());
    Behavior behavior = command.getBehavior();
    IntConsumer removal = tag -> addRemoval(callback, () -> stream.removeCallback(tag));
    removal.accept(RacePause.adding(removal, () -> stream.addCallback(
        value -> callBack(callback, behavior, x -> x.setValue(value)))));
  }

  /** Wait on an event, with no timeout if it is null. */
  private static boolean waitEvent(long handle, Double timeout) throws Exception {
    Event event = event(handle);
    synchronized (event.getCondition()) {
      if (timeout == null) {
        event.waitFor();
        return true;
      }
      return event.waitForWithTimeout(timeout);
    }
  }

  private void addEventCallback(AddEventCallback command) {
    long callback = command.getCallback();
    Event event = event(command.getEvent());
    Behavior behavior = command.getBehavior();
    IntConsumer removal = tag -> addRemoval(callback, () -> event.removeCallback(tag));
    removal.accept(RacePause.adding(removal, () -> event.addCallback(
        () -> callBack(callback, behavior, x -> { }))));
  }

  private void addEndCallback(AddEndCallback command) {
    long callback = command.getCallback();
    Behavior behavior = command.getBehavior();
    Consumer<Exception> notify = exn -> {
      Ending.Builder ending = Ending.newBuilder();
      if (exn != null) {
        ending.setError(toError(exn));
      }
      callBack(callback, behavior, x -> x.setEnding(ending));
    };
    Runnable remove;
    Object target = Values.getHandle(command.getStream());
    if (target instanceof Event) {
      Event event = (Event) target;
      int tag = event.addEndCallback(notify);
      remove = () -> event.removeEndCallback(tag);
    } else {
      StreamHandle stream = (StreamHandle) target;
      int tag = stream.addEndCallback(notify);
      remove = () -> stream.removeEndCallback(tag);
    }
    synchronized (callbacks) {
      callbacks.put(callback, remove);
    }
  }

  /** Stores the action that removes a callback, by the driver's id for it. */
  private void addRemoval(long callback, Runnable remove) {
    synchronized (callbacks) {
      callbacks.put(callback, remove);
    }
  }

  private void removeCallback(long callback) {
    Runnable remove;
    synchronized (callbacks) {
      remove = callbacks.remove(callback);
    }
    remove.run();
  }

  private void connect(Connect command) throws Exception {
    String name = currentConnection.get();
    List<Connection> closed = new ArrayList<>();
    synchronized (connections) {
      if (name.isEmpty()) {
        closed.addAll(connections.values());
        connections.clear();
      } else if (connections.containsKey(name)) {
        closed.add(connections.remove(name));
      }
    }
    for (Connection existing : closed) {
      existing.close();
    }
    if (name.isEmpty()) {
      Values.clearHandles();
      synchronized (callbacks) {
        callbacks.clear();
      }
      synchronized (releases) {
        releases.clear();
      }
    }
    Connection connection;
    Duration handshakeTimeout = Duration.ofSeconds(5);
    if (command.hasHandshakeTimeout()) {
      handshakeTimeout = toDuration(command.getHandshakeTimeout());
    }
    Duration stallTimeout = Duration.ofSeconds(30);
    if (command.hasStallTimeout()) {
      stallTimeout = toDuration(command.getStallTimeout());
    }
    if (command.getProtocol().equals("localsocket") && command.getPath().isEmpty()) {
      // Only the overload without a path takes the default path
      if (command.hasHandshakeTimeout()) {
        throw new UnsupportedException("No default path with a handshake timeout");
      }
      connection = Connection.newLocalInstance(command.getName());
    } else if (command.getProtocol().equals("localsocket")) {
      connection = Connection.newLocalInstance(command.getName(), command.getPath(),
                                               handshakeTimeout, stallTimeout);
    } else if (command.getProtocol().equals("tcpip")) {
      String address = command.getAddress().isEmpty() ? "127.0.0.1" : command.getAddress();
      int port = command.getPort() > 0 ? command.getPort() : 50000;
      connection = Connection.newInstance(command.getName(), address, port, Duration.ZERO,
                                          handshakeTimeout, stallTimeout);
    } else {
      throw new UnsupportedOperationException("Unknown protocol " + command.getProtocol());
    }
    synchronized (connections) {
      connections.put(name, connection);
    }
    if (name.isEmpty()) {
      Values.setConnection(connection);
    }
  }

  private Connection connected() {
    synchronized (connections) {
      Connection connection = connections.get(currentConnection.get());
      if (connection == null) {
        throw new IllegalStateException("Not connected");
      }
      return connection;
    }
  }

  private static StreamHandle stream(long handle) {
    return (StreamHandle) Values.getHandle(handle);
  }

  /** The client's stream or event object for a handle. */
  private static Object clientObject(long handle) {
    Object value = Values.getHandle(handle);
    return value instanceof StreamHandle ? ((StreamHandle) value).getStream() : value;
  }

  private static Event event(long handle) {
    return (Event) Values.getHandle(handle);
  }

  /** A timeout as the client takes it, where zero is no timeout. */
  private static Duration toDuration(double seconds) {
    if (Double.isNaN(seconds)) {
      throw new UnsupportedException("A Duration cannot hold NaN");
    }
    if (Double.isInfinite(seconds) && seconds > 0) {
      return Duration.ZERO;
    }
    return Duration.ofMillis((long) (seconds * 1000));
  }

  private static Error toError(Exception exn) {
    Error.Builder error = Error.newBuilder();
    if (exn.getMessage() != null) {
      error.setMessage(exn.getMessage());
    }
    for (Class<?> type = exn.getClass(); type != Object.class; type = type.getSuperclass()) {
      error.addNativeTypes(type.getName());
    }
    if (exn.getCause() != null) {
      error.setCause(exn.getCause().toString());
    }
    if (exn instanceof UnsupportedException) {
      error.setKind(Error.Kind.UNSUPPORTED);
    } else if (exn instanceof ConnectionException) {
      ConnectionException connectionException = (ConnectionException) exn;
      error.setKind(Error.Kind.CONNECTION);
      if (connectionException.getStatus() != null) {
        error.setStatus(connectionException.getStatus().name());
      }
      if (connectionException.getReason() != null) {
        error.setReason(connectionException.getReason().name());
      }
    } else if (exn instanceof StreamException) {
      error.setKind(Error.Kind.STREAM);
    } else if (exn instanceof EncodingException) {
      error.setKind(Error.Kind.ENCODING);
    } else if (exn instanceof IllegalArgumentException) {
      // The client's argument error, which is also the type of a server ArgumentException
      error.setKind(Error.Kind.ARGUMENT);
    } else {
      error.setKind(Error.Kind.RPC);
      // A generated exception type is nested in the class of its service
      Class<?> outer = exn.getClass().getEnclosingClass();
      if (exn instanceof RPCException && outer != null
          && outer.getPackageName().equals("krpc.client.services")) {
        error.setService(outer.getSimpleName());
        error.setName(exn.getClass().getSimpleName());
      }
    }
    return error.build();
  }

  /** The error a callback raises for its fail fatally behavior. */
  static final class FatalError extends java.lang.Error {
    private static final long serialVersionUID = 1L;

    FatalError(String message) {
      super(message);
    }
  }

  /** The client offers no way to do what a command asks. */
  static final class UnsupportedException extends RuntimeException {
    private static final long serialVersionUID = 1L;

    UnsupportedException(String message) {
      super(message);
    }
  }

  /** The sign of the order of two values by compareTo, once equals agrees with it. */
  @SuppressWarnings({"unchecked", "rawtypes"})
  private static int order(Order command) {
    Object a = Values.argAny(command.getA());
    Object b = Values.argAny(command.getB());
    int sign = Integer.signum(((Comparable) a).compareTo(b));
    if ((sign == 0) != a.equals(b)) {
      throw new IllegalStateException("compareTo and equals disagree on the order");
    }
    return sign;
  }
}
