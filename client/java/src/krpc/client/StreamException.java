package krpc.client;

/** Thrown when a stream operation encouters an error. */
public class StreamException extends Exception {
  private static final long serialVersionUID = 54119455204332164L;

  /** Creates a new StreamException with the given message. */
  public StreamException(String message) {
    super(message);
  }

  /** Creates a new StreamException with the given message and cause. */
  public StreamException(String message, Exception innerException) {
    super(message, innerException);
  }
}
