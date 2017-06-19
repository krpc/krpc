package krpc.client;

import com.google.protobuf.ByteString;
import com.google.protobuf.CodedInputStream;
import com.google.protobuf.CodedOutputStream;

import krpc.schema.KRPC;

import java.io.IOException;
import java.lang.reflect.Constructor;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.lang.reflect.Modifier;
import java.net.InetAddress;
import java.net.Socket;
import java.util.Arrays;
import java.util.HashMap;
import java.util.Map;

public class Connection {
  private final Object connectionLock = new Object();

  private Socket rpcSocket;
  private CodedOutputStream rpcOutputStream;
  private CodedInputStream rpcInputStream;
  private StreamManager streamManager;

  private static String EMPTY_NAME = "";
  private static InetAddress DEFAULT_ADDRESS = InetAddress.getLoopbackAddress();
  private static int DEFAULT_RPC_PORT = 50000;
  private static int DEFAULT_STREAM_PORT = 50001;

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

  private Connection(String name, InetAddress address, int rpcPort, int streamPort)
      throws IOException {
    rpcSocket = new Socket(address, rpcPort);
    rpcOutputStream = CodedOutputStream.newInstance(rpcSocket.getOutputStream());
    rpcInputStream = CodedInputStream.newInstance(rpcSocket.getInputStream());

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

    Socket streamSocket = new Socket(address, streamPort);
    CodedOutputStream streamOutputStream =
        CodedOutputStream.newInstance(streamSocket.getOutputStream());
    final CodedInputStream streamInputStream =
        CodedInputStream.newInstance(streamSocket.getInputStream());

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

    streamManager = new StreamManager(this, streamSocket);
  }

  /** Close the connection. */
  public void close() throws IOException {
    synchronized (connectionLock) {
      rpcSocket.close();
    }
    streamManager.close();
  }

  /**
   * Invoke a remote procedure call. Should not be called directly. This
   * interface is for generated service code.
   */
  public ByteString invoke(String service, String procedure, ByteString... arguments)
      throws RPCException {
    return invoke(buildCall(service, procedure, arguments));
  }

  ByteString invoke(KRPC.ProcedureCall call) throws RPCException {
    try {
      KRPC.Request request = KRPC.Request.newBuilder().addCalls(call).build();
      byte[] data;
      synchronized (connectionLock) {
        rpcOutputStream.writeMessageNoTag(request);
        rpcOutputStream.flush();
        int size = rpcInputStream.readRawVarint32();
        data = rpcInputStream.readRawBytes(size);
      }
      KRPC.Response response = KRPC.Response.parseFrom(data);
      if (response.hasError()) {
        throwException(response.getError());
      }
      if (response.getResultsList().get(0).hasError()) {
        throwException(response.getResultsList().get(0).getError());
      }
      return response.getResultsList().get(0).getValue();
    } catch (IOException exn) {
      throw new RPCException("Failed to invoke call", exn);
    }
  }

  KRPC.ProcedureCall buildCall(String service, String procedure, ByteString... arguments) {
    KRPC.ProcedureCall.Builder callBuilder = KRPC.ProcedureCall.newBuilder();
    callBuilder.setService(service);
    callBuilder.setProcedure(procedure);
    if (arguments.length > 0) {
      KRPC.Argument.Builder argumentBuilder = KRPC.Argument.newBuilder();
      int position = 0;
      for (ByteString value : arguments) {
        KRPC.Argument argument = argumentBuilder.setPosition(position).setValue(value).build();
        callBuilder.addArguments(argument);
        position++;
      }
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

  private void throwException(KRPC.Error error) throws RPCException {
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
      Constructor<?>[] ctors = exnType.getDeclaredConstructors();
      Constructor<?> ctor = null;
      for (int i = 0; i < ctors.length; i++) {
        ctor = ctors[i];
        if (ctor.getParameterTypes().length == 1) {
          break;
        }
      }
      try {
        ctor.setAccessible(true);
        RPCException exn = (RPCException)ctor.newInstance(message);
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
    try {
      return internalAddStream(clazz, null, method, args);
    } catch (IOException exn) {
      throw new StreamException("Failed to add stream", exn);
    }
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
    try {
      return internalAddStream(instance.getClass(), instance, method, args);
    } catch (IOException exn) {
      throw new StreamException("Failed to add stream", exn);
    }
  }

  private <T> Stream<T> internalAddStream(
      Class<?> clazz, Object instance, String methodName, Object... args)
      throws StreamException, RPCException, IOException {
    Method[] methods = clazz.getMethods();
    for (Method method : methods) {
      if (method.getName() == methodName) {
        Class<?>[] paramTypes = method.getParameterTypes();
        if (args.length != paramTypes.length) {
          continue;
        }
        for (int i = 0; i < args.length; i++) {
          if (!paramTypes[i].isAssignableFrom(args.getClass())) {
            continue;
          }
        }
        return internalAddStream(method, instance, args);
      }
    }
    String params = "";
    for (int i = 0; i < args.length; i++) {
      if (i > 0) {
        params += ",";
      }
      params += args[i].getClass().toString();
    }
    throw new StreamException(
      "Failed to add stream. "
      + "Method " + clazz.getName() + "." + methodName + "(" + params + ") not found.");
  }

  private <T> Stream<T> internalAddStream(Method method, Object instance, Object... args)
      throws StreamException, RPCException, IOException {
    KRPC.Type returnType;
    KRPC.Type[] parameterTypes;
    RPCInfo info = method.getAnnotation(RPCInfo.class);
    if (info == null) {
      throw new StreamException("Failed to add stream. Method is not an RPC.");
    }
    try {
      Method getReturnType = info.types().getMethod("getReturnType", String.class);
      Method getParameterTypes = info.types().getMethod("getParameterTypes", String.class);
      returnType = (KRPC.Type) getReturnType.invoke(null, info.procedure());
      parameterTypes = (KRPC.Type[]) getParameterTypes.invoke(null, info.procedure());
    } catch (NoSuchMethodException exn) {
      throw new StreamException("Failed to add stream", exn);
    } catch (IllegalAccessException exn) {
      throw new StreamException("Failed to add stream", exn);
    } catch (InvocationTargetException exn) {
      throw new StreamException("Failed to add stream", exn);
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
      throw new StreamException("Failed to add stream. Incorrect number of arguments.");
    }
    ByteString[] encodedArgs = new ByteString[args.length];
    for (int i = 0; i < args.length; i++) {
      encodedArgs[i] = Encoder.encode(args[i], parameterTypes[i]);
    }
    return streamManager.add(buildCall(method, encodedArgs), returnType);
  }
}
