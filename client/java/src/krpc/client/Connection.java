package krpc.client;

import java.io.IOException;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.lang.reflect.Modifier;
import java.net.InetAddress;
import java.net.Socket;
import java.util.Arrays;

import com.google.protobuf.ByteString;
import com.google.protobuf.CodedInputStream;
import com.google.protobuf.CodedOutputStream;

import krpc.schema.KRPC;

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
     * @throws IOException
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
     * @throws IOException
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
     * @throws IOException
     */
    public static Connection newInstance(String name, InetAddress address) throws IOException {
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
     * @throws IOException
     */
    public static Connection newInstance(String name, InetAddress address, int rpcPort, int streamPort) throws IOException {
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
     *            the host or a textual representation of its IP address. See
     *            {@link InetAddress.getByName}.
     * 
     * @return A connection to the kRPC server.
     * @throws IOException
     */
    public static Connection newInstance(String name, String address) throws IOException {
        return new Connection(name, InetAddress.getByName(address), DEFAULT_RPC_PORT, DEFAULT_STREAM_PORT);
    }

    /**
     * Connect to a kRPC server using the given client name, on the given
     * address, RPC port number and stream port.
     * 
     * @param name
     *            The name of the client.
     * @param address
     *            The server address to connect to. Can be either the name of
     *            the host or a textual representation of its IP address. See
     *            {@link InetAddress.getByName}.
     * @param rpcPort
     *            The RPC port to connect to.
     * @param streamPort
     *            The stream port to connect to.
     * 
     * @return A connection to the kRPC server.
     * @throws IOException
     */
    public static Connection newInstance(String name, String address, int rpcPort, int streamPort) throws IOException {
        return new Connection(name, InetAddress.getByName(address), rpcPort, streamPort);
    }

    private Connection(String name, InetAddress address, int rpcPort, int streamPort) throws IOException {
        rpcSocket = new Socket(address, rpcPort);
        rpcSocket.getOutputStream().write(Encoder.RPC_HELLO_MESSAGE);
        rpcSocket.getOutputStream().write(Encoder.encodeClientName(name));
        rpcSocket.getOutputStream().flush();
        byte[] clientIdentifier = new byte[Encoder.CLIENT_IDENTIFIER_LENGTH];
        int read = 0;
        while (read < Encoder.CLIENT_IDENTIFIER_LENGTH)
            read += rpcSocket.getInputStream().read(clientIdentifier, read, Encoder.CLIENT_IDENTIFIER_LENGTH - read);

        Socket streamSocket = new Socket(address, streamPort);
        streamSocket.getOutputStream().write(Encoder.STREAM_HELLO_MESSAGE);
        streamSocket.getOutputStream().write(clientIdentifier);
        streamSocket.getOutputStream().flush();
        byte[] okMessage = new byte[Encoder.OK_MESSAGE.length];
        read = 0;
        while (read < Encoder.OK_MESSAGE.length)
            read += streamSocket.getInputStream().read(okMessage, read, Encoder.OK_MESSAGE.length - read);
        assert (Arrays.equals(okMessage, Encoder.OK_MESSAGE));

        rpcOutputStream = CodedOutputStream.newInstance(rpcSocket.getOutputStream());
        rpcInputStream = CodedInputStream.newInstance(rpcSocket.getInputStream());
        streamManager = new StreamManager(this, streamSocket);
    }

    /**
     * Close the connection.
     * 
     * @throws IOException
     */
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
    public ByteString invoke(String service, String procedure, ByteString... arguments) throws RPCException, IOException {
        return invoke(request(service, procedure, arguments));
    }

    ByteString invoke(KRPC.Request request) throws RPCException, IOException {
        byte[] data;
        synchronized (connectionLock) {
            rpcOutputStream.writeMessageNoTag(request);
            rpcOutputStream.flush();
            int size = rpcInputStream.readRawVarint32();
            data = rpcInputStream.readRawBytes(size);
        }
        KRPC.Response response = KRPC.Response.parseFrom(data);
        if (response.getHasError())
            throw new RPCException(response.getError());
        return response.getHasReturnValue() ? response.getReturnValue() : null;
    }

    KRPC.Request request(String service, String procedure, ByteString... arguments) throws IOException {
        KRPC.Request.Builder requestBuilder = KRPC.Request.newBuilder();
        requestBuilder.setService(service);
        requestBuilder.setProcedure(procedure);
        if (arguments.length > 0) {
            KRPC.Argument.Builder argumentBuilder = KRPC.Argument.newBuilder();
            int position = 0;
            for (ByteString value : arguments) {
                KRPC.Argument argument = argumentBuilder.setPosition(position).setValue(value).build();
                requestBuilder.addArguments(argument);
                position++;
            }
        }
        return requestBuilder.build();
    }

    private KRPC.Request request(Method method, Object... args) throws IOException {
        RPCInfo info = method.getAnnotation(RPCInfo.class);
        String service = info.service();
        String procedure = info.procedure();
        ByteString[] encodedArgs = new ByteString[args.length];
        int position = 0;
        for (Object arg : args) {
            encodedArgs[position] = Encoder.encode(arg);
            position++;
        }
        return request(service, procedure, encodedArgs);
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
     * @throws StreamException
     * @throws IOException
     */
    public <T> Stream<T> addStream(Class<?> clazz, String method, Object... args) throws StreamException, RPCException, IOException {
        return internalAddStream(clazz, null, method, args);
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
     * @throws StreamException
     * @throws IOException
     */
    public <T> Stream<T> addStream(RemoteObject instance, String method, Object... args) throws StreamException, RPCException, IOException {
        return internalAddStream(instance.getClass(), instance, method, args);
    }

    private <T> Stream<T> internalAddStream(Class<?> clazz, Object instance, String methodName, Object... args) throws StreamException, RPCException, IOException {
        Method[] methods = clazz.getMethods();
        for (Method method : methods) {
            if (method.getName() == methodName) {
                Class<?>[] paramTypes = method.getParameterTypes();
                if (args.length != paramTypes.length)
                    continue;
                for (int i = 0; i < args.length; i++)
                    if (!paramTypes[i].isAssignableFrom(args.getClass()))
                        continue;
                return internalAddStream(method, instance, args);
            }
        }
        String params = "";
        for (int i = 0; i < args.length; i++) {
            if (i > 0)
                params += ",";
            params += args[i].getClass().toString();
        }
        throw new StreamException("Failed to add stream. Method " + clazz.getName() + "." + methodName + "(" + params + ") not found.");
    }

    private <T> Stream<T> internalAddStream(Method method, Object instance, Object... args) throws StreamException, RPCException, IOException {
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
        KRPC.Request request = request(method, args);
        RPCInfo info = method.getAnnotation(RPCInfo.class);
        if (info == null)
            throw new StreamException("Failed to add stream. Method is not an RPC.");
        TypeSpecification returnTypeSpec;
        try {
            Method getReturnTypeSpec = info.returnTypeSpec().getMethod("get", String.class);
            returnTypeSpec = (TypeSpecification) getReturnTypeSpec.invoke(null, info.procedure());
        } catch (NoSuchMethodException e) {
            throw new StreamException("Failed to add stream. NoSuchMethodException when getting return type spec.");
        } catch (IllegalAccessException e) {
            throw new StreamException("Failed to add stream. IllegalAccessException when getting return type spec.");
        } catch (InvocationTargetException e) {
            throw new StreamException("Failed to add stream. InvocationTargetException when getting return type spec.");
        }
        return streamManager.add(request, returnTypeSpec);
    }
}
