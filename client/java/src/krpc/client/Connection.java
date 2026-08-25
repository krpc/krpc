package krpc.client;

import com.google.protobuf.ByteString;
import com.google.protobuf.CodedInputStream;
import com.google.protobuf.CodedOutputStream;
import java.io.IOException;
import java.lang.reflect.Constructor;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.lang.reflect.Modifier;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.UnixDomainSocketAddress;
import java.nio.channels.Channels;
import java.nio.channels.SocketChannel;
import java.nio.file.Paths;
import java.time.Duration;
import java.util.Arrays;
import java.util.HashMap;
import java.util.Map;
import krpc.schema.KRPC;

/** A connection to a kRPC server. */
public class Connection implements AutoCloseable {
  private final Object connectionLock = new Object();

  private SocketChannel rpcChannel;
  private CodedOutputStream rpcOutputStream;
  private CodedInputStream rpcInputStream;
  StreamManager streamManager;

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
  private static int DEFAULT_RPC_PORT = 50000;
  private static int DEFAULT_STREAM_PORT = 50001;
  private static Duration DEFAULT_TIMEOUT = Duration.ZERO;

  /**
   * Connect to a kRPC server using a blank client name, on the default
   * address {@value #DEFAULT_ADDRESS}, RPC port number
   * {@value #DEFAULT_RPC_PORT} and stream port number
   * {@value #DEFAULT_STREAM_PORT}.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance() throws IOException {
    return new Connection(EMPTY_NAME, DEFAULT_ADDRESS, DEFAULT_RPC_PORT, DEFAULT_STREAM_PORT);
  }

  /**
   * Connect to a kRPC server using the given client name, on the default
   * address {@value #DEFAULT_ADDRESS}, RPC port number
   * {@value #DEFAULT_RPC_PORT} and stream port number
   * {@value #DEFAULT_STREAM_PORT}.
   *
   * @param name
   *            The name of the client.
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance(String name) throws IOException {
    return new Connection(name, DEFAULT_ADDRESS, DEFAULT_RPC_PORT, DEFAULT_STREAM_PORT);
  }

  /**
   * Connect to a kRPC server using the given client name, on the given
   * address, RPC port number {@value #DEFAULT_RPC_PORT} and stream port
   * number {@value #DEFAULT_STREAM_PORT}.
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
    return new Connection(name, address, DEFAULT_RPC_PORT, DEFAULT_STREAM_PORT);
  }

  /**
   * Connect to a kRPC server using the given client name, on the given
   * address, RPC port number and stream port.
   *
   * @param name
   *            The name of the client.
   * @param address
   *            The server address to connect to.
   * @param rpcPort
   *            The RPC port to connect to.
   * @param streamPort
   *            The stream port to connect to.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance(
      String name, InetAddress address, int rpcPort, int streamPort)
      throws IOException {
    return new Connection(name, address, rpcPort, streamPort);
  }

  /**
   * Connect to a kRPC server using the given client name, on the given
   * address, RPC port number and stream port, waiting no longer than the given
   * timeout for each connection.
   *
   * @param name
   *            The name of the client.
   * @param address
   *            The server address to connect to.
   * @param rpcPort
   *            The RPC port to connect to.
   * @param streamPort
   *            The stream port to connect to.
   * @param timeout
   *            How long to wait for a connection. Zero waits indefinitely.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance(
      String name, InetAddress address, int rpcPort, int streamPort, Duration timeout)
      throws IOException {
    return new Connection(name, address, rpcPort, streamPort, timeout);
  }

  /**
   * Connect to a kRPC server using the given client name, on the given
   * address, RPC port number {@value #DEFAULT_RPC_PORT} and stream port
   * number {@value #DEFAULT_STREAM_PORT}.
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
    return new Connection(
        name, InetAddress.getByName(address), DEFAULT_RPC_PORT, DEFAULT_STREAM_PORT);
  }

  /**
   * Connect to a kRPC server using the given client name, on the given
   * address, RPC port number and stream port.
   *
   * @param name
   *            The name of the client.
   * @param address
   *            The server address to connect to. Can be either the name of
   *            the host or a textual representation of its IP address.
   * @param rpcPort
   *            The RPC port to connect to.
   * @param streamPort
   *            The stream port to connect to.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance(
      String name, String address, int rpcPort, int streamPort)
      throws IOException {
    return new Connection(name, InetAddress.getByName(address), rpcPort, streamPort);
  }

  /**
   * Connect to a kRPC server using the given client name, on the given
   * address, RPC port number and stream port, waiting no longer than the given
   * timeout for each connection.
   *
   * @param name
   *            The name of the client.
   * @param address
   *            The server address to connect to. Can be either the name of
   *            the host or a textual representation of its IP address.
   * @param rpcPort
   *            The RPC port to connect to.
   * @param streamPort
   *            The stream port to connect to.
   * @param timeout
   *            How long to wait for a connection. Zero waits indefinitely.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newInstance(
      String name, String address, int rpcPort, int streamPort, Duration timeout)
      throws IOException {
    return new Connection(name, InetAddress.getByName(address), rpcPort, streamPort, timeout);
  }

  /**
   * Connect to a kRPC server on the same machine, over unix domain sockets rather than
   * TCP/IP, using a blank client name and the default socket paths.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newLocalInstance() throws IOException {
    return newLocalInstance(EMPTY_NAME);
  }

  /**
   * Connect to a kRPC server on the same machine, over unix domain sockets rather than
   * TCP/IP, using the given client name and the default socket paths.
   *
   * @param name
   *            The name of the client.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newLocalInstance(String name) throws IOException {
    return newLocalInstance(name, defaultPath("rpc"), defaultPath("stream"));
  }

  /**
   * Connect to a kRPC server on the same machine, over the unix domain sockets named by
   * the given paths rather than over TCP/IP. The connection behaves identically once
   * established.
   *
   * @param name
   *            The name of the client.
   * @param rpcPath
   *            The path of the socket the RPC server is listening on.
   * @param streamPath
   *            The path of the socket the stream server is listening on.
   *
   * @return A connection to the kRPC server.
   */
  public static Connection newLocalInstance(String name, String rpcPath, String streamPath)
      throws IOException {
    return new Connection(name, openLocal(rpcPath), () -> openLocal(streamPath));
  }

