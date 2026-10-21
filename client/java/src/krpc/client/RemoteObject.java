package krpc.client;

import java.io.Serializable;
import java.util.Objects;

/**
 * Abstract base class for objects that can be serialized and sent via kRPC
 * remote procedure calls.
 */
public abstract class RemoteObject implements Serializable, Comparable<RemoteObject> {
  private static final long serialVersionUID = 3164247842142774386L;

  protected final transient Connection connection;
  private final long id;

  /** Creates a new RemoteObject wrapping the given id. */
  protected RemoteObject(Connection connection, long id) {
    this.connection = connection;
    this.id = id;
  }

  /**
   * The identifier of a remote object on the server. Throws an exception if the object
   * is null.
   *
   * @param obj The remote object.
   * @return The identifier, e.g. to pass to KRPC.Expression.constantObject.
   */
  public static long getObjectId(RemoteObject obj) {
    return Objects.requireNonNull(obj, "obj").id;
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
