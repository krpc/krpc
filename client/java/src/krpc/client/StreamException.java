package krpc.client;

/** Thrown for a stream or event that has been removed, or that cannot be added. */
public class StreamException extends RemoteException {
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
