package krpc.client;

/**
 * Thrown for an error the server reports, through a call, a stream or an event, that has no Java
 * exception type. The exception classes of the services extend it.
 */
public class ServerException extends RemoteException {
  private static final long serialVersionUID = -7044149458204235614L;

  /** Creates a new ServerException with the given message. */
  public ServerException(String message) {
    super(message);
  }

  /** Creates a new ServerException with the given message and cause. */
  public ServerException(String message, Exception innerException) {
    super(message, innerException);
  }
}
