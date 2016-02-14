package krpc.client;

import java.io.IOException;
import java.net.InetAddress;
import java.net.Socket;
import java.util.Arrays;

import com.google.protobuf.ByteString;
import com.google.protobuf.CodedInputStream;
import com.google.protobuf.CodedOutputStream;

import krpc.schema.KRPC;

public class Connection {

    private Socket rpcSocket;
    private Socket streamSocket;

    private CodedOutputStream rpcOutputStream;
    private CodedInputStream rpcInputStream;

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

    /**
     * Close the connection.
     * 
     * @throws IOException
     */
    public void close() throws IOException {
        rpcSocket.close();
        streamSocket.close();
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

        streamSocket = new Socket(address, streamPort);
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
    }

    /**
     * Invoke a remote procedure call. Should not be called directly. This
     * interface is for generated service code.
     */
    public ByteString invoke(String service, String procedure, ByteString... arguments) throws RPCException, IOException {
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

        rpcOutputStream.writeMessageNoTag(requestBuilder.build());
        rpcOutputStream.flush();

        int size = rpcInputStream.readRawVarint32();
        byte[] data = rpcInputStream.readRawBytes(size);
        KRPC.Response response = KRPC.Response.parseFrom(data);
        if (response.getHasError())
            throw new RPCException(response.getError());
        return response.getHasReturnValue() ? response.getReturnValue() : null;
    }
}
