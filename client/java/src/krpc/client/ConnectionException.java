package krpc.client;

import java.io.IOException;

/** Thrown when failing to connect to the server. */
public class ConnectionException extends IOException {
  private static final long serialVersionUID = 16427533217488326L;

  public ConnectionException(String message) {
    super(message);
  }

  public ConnectionException(String message, Exception innerException) {
    super(message, innerException);
  }
}
