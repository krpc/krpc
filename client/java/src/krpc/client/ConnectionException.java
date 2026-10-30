package krpc.client;

import java.io.IOException;
import krpc.schema.KRPC;

/** Thrown when failing to connect to the server, or when the connection fails or ends. */
public class ConnectionException extends IOException {
  private static final long serialVersionUID = 16427533217488326L;

  private final KRPC.ConnectionResponse.Status status;
  private final KRPC.Disconnect.Reason reason;

  /** Creates a new ConnectionException with the given message. */
  public ConnectionException(String message) {
    this(message, null, null);
  }

  /** Creates a new ConnectionException with the given message and cause. */
  public ConnectionException(String message, Exception innerException) {
    super(message, innerException);
    this.status = null;
    this.reason = null;
  }

  /** Creates a new ConnectionException for a refused or ended connection. */
  ConnectionException(
      String message, KRPC.ConnectionResponse.Status status, KRPC.Disconnect.Reason reason) {
    super(message);
    this.status = status;
    this.reason = reason;
  }

  /**
   * The status of the server's response when it refused the connection, such as DENIED, or
   * null.
   */
  public KRPC.ConnectionResponse.Status getStatus() {
    return status;
  }

  /**
   * The reason the server gave when it ended the connection, such as SERVER_STOPPED, or null.
   */
  public KRPC.Disconnect.Reason getReason() {
    return reason;
  }
}
