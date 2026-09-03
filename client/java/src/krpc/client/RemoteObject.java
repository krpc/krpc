package krpc.client;

import java.io.Serializable;

/**
 * Abstract base class for objects that can be serialized and sent via kRPC
 * remote procedure calls.
 */
public abstract class RemoteObject implements Serializable, Comparable<RemoteObject> {
  private static final long serialVersionUID = 3164247842142774386L;

  protected final transient Connection connection;
  /**
   * The identifier used to reference the object over the communication
   * protocol, for example to pass to KRPC.Expression.constantObject.
   */
  public final long id;

  /** Creates a new RemoteObject wrapping the given id. */
  protected RemoteObject(Connection connection, long id) {
    this.connection = connection;
    this.id = id;
  }

  @Override
    public int hashCode() {
    return (int) this.id;
  }

  @Override
    public boolean equals(final Object obj) {
    if (this == obj) {
      return true;
    }
    if (obj == null) {
      return false;
    }
    if (getClass() != obj.getClass()) {
      return false;
    }
    final RemoteObject other = (RemoteObject) obj;
    return this.id == other.id;
  }

  @Override
    public int compareTo(final RemoteObject obj) {
    return Long.valueOf(this.id).compareTo(Long.valueOf(obj.id));
  }
}
