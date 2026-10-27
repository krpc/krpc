package krpc.client;

import com.google.protobuf.ByteString;
import com.google.protobuf.CodedOutputStream;
import com.google.protobuf.InvalidProtocolBufferException;
import java.io.IOException;
import java.lang.reflect.Constructor;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.lang.reflect.Modifier;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.SocketTimeoutException;
import java.net.UnixDomainSocketAddress;
import java.net.UnknownHostException;
import java.nio.channels.Channels;
import java.nio.channels.SocketChannel;
import java.nio.file.Paths;
import java.time.Duration;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import krpc.schema.KRPC;

/** A connection to a kRPC server. */
public class Connection implements AutoCloseable {
  private static final int PROTOCOL_VERSION = 2;

  final Session session;
  StreamManager streamManager;
  // The return type code of each procedure, by service and then procedure name. Accessed with
  // the map's lock held.
  private final Map<String, Map<String, KRPC.Type.TypeCode>> returnTypeCodes =
      new HashMap<String, Map<String, KRPC.Type.TypeCode>>();
  // Whether returnTypeCodes holds every procedure the server lists
  private boolean listedReturnTypeCodes = false;
  // The class loader the generated stubs are looked up in
  ClassLoader stubsLoader = Connection.class.getClassLoader();

  private static final Map<Class<?>, Class<?>> WRAPPER_TYPES = new HashMap<Class<?>, Class<?>>();

  static {
    WRAPPER_TYPES.put(boolean.class, Boolean.class);
    WRAPPER_TYPES.put(byte.class, Byte.class);
    WRAPPER_TYPES.put(char.class, Character.class);
    WRAPPER_TYPES.put(short.class, Short.class);
    WRAPPER_TYPES.put(int.class, Integer.class);
    WRAPPER_TYPES.put(long.class, Long.class);
    WRAPPER_TYPES.put(float.class, Float.class);
    WRAPPER_TYPES.put(double.class, Double.class);
  }

  private static String EMPTY_NAME = "";
  private static InetAddress DEFAULT_ADDRESS = InetAddress.getLoopbackAddress();
  private static final int DEFAULT_PORT = 50000;
  private static Duration DEFAULT_TIMEOUT = Duration.ZERO;
  private static Duration DEFAULT_HANDSHAKE_TIMEOUT = Duration.ofSeconds(5);
  private static Duration DEFAULT_STALL_TIMEOUT = Duration.ofSeconds(30);

