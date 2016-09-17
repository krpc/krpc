package krpc.client;

import com.google.protobuf.ByteString;

import java.io.IOException;

class TestUtils {

  public static Connection connect() throws IOException {
    String envRpcPort = System.getenv("RPC_PORT");
    String envStreamPort = System.getenv("STREAM_PORT");
    int rpcPort = envRpcPort == null ? 50000 : Integer.parseInt(envRpcPort);
    int streamPort = envStreamPort == null ? 50001 : Integer.parseInt(envStreamPort);
    return Connection.newInstance("JavaClientTest", "localhost", rpcPort, streamPort);
  }

  public static String hexlify(byte[] data) {
    StringBuilder builder = new StringBuilder();
    for (byte b : data) {
      builder.append(String.format("%02x", b));
    }
    return builder.toString();
  }

  public static String hexlify(ByteString data) {
    return hexlify(data.toByteArray());
  }

  public static ByteString unhexlify(String data) {
    int length = data.length();
    byte[] result = new byte[length / 2];
    for (int i = 0; i < length; i += 2) {
      result[i / 2] = (byte) ((Character.digit(data.charAt(i), 16) << 4)
                              + Character.digit(data.charAt(i + 1), 16));
    }
    return ByteString.copyFrom(result);
  }

  public static String repeatedString(String string, int numRepeats) {
    return new String(new char[numRepeats]).replace("\0", string);
  }

}
