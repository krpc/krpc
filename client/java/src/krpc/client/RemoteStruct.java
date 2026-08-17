package krpc.client;

/**
 * A structure defined by a service: a compound value whose fields are sent inline, rather
 * than a reference to an object that stays on the server.
 *
 * <p>A generated structure also has the static methods {@code fieldTypes}, giving the types
 * of its fields in the order they are encoded in, and {@code fromFieldValues}, building one from
 * their values.
 */
public interface RemoteStruct {
  /** Returns the values of the fields, in the order they are encoded in. */
  public Object[] fieldValues();
}