  /**
   * Connect to a kRPC server using a blank client name, on the loopback
   * address and port number {@value #DEFAULT_PORT}.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance() throws IOException {
    return newInstance(EMPTY_NAME, DEFAULT_ADDRESS, DEFAULT_PORT);
  }

  /**
   * Connect to a kRPC server using the given client name, on the loopback
   * address and port number {@value #DEFAULT_PORT}.
   *
   * @param name
   *            The name of the client.
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance(String name) throws IOException {
    return newInstance(name, DEFAULT_ADDRESS, DEFAULT_PORT);
  }

  /**
   * Connect to a kRPC server using the given client name, on the given
   * address and port number {@value #DEFAULT_PORT}.
   *
   * @param name
   *            The name of the client.
   * @param address
   *            The server address to connect to.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance(String name, InetAddress address)
      throws IOException {
    return newInstance(name, address, DEFAULT_PORT);
  }

  /**
   * Connect to a kRPC server using the given client name, on the given
   * address and port number.
   *
   * @param name
   *            The name of the client.
   * @param address
   *            The server address to connect to.
   * @param port
   *            The port to connect to.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance(String name, InetAddress address, int port)
      throws IOException {
    return newInstance(name, address, port, DEFAULT_TIMEOUT);
  }

  /**
   * Connect to a kRPC server using the given client name, on the given
   * address and port number, waiting no longer than the given timeout for
   * the connection.
   *
   * @param name
   *            The name of the client.
   * @param address
   *            The server address to connect to.
   * @param port
   *            The port to connect to.
   * @param timeout
   *            How long to wait for a connection. Zero waits indefinitely.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance(
      String name, InetAddress address, int port, Duration timeout)
      throws IOException {
    return newInstance(name, address, port, timeout, DEFAULT_HANDSHAKE_TIMEOUT);
  }

  /**
   * Connect to a kRPC server using the given client name, on the given
   * address and port number, waiting no longer than the given timeouts.
   *
   * @param name
   *            The name of the client.
   * @param address
   *            The server address to connect to.
   * @param port
   *            The port to connect to.
   * @param timeout
   *            How long to wait for a connection. Zero waits indefinitely.
   * @param handshakeTimeout
   *            How long to wait for the server to accept the connection. Zero
   *            waits indefinitely, such as for a server that asks the player to
   *            approve each connection.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance(
      String name, InetAddress address, int port, Duration timeout,
      Duration handshakeTimeout)
      throws IOException {
    return newInstance(name, address, port, timeout, handshakeTimeout, DEFAULT_STALL_TIMEOUT);
  }

  /**
   * Connect to a kRPC server using the given client name, on the given
   * address and port number, waiting no longer than the given timeouts.
   *
   * @param name
   *            The name of the client.
   * @param address
   *            The server address to connect to.
   * @param port
   *            The port to connect to.
   * @param timeout
   *            How long to wait for a connection. Zero waits indefinitely.
   * @param handshakeTimeout
   *            How long to wait for the server to accept the connection. Zero
   *            waits indefinitely, such as for a server that asks the player to
   *            approve each connection.
   * @param stallTimeout
   *            How long a message from the server may go without a new byte once
   *            it has started to arrive, before the connection fails. Zero waits
   *            indefinitely.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance(
      String name, InetAddress address, int port, Duration timeout,
      Duration handshakeTimeout, Duration stallTimeout)
      throws IOException {
    checkTimeouts(handshakeTimeout, stallTimeout);
    return new Connection(name, open(address, port, timeout), handshakeTimeout, stallTimeout);
  }

  /**
   * Connect to a kRPC server using the given client name, on the given
   * address and port number {@value #DEFAULT_PORT}.
   *
   * @param name
   *            The name of the client.
   * @param address
   *            The server address to connect to. Can be either the name of
   *            the host or a textual representation of its IP address.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance(String name, String address)
      throws IOException {
    return newInstance(name, resolve(address));
  }

  /**
   * Connect to a kRPC server using the given client name, on the given
   * address and port number.
   *
   * @param name
   *            The name of the client.
   * @param address
   *            The server address to connect to. Can be either the name of
   *            the host or a textual representation of its IP address.
   * @param port
   *            The port to connect to.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance(String name, String address, int port)
      throws IOException {
    return newInstance(name, resolve(address), port);
  }

  /**
   * Connect to a kRPC server using the given client name, on the given
   * address and port number, waiting no longer than the given timeout for
   * the connection.
   *
   * @param name
   *            The name of the client.
   * @param address
   *            The server address to connect to. Can be either the name of
   *            the host or a textual representation of its IP address.
   * @param port
   *            The port to connect to.
   * @param timeout
   *            How long to wait for a connection. Zero waits indefinitely.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance(
      String name, String address, int port, Duration timeout)
      throws IOException {
    return newInstance(name, resolve(address), port, timeout);
  }

  /**
   * Connect to a kRPC server using the given client name, on the given
   * address and port number, waiting no longer than the given timeouts.
   * See {@link #newInstance(String, InetAddress, int, Duration, Duration)}.
   *
   * @param name
   *            The name of the client.
   * @param address
   *            The server address to connect to. Can be either the name of
   *            the host or a textual representation of its IP address.
   * @param port
   *            The port to connect to.
   * @param timeout
   *            How long to wait for a connection. Zero waits indefinitely.
   * @param handshakeTimeout
   *            How long to wait for the server to accept the connection. Zero
   *            waits indefinitely.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance(
      String name, String address, int port, Duration timeout, Duration handshakeTimeout)
      throws IOException {
    return newInstance(name, resolve(address), port, timeout, handshakeTimeout);
  }

  /**
   * Connect to a kRPC server using the given client name, on the given
   * address and port number, waiting no longer than the given timeouts.
   * See {@link #newInstance(String, InetAddress, int, Duration, Duration, Duration)}.
   *
   * @param name
   *            The name of the client.
   * @param address
   *            The server address to connect to. Can be either the name of
   *            the host or a textual representation of its IP address.
   * @param port
   *            The port to connect to.
   * @param timeout
   *            How long to wait for a connection. Zero waits indefinitely.
   * @param handshakeTimeout
   *            How long to wait for the server to accept the connection. Zero
   *            waits indefinitely.
   * @param stallTimeout
   *            How long a message from the server may go without a new byte once
   *            it has started to arrive. Zero waits indefinitely.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance(
      String name, String address, int port, Duration timeout, Duration handshakeTimeout,
      Duration stallTimeout)
      throws IOException {
    return newInstance(
        name, resolve(address), port, timeout, handshakeTimeout, stallTimeout);
  }

  /**
   * Connect to a kRPC server on the same machine, over a unix domain socket rather than
   * TCP/IP, using a blank client name and the default socket path.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newLocalInstance() throws IOException {
    return newLocalInstance(EMPTY_NAME);
  }

  /**
   * Connect to a kRPC server on the same machine, over a unix domain socket rather than
   * TCP/IP, using the given client name and the default socket path.
   *
   * @param name
   *            The name of the client.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newLocalInstance(String name) throws IOException {
    return newLocalInstance(name, defaultPath());
  }

  /**
   * Connect to a kRPC server on the same machine, over the unix domain socket named by
   * the given path rather than over TCP/IP. The connection behaves identically once
   * established.
   *
   * @param name
   *            The name of the client.
   * @param path
   *            The path of the socket the server is listening on.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newLocalInstance(String name, String path) throws IOException {
    return newLocalInstance(name, path, DEFAULT_HANDSHAKE_TIMEOUT);
  }

  /**
   * Connect to a kRPC server on the same machine, over the unix domain socket named by
   * the given path, waiting no longer than the given timeout for the server to accept
   * the connection.
   *
   * @param name
   *            The name of the client.
   * @param path
   *            The path of the socket the server is listening on.
   * @param handshakeTimeout
   *            How long to wait for the server to accept the connection. Zero
   *            waits indefinitely.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newLocalInstance(
      String name, String path, Duration handshakeTimeout) throws IOException {
    return newLocalInstance(name, path, handshakeTimeout, DEFAULT_STALL_TIMEOUT);
  }

  /**
   * Connect to a kRPC server on the same machine, over the unix domain socket named by
   * the given path, waiting no longer than the given timeouts.
   *
   * @param name
   *            The name of the client.
   * @param path
   *            The path of the socket the server is listening on.
   * @param handshakeTimeout
   *            How long to wait for the server to accept the connection. Zero
   *            waits indefinitely.
   * @param stallTimeout
   *            How long a message from the server may go without a new byte once
   *            it has started to arrive. Zero waits indefinitely.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newLocalInstance(
      String name, String path, Duration handshakeTimeout, Duration stallTimeout)
      throws IOException {
    checkTimeouts(handshakeTimeout, stallTimeout);
    return new Connection(name, openLocal(path), handshakeTimeout, stallTimeout);
  }

  /** The address of a host name or a textual IP address. */
  private static InetAddress resolve(String address) throws ConnectionException {
    try {
      return InetAddress.getByName(address);
    } catch (UnknownHostException exn) {
      throw new ConnectionException("Failed to resolve " + address + ": " + exn.getMessage(), exn);
    }
  }

