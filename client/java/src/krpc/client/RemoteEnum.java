package krpc.client;

/**
 * An enumeration that can be serialized and sent via kRPC remote procedure
 * calls.
 */
public interface RemoteEnum {
  /** Returns the integer value of this enumeration member. */
  public int getValue();
}
