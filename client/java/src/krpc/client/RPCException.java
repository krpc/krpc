package krpc.client;

/** An error caused when a kRPC remote procedure call fails. */
@SuppressWarnings("checkstyle:abbreviationaswordinname")
public class RPCException extends Exception {
  private static final long serialVersionUID = -7044149458204235614L;

  /** Creates a new RPCException with the given message. */
  public RPCException(String message) {
    super(message);
  }

  /** Creates a new RPCException with the given message and cause. */
  public RPCException(String message, Exception innerException) {
    super(message, innerException);
  }
}