  /** Throws IllegalArgumentException for a negative handshake or stall timeout. */
  private static void checkTimeouts(Duration handshakeTimeout, Duration stallTimeout) {
    if (handshakeTimeout.isNegative()) {
      throw new IllegalArgumentException("The handshake timeout must not be negative");
    }
    if (stallTimeout.isNegative()) {
      throw new IllegalArgumentException("The stall timeout must not be negative");
    }
  }

  private static SocketChannel open(InetAddress address, int port, Duration timeout)
      throws IOException {
    try {
      return openChannel(address, port, timeout);
    } catch (ConnectionException exn) {
      throw exn;
    } catch (IOException exn) {
      throw new ConnectionException("Failed to connect to the server: " + exn.getMessage(), exn);
    }
  }

  private static SocketChannel openChannel(InetAddress address, int port, Duration timeout)
      throws IOException {
    if (timeout.isZero()) {
      return SocketChannel.open(new InetSocketAddress(address, port));
    }
    // A network that drops a connection attempt instead of refusing it leaves the client
    // waiting, so bound the wait where one was asked for.
    SocketChannel channel = SocketChannel.open();
    try {
      channel.socket().connect(new InetSocketAddress(address, port), (int) timeout.toMillis());
    } catch (IOException exn) {
      channel.close();
      throw exn;
    }
    return channel;
  }

  private static SocketChannel openLocal(String path) throws IOException {
    try {
      return SocketChannel.open(UnixDomainSocketAddress.of(path));
    } catch (IOException exn) {
      throw new ConnectionException("Failed to connect to the server: " + exn.getMessage(), exn);
    }
  }

  /**
   * The default socket path, matching the one the server uses unless it was configured with
   * another. The fallback sits in a fixed directory rather than the temporary one, which
   * java.io.tmpdir and TMPDIR move for the client and not the server.
   */
  private static String defaultPath() {
    boolean windows = System.getProperty("os.name", "").startsWith("Windows");
    String directory = System.getenv(windows ? "LOCALAPPDATA" : "XDG_RUNTIME_DIR");
    if (directory == null || directory.isEmpty()) {
      String temporary = windows ? System.getProperty("java.io.tmpdir") : "/tmp";
      return Paths.get(temporary, "krpc-" + System.getProperty("user.name") + ".sock").toString();
    }
    return Paths.get(directory, "krpc.sock").toString();
  }

  /**
   * Perform the connection handshake over an already opened socket. The handshake is the same
   * whatever carries it.
   */
  private Connection(
      String name, SocketChannel channel, Duration handshakeTimeout, Duration stallTimeout)
      throws IOException {
    MessageReader reader = new MessageReader(channel);
    try {
      handshake(name, channel, reader, handshakeTimeout);
    } catch (IOException | RuntimeException exn) {
      channel.close();
      throw exn;
    }
    session = new Session(channel, reader, this::removeStreamRequest, stallTimeout);
    streamManager = new StreamManager(this, session);
    session.start(streamManager);
  }

