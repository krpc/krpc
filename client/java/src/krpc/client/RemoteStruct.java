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

  /**
   * Compare the values two structures hold for the same field, which a generated structure
   * calls for each of its fields in turn. A field whose type has no ordering, such as a
   * collection, throws a {@link ClassCastException} unless the two values are the same
   * object, exactly as comparing two tuples holding such an item does, so two structures
   * that are equal can still throw here.
   */
  @SuppressWarnings({"unchecked", "rawtypes"})
  public static int compareFields(Object lhs, Object rhs) {
    if (lhs == rhs) {
      return 0;
    }
    if (lhs == null) {
      return -1;
    }
    if (rhs == null) {
      return 1;
    }
    return ((Comparable) lhs).compareTo(rhs);
  }
}
