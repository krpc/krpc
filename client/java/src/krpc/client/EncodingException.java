package krpc.client;

/** Thrown for a value of the right type that fails to encode, or a value that fails to decode. */
public class EncodingException extends RemoteException {
  private static final long serialVersionUID = 16427533217488326L;

  /** Creates a new EncodingException with the given message. */
  public EncodingException(String message) {
    super(message);
  }

  /** Creates a new EncodingException with the given message and cause. */
  public EncodingException(String message, Throwable innerException) {
    super(message, innerException);
  }
}