  /** Sends the connection request, and checks the server's response to it. */
  private static void handshake(
      String name, SocketChannel channel, MessageReader reader, Duration timeout)
      throws IOException {
    WriteFault.reach();
    CodedOutputStream output = CodedOutputStream.newInstance(Channels.newOutputStream(channel));
    output.writeMessageNoTag(KRPC.Message.newBuilder()
        .setConnectionRequest(KRPC.ConnectionRequest.newBuilder()
            .setClientName(name)
            .setProtocolVersion(PROTOCOL_VERSION))
        .build());
    output.flush();
    KRPC.Message message;
    try {
      message = KRPC.Message.parseFrom(reader.read(timeout));
    } catch (SocketTimeoutException exn) {
      throw new ConnectionException(
          "The server did not answer the connection request within "
          + timeout.toMillis() / 1000.0 + " seconds", exn);
    } catch (InvalidProtocolBufferException exn) {
      throw new ConnectionException(
          "Failed to parse the server's response to the connection request", exn);
    }
    if (message.hasDisconnect()) {
      KRPC.Disconnect disconnect = message.getDisconnect();
      String text = "The server ended the connection (" + disconnect.getReason() + ")";
      if (!disconnect.getMessage().isEmpty()) {
        text += ": " + disconnect.getMessage();
      }
      throw new ConnectionException(text, null, disconnect.getReason());
    }
    if (!message.hasConnectionResponse()) {
      throw new ConnectionException(
          "Expected a response to the connection request, got "
          + (message.getContentCase() == KRPC.Message.ContentCase.CONTENT_NOT_SET
             ? "a message of an unknown kind" : message.getContentCase().toString()));
    }
    KRPC.ConnectionResponse response = message.getConnectionResponse();
    if (response.getStatus() != KRPC.ConnectionResponse.Status.OK) {
      String text = "The server refused the connection (" + response.getStatus() + ")";
      if (!response.getMessage().isEmpty()) {
        text += ": " + response.getMessage();
      }
      throw new ConnectionException(text, response.getStatus(), null);
    }
    if (response.getProtocolVersion() != PROTOCOL_VERSION) {
      throw new ConnectionException(
          "The server does not support protocol version " + PROTOCOL_VERSION
          + ". Use a client from the same kRPC release as the server.");
    }
  }

  /** A request to remove a stream on the server. */
  private KRPC.Request removeStreamRequest(long id) {
    KRPC.Type uint64 = KRPC.Type.newBuilder().setCode(KRPC.Type.TypeCode.UINT64).build();
    return KRPC.Request.newBuilder()
        .addCalls(buildCall("KRPC", "RemoveStream", Encoder.encode(id, uint64)))
        .build();
  }

  /** Close the connection. Sends the server a disconnect first. */
  @Override
  public void close() throws IOException {
    // The dispatcher thread stops trying to take a callback's lock this thread can hold
    streamManager.requestClose();
    session.close();
    // Streams end before the join, so a thread waiting on one is woken. A callback runs on the
    // dispatcher thread, so a connection closed from one does not join it. A callback can also
    // wait for a condition this thread holds, so the dispatcher thread then ends once this thread
    // releases it.
    boolean holding = streamManager.holdsCondition();
    try {
      streamManager.notifyClosed(session.failure());
    } finally {
      session.join(!holding);
    }
  }

  /**
   * Invoke a remote procedure call. Should not be called directly. This
   * interface is for generated service code.
   */
  public KRPC.ProcedureResult invoke(String service, String procedure, ByteString... arguments)
      throws RPCException {
    return invoke(buildCall(service, procedure, arguments));
  }

  private KRPC.ProcedureResult invoke(KRPC.ProcedureCall call) throws RPCException {
    KRPC.Response response = invokeInternal(call);
    KRPC.Error error = getErrorFromResponse(response);
    if (error != null) {
      throwException(error);
    }
    return getResultFromResponse(response);
  }

  KRPC.Response invokeInternal(KRPC.ProcedureCall call) throws RPCException {
    try {
      // The reader registers a stream or an event the call adds before it reads the stream's
      // first update
      KRPC.Type.TypeCode code = getReturnTypeCode(call.getService(), call.getProcedure());
      KRPC.Type.TypeCode streamCode =
          code == KRPC.Type.TypeCode.STREAM || code == KRPC.Type.TypeCode.EVENT ? code : null;
      return session.call(KRPC.Request.newBuilder().addCalls(call), streamCode);
    } catch (IOException exn) {
      throw new RPCException("Failed to invoke call", exn);
    }
  }

  /**
   * The return type code of a procedure, looked up in its service's generated stubs, or in the
   * server's list of services if the stubs are not loaded. NONE if the procedure returns nothing.
   */
  private KRPC.Type.TypeCode getReturnTypeCode(String service, String procedure)
      throws IOException {
    synchronized (returnTypeCodes) {
      KRPC.Type.TypeCode code = findReturnTypeCode(service, procedure);
      if (code != null) {
        return code;
      }
      if (listedReturnTypeCodes) {
        return returnTypeCodes.get(service).computeIfAbsent(
            procedure, key -> KRPC.Type.TypeCode.NONE);
      }
    }
    // Listed without the lock, as no lock is held across a call. Threads that race here each
    // list the services.
    KRPC.Services services = null;
    try {
      services = listServices();
    } finally {
      synchronized (returnTypeCodes) {
        // Listed once, even if the listing fails
        listedReturnTypeCodes = true;
        if (services != null) {
          storeReturnTypeCodes(services);
        }
      }
    }
    synchronized (returnTypeCodes) {
      return returnTypeCodes.get(service).computeIfAbsent(
          procedure, key -> KRPC.Type.TypeCode.NONE);
    }
  }

