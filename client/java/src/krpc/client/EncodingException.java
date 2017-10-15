package krpc.client;

/** Thrown when failing to encode or decode a message. */
public class EncodingException extends RuntimeException {
  private static final long serialVersionUID = 16427533217488326L;

  public EncodingException(String message) {
    super(message);
  }

  public EncodingException(String message, Exception innerException) {
    super(message, innerException);
  }
}