  /** Opens the connection to the stream server. */
  private interface StreamOpener {
    SocketChannel open() throws IOException;
  }

  private static SocketChannel open(InetAddress address, int port, Duration timeout)
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
    return SocketChannel.open(UnixDomainSocketAddress.of(path));
  }

  /**
   * A default path for a socket of the given name, matching the one the server uses unless
   * it was configured with another. The fallback names a fixed directory rather than asking
   * for the temporary one, which java.io.tmpdir and TMPDIR move for the client and not the
   * server.
   */
  private static String defaultPath(String name) {
    boolean windows = System.getProperty("os.name", "").startsWith("Windows");
    String directory = System.getenv(windows ? "LOCALAPPDATA" : "XDG_RUNTIME_DIR");
    if (directory == null || directory.isEmpty()) {
      String temporary = windows ? System.getProperty("java.io.tmpdir") : "/tmp";
      return Paths.get(temporary, "krpc-" + System.getProperty("user.name"), name).toString();
    }
    return Paths.get(directory, "krpc", name).toString();
  }

  private Connection(String name, InetAddress address, int rpcPort, int streamPort)
      throws IOException {
    this(name, address, rpcPort, streamPort, DEFAULT_TIMEOUT);
  }

  private Connection(
      String name, InetAddress address, int rpcPort, int streamPort, Duration timeout)
      throws IOException {
    this(name, open(address, rpcPort, timeout), () -> open(address, streamPort, timeout));
  }

  /**
   * Perform the connection handshake over an already opened rpc socket. The handshake is the
   * same whatever carries it. The stream socket is opened only once the rpc connection has
   * been accepted, so that a rejected connection does not leave a second one behind.
   */
  private Connection(String name, SocketChannel rpc, StreamOpener openStreamChannel)
      throws IOException {
    rpcChannel = rpc;
    rpcOutputStream = CodedOutputStream.newInstance(Channels.newOutputStream(rpcChannel));
    rpcInputStream = CodedInputStream.newInstance(Channels.newInputStream(rpcChannel));

    KRPC.ConnectionRequest request = KRPC.ConnectionRequest.newBuilder()
        .setType(KRPC.ConnectionRequest.Type.RPC)
        .setClientName(name)
        .build();
    rpcOutputStream.writeMessageNoTag(request);
    rpcOutputStream.flush();

    int size = rpcInputStream.readRawVarint32();
    byte[] data = rpcInputStream.readRawBytes(size);
    KRPC.ConnectionResponse response = KRPC.ConnectionResponse.parseFrom(data);
    if (response.getStatus() != KRPC.ConnectionResponse.Status.OK) {
      throw new ConnectionException(response.getMessage());
    }
    ByteString clientIdentifier = response.getClientIdentifier();

    SocketChannel streamChannel = openStreamChannel.open();
    CodedOutputStream streamOutputStream =
        CodedOutputStream.newInstance(Channels.newOutputStream(streamChannel));
    final CodedInputStream streamInputStream =
        CodedInputStream.newInstance(Channels.newInputStream(streamChannel));

    request = KRPC.ConnectionRequest.newBuilder()
        .setType(KRPC.ConnectionRequest.Type.STREAM)
        .setClientIdentifier(clientIdentifier)
        .build();
    streamOutputStream.writeMessageNoTag(request);
    streamOutputStream.flush();

    size = streamInputStream.readRawVarint32();
    data = streamInputStream.readRawBytes(size);
    response = KRPC.ConnectionResponse.parseFrom(data);
    if (response.getStatus() != KRPC.ConnectionResponse.Status.OK) {
      throw new ConnectionException(response.getMessage());
    }

    streamManager = new StreamManager(this, streamChannel);
  }

  /** Close the connection. */
  @Override
  public void close() throws IOException {
    synchronized (connectionLock) {
      rpcChannel.close();
    }
    streamManager.close();
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
      KRPC.Request request = KRPC.Request.newBuilder().addCalls(call).build();
      byte[] data;
      synchronized (connectionLock) {
        rpcOutputStream.writeMessageNoTag(request);
        rpcOutputStream.flush();
        int size = rpcInputStream.readRawVarint32();
        data = rpcInputStream.readRawBytes(size);
      }
      return KRPC.Response.parseFrom(data);
    } catch (IOException exn) {
      throw new RPCException("Failed to invoke call", exn);
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

  private Map<String, Class<?>> exceptionTypes = new HashMap<String, Class<?>>();

  /**
   * Add an exception type.
   * Should only be called by generated client stubs.
   */
  public <T> void addExceptionType(String service, String name, Class<T> exnType) {
    exceptionTypes.put(service + "." + name, exnType);
  }

  void throwException(KRPC.Error error) throws RPCException {
    String message = error.getDescription();
    if (!error.getStackTrace().isEmpty()) {
      message += "\nServer stack trace:\n" + error.getStackTrace();
    }
    if (!error.getService().isEmpty() && !error.getName().isEmpty()) {
      String key = error.getService() + "." + error.getName();
      if (key.equals("KRPC.InvalidOperationException")) {
        throw new UnsupportedOperationException(message);
      }
      if (key.equals("KRPC.ArgumentException")) {
        throw new IllegalArgumentException(message);
      }
      if (key.equals("KRPC.ArgumentNullException")) {
        throw new IllegalArgumentException(message);
      }
      if (key.equals("KRPC.ArgumentOutOfRangeException")) {
        throw new IndexOutOfBoundsException(message);
      }
      Class<?> exnType = exceptionTypes.get(key);
      Constructor<?> ctor = null;
      if (exnType != null) {
        for (Constructor<?> candidate : exnType.getDeclaredConstructors()) {
          if (candidate.getParameterTypes().length == 1) {
            ctor = candidate;
            break;
          }
        }
      }
      if (ctor == null) {
        // The type is unknown here if the service it belongs to has no generated stubs loaded,
        // and has no usable constructor if it was not generated as an exception type. Report
        // the error itself, named by its type on the server, so that the failure to build an
        // exception for it does not hide it.
        throw new RPCException(key + ": " + message);
      }
      try {
        ctor.setAccessible(true);
        RPCException exn = (RPCException) ctor.newInstance(message);
        throw exn;
      } catch (IllegalAccessException exn) {
        throw new RPCException("Failed to throw server exception");
      } catch (InstantiationException exn) {
        throw new RPCException("Failed to throw server exception");
      } catch (InvocationTargetException exn) {
        throw new RPCException("Failed to throw server exception");
      }
    }
    throw new RPCException(message);
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