  /**
   * The return type code of a procedure, from the codes already stored or its service's
   * generated stubs, or null if neither has it. Called with the lock on the return type codes
   * held.
   */
  private KRPC.Type.TypeCode findReturnTypeCode(String service, String procedure) {
    Map<String, KRPC.Type.TypeCode> codes =
        returnTypeCodes.computeIfAbsent(service, key -> new HashMap<String, KRPC.Type.TypeCode>());
    KRPC.Type.TypeCode code = codes.get(procedure);
    if (code != null) {
      return code;
    }
    try {
      Class<?> types =
          Class.forName("krpc.client.services." + service + "$_Types", true, stubsLoader);
      Method getReturnType = types.getMethod("getReturnType", String.class);
      KRPC.Type type = (KRPC.Type) getReturnType.invoke(null, procedure);
      code = type == null ? KRPC.Type.TypeCode.NONE : type.getCode();
      codes.put(procedure, code);
      return code;
    } catch (ReflectiveOperationException | LinkageError exn) {
      // The stubs for the service or procedure are not loaded, or do not link
      return null;
    }
  }

  /** The services the server lists, or null if the call fails. */
  private KRPC.Services listServices() throws IOException {
    KRPC.Response response =
        session.call(KRPC.Request.newBuilder().addCalls(buildCall("KRPC", "GetServices")), null);
    if (getErrorFromResponse(response) != null) {
      return null;
    }
    return KRPC.Services.parseFrom(getResultFromResponse(response).getValue());
  }

  /**
   * Stores the return type code of every procedure in the given services. Called with the lock
   * on the return type codes held.
   */
  private void storeReturnTypeCodes(KRPC.Services services) {
    for (KRPC.Service service : services.getServicesList()) {
      Map<String, KRPC.Type.TypeCode> codes = returnTypeCodes.computeIfAbsent(
          service.getName(), key -> new HashMap<String, KRPC.Type.TypeCode>());
      for (KRPC.Procedure procedure : service.getProceduresList()) {
        codes.putIfAbsent(procedure.getName(), procedure.getReturnType().getCode());
      }
    }
  }

  KRPC.Error getErrorFromResponse(KRPC.Response response) {
    if (response.hasError()) {
      return response.getError();
    }
    if (response.getResultsList().get(0).hasError()) {
      return response.getResultsList().get(0).getError();
    }
    return null;
  }

  KRPC.ProcedureResult getResultFromResponse(KRPC.Response response) {
    return response.getResultsList().get(0);
  }

  KRPC.ProcedureCall buildCall(String service, String procedure, ByteString... arguments) {
    KRPC.ProcedureCall.Builder callBuilder = KRPC.ProcedureCall.newBuilder();
    callBuilder.setService(service);
    callBuilder.setProcedure(procedure);
    int position = 0;
    for (ByteString value : arguments) {
      KRPC.Argument.Builder argumentBuilder = KRPC.Argument.newBuilder().setPosition(position);
      // A null encoded value signals a null argument, carried out-of-band by is_null
      // with the value field left unset.
      if (value == null) {
        argumentBuilder.setIsNull(true);
      } else {
        argumentBuilder.setValue(value);
      }
      callBuilder.addArguments(argumentBuilder.build());
      position++;
    }
    return callBuilder.build();
  }

  private KRPC.ProcedureCall buildCall(Method method, ByteString... args) {
    RPCInfo info = method.getAnnotation(RPCInfo.class);
    String service = info.service();
    String procedure = info.procedure();
    return buildCall(service, procedure, args);
  }

  // Read by the reader too
  private Map<String, Class<?>> exceptionTypes = new ConcurrentHashMap<String, Class<?>>();

  /**
   * Add an exception type.
   * Should only be called by generated client stubs.
   */
  public <T> void addExceptionType(String service, String name, Class<T> exnType) {
    exceptionTypes.put(service + "." + name, exnType);
  }

  void throwException(KRPC.Error error) throws RPCException {
    Exception exn = buildException(error);
    if (exn instanceof RuntimeException) {
      throw (RuntimeException) exn;
    }
    throw (RPCException) exn;
  }

