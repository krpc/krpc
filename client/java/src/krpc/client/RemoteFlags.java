package krpc.client;

import java.lang.reflect.Field;
import java.lang.reflect.Modifier;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.SortedMap;
import java.util.TreeMap;

/**
 * A value of a flags enumeration: a combination of the values the enumeration declares.
 *
 * @param <T> The class of the enumeration.
 */
public abstract class RemoteFlags<T extends RemoteFlags<T>> implements RemoteEnum {
  private final int value;

  /** Creates the value of the enumeration that holds the given number. */
  protected RemoteFlags(int value) {
    this.value = value;
  }

  /** The value of the enumeration that holds the given number. */
  protected abstract T withValue(int value);

  @Override
  public int getValue() {
    return value;
  }

  /** Returns the combination of this value and another. */
  public T or(T other) {
    return withValue(value | other.getValue());
  }

  /** Returns the flags this value shares with another. */
  public T and(T other) {
    return withValue(value & other.getValue());
  }

  /** Returns the flags held by only one of this value and another. */
  public T xor(T other) {
    return withValue(value ^ other.getValue());
  }

  /** Returns this value with the flags of another removed. */
  public T andNot(T other) {
    return withValue(value & ~other.getValue());
  }

  /** Returns whether this value holds every flag of another. */
  public boolean contains(T other) {
    return (value & other.getValue()) == other.getValue();
  }

  @Override
  public boolean equals(Object other) {
    return other != null
        && other.getClass() == getClass()
        && ((RemoteFlags<?>) other).getValue() == value;
  }

  @Override
  public int hashCode() {
    return value;
  }

  /**
   * Returns the name of a declared value, or the names of the single flags a combination
   * holds in increasing order, joined by " | ".
   */
  @Override
  public String toString() {
    SortedMap<Integer, String> declared = NAMES.get(getClass());
    String name = declared.get(value);
    if (name != null) {
      return name;
    }
    List<String> flags = new ArrayList<String>();
    for (Map.Entry<Integer, String> entry : declared.entrySet()) {
      int flag = entry.getKey();
      if (flag != 0 && (flag & (flag - 1)) == 0 && (value & flag) != 0) {
        flags.add(entry.getValue());
      }
    }
    return String.join(" | ", flags);
  }

  /** The name of each value a flags enumeration declares, keyed by its number. */
  private static final ClassValue<SortedMap<Integer, String>> NAMES =
      new ClassValue<SortedMap<Integer, String>>() {
        @Override
        protected SortedMap<Integer, String> computeValue(Class<?> type) {
          SortedMap<Integer, String> names = new TreeMap<Integer, String>();
          for (Field field : type.getFields()) {
            if (!Modifier.isStatic(field.getModifiers()) || field.getType() != type) {
              continue;
            }
            try {
              names.putIfAbsent(((RemoteFlags<?>) field.get(null)).getValue(), field.getName());
            } catch (IllegalAccessException exn) {
              continue;
            }
          }
          return names;
        }
      };
}
