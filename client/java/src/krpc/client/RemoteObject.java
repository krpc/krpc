package krpc.client;

import java.io.Serializable;

/**
 * Abstract base class for objects that can be serialized and sent via kRPC
 * remote procedure calls.
 */
public abstract class RemoteObject implements Serializable, Comparable<RemoteObject> {

  private static final long serialVersionUID = 3164247842142774386L;

  @SuppressWarnings("checkstyle:membername")
  protected final Connection _connection;
  @SuppressWarnings("checkstyle:membername")
  final long _id;

  protected RemoteObject(Connection connection, long id) {
    this._connection = connection;
    this._id = id;
  }

  @Override
    public int hashCode() {
    return (int) this._id;
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
    return this._id == other._id;
  }

  @Override
    public int compareTo(final RemoteObject obj) {
    return Long.valueOf(this._id).compareTo(Long.valueOf(obj._id));
  }

}