  /**
   * Builds the exception for an error from the server. Returns an RPCException or a
   * RuntimeException.
   */
  Exception buildException(KRPC.Error error) {
    String message = error.getDescription();
    if (!error.getStackTrace().isEmpty()) {
      message += "\nServer stack trace:\n" + error.getStackTrace();
    }
    if (!error.getService().isEmpty() && !error.getName().isEmpty()) {
      String key = error.getService() + "." + error.getName();
      Class<?> exnType = exceptionTypes.get(key);
      Constructor<?> ctor = null;
      if (exnType != null) {
        try {
          ctor = exnType.getDeclaredConstructor(String.class);
        } catch (NoSuchMethodException exn) {
          ctor = null;
        }
      }
      if (ctor == null) {
        // The type is unknown here if the service it belongs to has no generated stubs loaded,
        // and has no usable constructor if it was not generated as an exception type. Report
        // the error itself, named by its type on the server, so that the failure to build an
        // exception for it does not hide it.
        return new RPCException(key + ": " + message);
      }
      try {
        ctor.setAccessible(true);
        return (Exception) ctor.newInstance(message);
      } catch (IllegalAccessException exn) {
        return new RPCException("Failed to throw server exception");
      } catch (InstantiationException exn) {
        return new RPCException("Failed to throw server exception");
      } catch (InvocationTargetException exn) {
        return new RPCException("Failed to throw server exception");
      }
    }
    return new RPCException(message);
  }

  /**
   * Create a stream for a static method call.
   *
   * @param clazz
   *            The class containing the static method.
   * @param method
   *            The name of the static method.
   * @param args
   *            The arguments to pass to the method.
   *
   * @return A stream object.
   */
  public <T> Stream<T> addStream(Class<?> clazz, String method, Object... args)
      throws StreamException, RPCException {
    Method methodInfo = getMethodByName(clazz, method, args);
    return new Stream<T>(this, getReturnType(methodInfo), getCall(methodInfo, null, args));
  }

  /**
   * Create a stream for a method call on an object.
   *
   * @param instance
   *            An instance of the object.
   * @param method
   *            The name of the method.
   * @param args
   *            The arguments to pass to the method.
   *
   * @return A stream object.
   */
  public <T> Stream<T> addStream(RemoteObject instance, String method, Object... args)
      throws StreamException, RPCException {
    Method methodInfo = getMethodByName(instance.getClass(), method, args);
    return new Stream<T>(this, getReturnType(methodInfo), getCall(methodInfo, instance, args));
  }

  /**
   * Create a stream from a server side function, evaluated on each update. Each call creates a
   * new stream. Values are decoded at the type the server reports for the function. A type
   * parameter that cannot hold that type throws a ClassCastException where a
   * value is assigned. Throws a StreamException if the function has no value, and an
   * IllegalArgumentException if it has parameters.
   *
   * @param function
   *            The function to evaluate on each stream update.
   *
   * @return A stream object.
   */
  public <T> Stream<T> addStream(krpc.client.services.KRPC.Expression function)
      throws StreamException, RPCException {
    krpc.client.services.KRPC krpcService = krpc.client.services.KRPC.newInstance(this);
    KRPC.Type returnType = expressionReturnType(function);
    if (returnType == null) {
      throw new StreamException("The function does not evaluate to a value");
    }
    KRPC.Stream stream = krpcService.addFunctionStream(function);
    return new Stream<T>(this, returnType, stream.getId());
  }

  /**
   * Run a server side function within a single physics tick, and return its value.
   * The value is decoded at the type the server reports for the function, and is
   * null for a function with no value. The arguments are passed to the function's
   * parameters, and are encoded at the types the server reports for them, with a number
   * widened as Java widens it. An int or a long passes to an unsigned parameter of its width bit
   * for bit, as the client holds unsigned values. A byte or a short that is not negative converts
   * to an unsigned parameter, and an int that is not negative to an unsigned 64-bit parameter.
   * Throws an IllegalArgumentException if the number of arguments
   * differs from the number of parameters, or if a parameter cannot take its argument. The
   * server checks a null for a parameter that is not nullable, and an object of another class,
   * and these also throw an IllegalArgumentException.
   *
   * @param function
   *            The function to evaluate.
   * @param args
   *            The arguments to pass to the function.
   *
   * @return The value of the function.
   */
  @SuppressWarnings("unchecked")
  public <T> T run(krpc.client.services.KRPC.Expression function, Object... args)
      throws RPCException {
    krpc.client.services.KRPC krpcService = krpc.client.services.KRPC.newInstance(this);
    byte[] data = krpcService.runFunction(function, encodeArguments(function, args));
    // The stub returns null for a null value
    if (data == null) {
      return null;
    }
    KRPC.Type returnType = expressionReturnType(function);
    if (returnType == null) {
      return null;
    }
    return (T) Encoder.decode(ByteString.copyFrom(data), returnType, this);
  }

  /**
   * Create an event from a server side function that evaluates to a boolean value. Each call
   * creates a new event. Throws an IllegalArgumentException if the function does not evaluate to
   * a boolean value, or has parameters.
   *
   * @param function
   *            The condition the event waits for.
   *
   * @return An event object.
   */
  public Event addEvent(krpc.client.services.KRPC.Expression function) throws RPCException {
    return krpc.client.services.KRPC.newInstance(this).addEvent(function);
  }

