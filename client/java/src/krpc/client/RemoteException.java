package krpc.client;

/**
 * The base of every exception the client throws of its own. A server exception with a mapped
 * type, such as ArgumentException, throws the Java exception it stands for.
 */
public class RemoteException extends RuntimeException {
  private static final long serialVersionUID = 3818266540914270713L;

  /** Creates a new RemoteException with the given message. */
  public RemoteException(String message) {
    super(message);
  }

  /** Creates a new RemoteException with the given message and cause. */
  public RemoteException(String message, Throwable cause) {
    super(message, cause);
  }
}