  /**
   * The encoded arguments of a call to a server side function, one per parameter. A single
   * null passed for the arguments is a null argument. Throws an IllegalArgumentException if the
   * number of arguments differs from the number of parameters, or an argument fails to encode.
   */
  private List<byte[]> encodeArguments(
      krpc.client.services.KRPC.Expression function, Object[] args) throws RPCException {
    if (args == null) {
      args = new Object[] {null};
    }
    List<byte[]> encoded = new ArrayList<byte[]>();
    List<KRPC.Type> types = expressionParameterTypes(function);
    if (args.length != types.size()) {
      throw new IllegalArgumentException(
          "The function takes " + types.size() + (types.size() == 1 ? " argument" : " arguments")
              + ", got " + args.length);
    }
    for (int i = 0; i < args.length; i++) {
      Object value = widen(args[i], types.get(i), i + 1);
      ByteString data;
      try {
        data = Encoder.encode(value, types.get(i));
      } catch (ClassCastException | EncodingException exn) {
        String mismatch = Encoder.describeMismatch(value, types.get(i), "argument " + (i + 1));
        if (mismatch == null) {
          mismatch = "Argument " + (i + 1) + " cannot be encoded as the function's parameter of "
              + "type " + types.get(i).getCode() + ": " + exn.getMessage();
        } else {
          mismatch = Character.toUpperCase(mismatch.charAt(0)) + mismatch.substring(1);
        }
        throw new IllegalArgumentException(mismatch, exn);
      }
      encoded.add(data == null ? null : data.toByteArray());
    }
    return encoded;
  }

  /**
   * A number converted to a parameter's numeric type, as Java widens it. The client holds an
   * unsigned value in an int or a long bit for bit, so an int takes an unsigned parameter and a
   * long an unsigned 64-bit one. A byte or a short, and an int for an unsigned 64-bit parameter,
   * must not be negative, and throws an IllegalArgumentException if it is.
   */
  private static Object widen(Object value, KRPC.Type type, int position) {
    boolean small = value instanceof Byte || value instanceof Short;
    boolean whole = small || value instanceof Integer;
    boolean wholeOrLong = whole || value instanceof Long;
    if (!wholeOrLong && !(value instanceof Float) && !(value instanceof Double)) {
      return value;
    }
    Number number = (Number) value;
    switch (type.getCode()) {
      case DOUBLE:
        return number.doubleValue();
      case FLOAT:
        return wholeOrLong || value instanceof Float ? number.floatValue() : value;
      case SINT32:
        return whole ? number.intValue() : value;
      case SINT64:
        return wholeOrLong ? number.longValue() : value;
      case UINT32:
        return small ? checkNotNegative(number, type, position).intValue() : value;
      case UINT64:
        return whole ? checkNotNegative(number, type, position).longValue() : value;
      default:
        return value;
    }
  }

  private static Number checkNotNegative(Number number, KRPC.Type type, int position) {
    if (number.longValue() < 0) {
      throw new IllegalArgumentException(
          "Argument " + position + " is negative, which the function's parameter of type "
              + type.getCode() + " cannot take");
    }
    return number;
  }

  /** The parameter types of server side functions, by object identifier. */
  private final Map<Long, List<KRPC.Type>> expressionParameterTypes =
      new HashMap<Long, List<KRPC.Type>>();

  private List<KRPC.Type> expressionParameterTypes(krpc.client.services.KRPC.Expression expression)
      throws RPCException {
    long expressionId = RemoteObject.getObjectId(expression);
    synchronized (expressionParameterTypes) {
      if (expressionParameterTypes.containsKey(expressionId)) {
        return expressionParameterTypes.get(expressionId);
      }
    }
    // Introspect outside the lock, as it makes a round trip per property
    List<KRPC.Type> types = new ArrayList<KRPC.Type>();
    for (krpc.client.services.KRPC.Type type : expression.getParameterTypes()) {
      types.add(buildType(type));
    }
    synchronized (expressionParameterTypes) {
      expressionParameterTypes.put(expressionId, types);
    }
    return types;
  }

  /** The return types of server side functions, by object identifier, null for no value. */
  private final Map<Long, KRPC.Type> expressionReturnTypes = new HashMap<Long, KRPC.Type>();

  private KRPC.Type expressionReturnType(krpc.client.services.KRPC.Expression expression)
      throws RPCException {
    long expressionId = RemoteObject.getObjectId(expression);
    synchronized (expressionReturnTypes) {
      if (expressionReturnTypes.containsKey(expressionId)) {
        return expressionReturnTypes.get(expressionId);
      }
    }
    // Introspect outside the lock, as it makes a round trip per property
    KRPC.Type returnType =
        expression.getHasReturnType() ? buildType(expression.getReturnType()) : null;
    synchronized (expressionReturnTypes) {
      expressionReturnTypes.put(expressionId, returnType);
    }
    return returnType;
  }

  /**
   * Build the protocol buffer type message describing the type of the values
   * a server side function evaluates to, by introspecting its return type
   * on the server.
   */
  private KRPC.Type buildType(krpc.client.services.KRPC.Type remoteType) throws RPCException {
    KRPC.Type.Builder builder = KRPC.Type.newBuilder();
    builder.setCode(KRPC.Type.TypeCode.forNumber(remoteType.getCode().getValue()));
    String service = remoteType.getService();
    if (!service.isEmpty()) {
      builder.setService(service);
      builder.setName(remoteType.getName());
    }
    for (krpc.client.services.KRPC.Type subType : remoteType.getTypes()) {
      builder.addTypes(buildType(subType));
    }
    builder.setNullable(remoteType.getNullable());
    return builder.build();
  }

  /**
   * Get the procedure call message for a static method call.
   *
   * @param clazz
   *            The class containing the static method.
   * @param method
   *            The name of the static method.
   * @param args
   *            The arguments to pass to the method.
   *
   * @return A procedure call message.
   */
  public KRPC.ProcedureCall getCall(Class<?> clazz, String method, Object... args)
      throws RPCException {
    return getCall(getMethodByName(clazz, method, args), null, args);
  }

  /**
   * Get the procedure call message for a method call on an object.
   *
   * @param instance
   *            An instance of the object.
   * @param method
   *            The name of the method.
   * @param args
   *            The arguments to pass to the method.
   *
   * @return A procedure call message.
   */
  public KRPC.ProcedureCall getCall(RemoteObject instance, String method, Object... args)
      throws RPCException {
    return getCall(getMethodByName(instance.getClass(), method, args), instance, args);
  }

  private KRPC.ProcedureCall getCall(Method method, Object instance, Object... args)
      throws RPCException {
    KRPC.Type[] parameterTypes;
    RPCInfo info = method.getAnnotation(RPCInfo.class);
    if (info == null) {
      throw new RPCException("Method is not a remote procedure call");
    }
    try {
      Method getParameterTypes = info.types().getMethod("getParameterTypes", String.class);
      parameterTypes = (KRPC.Type[]) getParameterTypes.invoke(null, info.procedure());
    } catch (NoSuchMethodException exn) {
      throw new RPCException("Failed to get procedure call message", exn);
    } catch (IllegalAccessException exn) {
      throw new RPCException("Failed to get procedure call message", exn);
    } catch (InvocationTargetException exn) {
      throw new RPCException("Failed to get procedure call message", exn);
    }

    if (instance == null && Modifier.isStatic(method.getModifiers())) {
      // Remove connection parameter for static methods
      args = Arrays.copyOfRange(args, 1, args.length);
    } else if (instance != null) {
      // Add instance parameter for remote object methods
      Object[] newArgs = new Object[args.length + 1];
      newArgs[0] = instance;
      System.arraycopy(args, 0, newArgs, 1, args.length);
      args = newArgs;
    }

    if (args.length != parameterTypes.length) {
      throw new RPCException("Incorrect number of arguments to remote procedure call");
    }
    ByteString[] encodedArgs = new ByteString[args.length];
    for (int i = 0; i < args.length; i++) {
      encodedArgs[i] = Encoder.encode(args[i], parameterTypes[i]);
    }
    return buildCall(method, encodedArgs);
  }

  private KRPC.Type getReturnType(Method method)
      throws RPCException {
    KRPC.Type returnType;
    RPCInfo info = method.getAnnotation(RPCInfo.class);
    if (info == null) {
      throw new RPCException("Method is not a remote procedure call");
    }
    try {
      Method getReturnType = info.types().getMethod("getReturnType", String.class);
      return (KRPC.Type) getReturnType.invoke(null, info.procedure());
    } catch (NoSuchMethodException exn) {
      throw new RPCException("Failed to get procedure call message", exn);
    } catch (IllegalAccessException exn) {
      throw new RPCException("Failed to get procedure call message", exn);
    } catch (InvocationTargetException exn) {
      throw new RPCException("Failed to get procedure call message", exn);
    }
  }

  private Method getMethodByName(Class<?> clazz, String methodName, Object... args)
      throws RPCException {
    Method[] methods = clazz.getMethods();
    for (Method method : methods) {
      if (!method.getName().equals(methodName)) {
        continue;
      }
      Class<?>[] paramTypes = method.getParameterTypes();
      if (args.length != paramTypes.length) {
        continue;
      }
      boolean matches = true;
      for (int i = 0; i < args.length; i++) {
        if (!isArgumentAssignable(paramTypes[i], args[i])) {
          matches = false;
          break;
        }
      }
      if (matches) {
        return method;
      }
    }
    String params = "";
    for (int i = 0; i < args.length; i++) {
      if (i > 0) {
        params += ",";
      }
      params += args[i] == null ? "null" : args[i].getClass().toString();
    }
    throw new RPCException(
      "Method " + clazz.getName() + "." + methodName + "(" + params + ") not found.");
  }

  // Whether an argument can be passed as a parameter of the given type. Arguments arrive boxed,
  // having been passed through an Object..., so a primitive parameter type is compared against
  // its wrapper type. Null is accepted by any parameter that is not a primitive.
  private static boolean isArgumentAssignable(Class<?> paramType, Object arg) {
    if (arg == null) {
      return !paramType.isPrimitive();
    }
    Class<?> type = paramType.isPrimitive() ? WRAPPER_TYPES.get(paramType) : paramType;
    return type.isAssignableFrom(arg.getClass());
  }
}
