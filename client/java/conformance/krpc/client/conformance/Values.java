package krpc.client.conformance;

import com.google.protobuf.ByteString;
import com.google.protobuf.Message;
import java.util.ArrayList;
import java.util.Collection;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.function.Function;
import java.util.function.IntFunction;
import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.RemoteEnum;
import krpc.client.RemoteObject;
import krpc.conformance.Testee.Entries;
import krpc.conformance.Testee.Entry;
import krpc.conformance.Testee.Items;
import krpc.conformance.Testee.Value;
import org.javatuples.Pair;
import org.javatuples.Quartet;
import org.javatuples.Triplet;
import org.javatuples.Unit;

/**
 * Conversions between conformance Values and the client's values, without the client's
 * encoder, and the objects and streams the testee holds by handle. Each object a call returns
 * gets a new handle, so two handles can refer to one server object.
 */
final class Values {
  private static final Map<Long, Object> HANDLES = new HashMap<>();
  private static long nextHandle = 1;
  private static Connection connection;

  private Values() {}

  static synchronized long addHandle(Object value) {
    long handle = nextHandle++;
    HANDLES.put(handle, value);
    return handle;
  }

  static synchronized void removeHandle(long handle) {
    HANDLES.remove(handle);
  }

  static synchronized Object getHandle(long handle) {
    return HANDLES.get(handle);
  }

  /** Sets the connection an object made from an id belongs to. */
  static synchronized void setConnection(Connection value) {
    connection = value;
  }

  static synchronized void clearHandles() {
    HANDLES.clear();
  }

  static double argDouble(Value value) {
    return notNull(value).getDouble();
  }

  static float argFloat(Value value) {
    return notNull(value).getFloat();
  }

  static int argSint32(Value value) {
    return notNull(value).getSint32();
  }

  static long argSint64(Value value) {
    return notNull(value).getSint64();
  }

  static int argUint32(Value value) {
    return notNull(value).getUint32();
  }

  static long argUint64(Value value) {
    return notNull(value).getUint64();
  }

  static boolean argBool(Value value) {
    return notNull(value).getBool();
  }

  /** The value, which must not be null, as the primitive type for it cannot hold null. */
  private static Value notNull(Value value) {
    if (isNull(value)) {
      throw new Testee.UnsupportedException("A primitive type cannot hold null");
    }
    return value;
  }

  static String argString(Value value) {
    return value.hasNull() ? null : value.getString();
  }

  static byte[] argBytes(Value value) {
    return value.hasNull() ? null : value.getBytes().toByteArray();
  }

  static UnsupportedOperationException wrongArgumentCount(String procedure, int count) {
    return new UnsupportedOperationException(
        "The testee cannot call " + procedure + " with " + count + " arguments");
  }

  static <T> T arg(Function<Value, T> convert, Value value) {
    return convert.apply(value);
  }

  static <T> Function<Value, T> argObject(Class<T> type) {
    return value -> {
      // A handle of 0 asks for an object of the class with the given id
      if (value.hasObject() && value.getObject().getHandle() == 0) {
        try {
          return type.getConstructor(Connection.class, long.class)
              .newInstance(connection, value.getObject().getId());
        } catch (ReflectiveOperationException exn) {
          throw new IllegalStateException(exn);
        }
      }
      return type.cast(argAny(value));
    };
  }

  static <T> Function<Value, T> argNullable(Function<Value, T> convert) {
    return value -> isNull(value) ? null : convert.apply(value);
  }

  static <T> Function<Value, T> argEnum(IntFunction<T> fromValue) {
    return value -> isNull(value) ? null : fromValue.apply(value.getEnum().getValue());
  }

  static <T> Function<Value, List<T>> argList(Function<Value, T> item) {
    return value -> {
      if (isNull(value)) {
        return null;
      }
      List<T> result = new ArrayList<>();
      for (Value x : value.getList().getItemsList()) {
        result.add(boxed(item, x));
      }
      return result;
    };
  }

  static <T> Function<Value, Set<T>> argSet(Function<Value, T> item) {
    return value -> {
      if (isNull(value)) {
        return null;
      }
      Set<T> result = new HashSet<>();
      for (Value x : value.getSet().getItemsList()) {
        result.add(boxed(item, x));
      }
      return result;
    };
  }

  static <K, V> Function<Value, Map<K, V>> argMap(
      Function<Value, K> key, Function<Value, V> item) {
    return value -> {
      if (isNull(value)) {
        return null;
      }
      Map<K, V> result = new HashMap<>();
      for (Entry x : value.getDictionary().getEntriesList()) {
        result.put(boxed(key, x.getKey()), boxed(item, x.getValue()));
      }
      return result;
    };
  }

  static <A> Function<Value, Unit<A>> argUnit(Function<Value, A> a) {
    return value -> isNull(value) ? null : new Unit<>(boxed(a, tupleItem(value, 0)));
  }

  static <A, B> Function<Value, Pair<A, B>> argPair(
      Function<Value, A> a, Function<Value, B> b) {
    return value -> {
      if (isNull(value)) {
        return null;
      }
      return new Pair<>(boxed(a, tupleItem(value, 0)), boxed(b, tupleItem(value, 1)));
    };
  }

  static <A, B, C> Function<Value, Triplet<A, B, C>> argTriplet(
      Function<Value, A> a, Function<Value, B> b, Function<Value, C> c) {
    return value -> {
      if (isNull(value)) {
        return null;
      }
      return new Triplet<>(boxed(a, tupleItem(value, 0)), boxed(b, tupleItem(value, 1)),
          boxed(c, tupleItem(value, 2)));
    };
  }

  static <A, B, C, D> Function<Value, Quartet<A, B, C, D>> argQuartet(Function<Value, A> a,
      Function<Value, B> b, Function<Value, C> c, Function<Value, D> d) {
    return value -> {
      if (isNull(value)) {
        return null;
      }
      return new Quartet<>(boxed(a, tupleItem(value, 0)), boxed(b, tupleItem(value, 1)),
          boxed(c, tupleItem(value, 2)), boxed(d, tupleItem(value, 3)));
    };
  }

  /** An item of a collection or tuple, which holds a boxed value, so it can hold null. */
  private static <T> T boxed(Function<Value, T> convert, Value value) {
    return isNull(value) ? null : convert.apply(value);
  }

  private static Value tupleItem(Value value, int index) {
    return value.getTuple().getItems(index);
  }

  private static boolean isNull(Value value) {
    return value.getValueCase() == Value.ValueCase.VALUE_NOT_SET
        || value.getValueCase() == Value.ValueCase.NULL;
  }

  /** The value of an object or a TestStruct, with an object as the object its handle refers to. */
  static Object argAny(Value value) {
    switch (value.getValueCase()) {
      case VALUE_NOT_SET:
      case NULL:
        return null;
      case OBJECT:
        return getHandle(value.getObject().getHandle());
      case ENUM:
        return argEnum(krpc.client.services.TestService.TestEnum::fromValue).apply(value);
      case STRUCT:
        return TestServiceDispatch.argTestServiceTestStruct(value);
      default:
        throw new UnsupportedOperationException("No conversion for " + value.getValueCase());
    }
  }

  static <T> Value result(Function<T, Value> convert, T value) {
    return convert.apply(value);
  }

  static <T> Function<T, Value> resultNullable(Function<T, Value> convert) {
    return x -> x == null ? resultNull() : convert.apply(x);
  }

  static Value resultEnum(RemoteEnum value) {
    if (value == null) {
      return resultNull();
    }
    return Value.newBuilder()
        .setEnum(krpc.conformance.Testee.Enum.newBuilder().setValue(value.getValue()))
        .build();
  }

  static <T> Function<List<T>, Value> resultList(Function<T, Value> item) {
    return x -> x == null ? resultNull() : Value.newBuilder().setList(items(x, item)).build();
  }

  static <T> Function<Set<T>, Value> resultSet(Function<T, Value> item) {
    return x -> x == null ? resultNull() : Value.newBuilder().setSet(items(x, item)).build();
  }

  static <K, V> Function<Map<K, V>, Value> resultMap(
      Function<K, Value> key, Function<V, Value> item) {
    return x -> {
      if (x == null) {
        return resultNull();
      }
      Entries.Builder entries = Entries.newBuilder();
      for (Map.Entry<K, V> entry : x.entrySet()) {
        entries.addEntries(Entry.newBuilder()
                               .setKey(key.apply(entry.getKey()))
                               .setValue(item.apply(entry.getValue())));
      }
      return Value.newBuilder().setDictionary(entries).build();
    };
  }

  static <A> Function<Unit<A>, Value> resultUnit(Function<A, Value> a) {
    return x -> x == null ? resultNull() : tuple(a.apply(x.getValue0()));
  }

  static <A, B> Function<Pair<A, B>, Value> resultPair(
      Function<A, Value> a, Function<B, Value> b) {
    return x -> x == null ? resultNull() : tuple(a.apply(x.getValue0()), b.apply(x.getValue1()));
  }

  static <A, B, C> Function<Triplet<A, B, C>, Value> resultTriplet(
      Function<A, Value> a, Function<B, Value> b, Function<C, Value> c) {
    return x -> {
      if (x == null) {
        return resultNull();
      }
      return tuple(a.apply(x.getValue0()), b.apply(x.getValue1()), c.apply(x.getValue2()));
    };
  }

  static <A, B, C, D> Function<Quartet<A, B, C, D>, Value> resultQuartet(Function<A, Value> a,
      Function<B, Value> b, Function<C, Value> c, Function<D, Value> d) {
    return x -> {
      if (x == null) {
        return resultNull();
      }
      return tuple(a.apply(x.getValue0()), b.apply(x.getValue1()), c.apply(x.getValue2()),
          d.apply(x.getValue3()));
    };
  }

  private static <T> Items items(Collection<T> values, Function<T, Value> item) {
    Items.Builder items = Items.newBuilder();
    for (T x : values) {
      items.addItems(item.apply(x));
    }
    return items.build();
  }

  private static Value tuple(Value... values) {
    Items.Builder items = Items.newBuilder();
    for (Value x : values) {
      items.addItems(x);
    }
    return Value.newBuilder().setTuple(items).build();
  }

  private static Value resultNull() {
    return Value.newBuilder().setNull(true).build();
  }

  static Value resultNone() {
    return Value.getDefaultInstance();
  }

  static Value resultDouble(double value) {
    return Value.newBuilder().setDouble(value).build();
  }

  static Value resultFloat(float value) {
    return Value.newBuilder().setFloat(value).build();
  }

  static Value resultSint32(int value) {
    return Value.newBuilder().setSint32(value).build();
  }

  static Value resultSint64(long value) {
    return Value.newBuilder().setSint64(value).build();
  }

  static Value resultUint32(int value) {
    return Value.newBuilder().setUint32(value).build();
  }

  static Value resultUint64(long value) {
    return Value.newBuilder().setUint64(value).build();
  }

  static Value resultBool(boolean value) {
    return Value.newBuilder().setBool(value).build();
  }

  static Value resultString(String value) {
    if (value == null) {
      return Value.newBuilder().setNull(true).build();
    }
    return Value.newBuilder().setString(value).build();
  }

  static Value resultBytes(byte[] value) {
    if (value == null) {
      return Value.newBuilder().setNull(true).build();
    }
    return Value.newBuilder().setBytes(ByteString.copyFrom(value)).build();
  }

  static Value resultEvent(Event value) {
    return Value.newBuilder().setEvent(addHandle(value)).build();
  }

  static Value resultMessage(Message value) {
    return Value.newBuilder().setMessage(value.toByteString()).build();
  }

  static Value resultObject(RemoteObject value) {
    if (value == null) {
      return Value.newBuilder().setNull(true).build();
    }
    return Value.newBuilder()
        .setObject(krpc.conformance.Testee.Object.newBuilder()
                       .setHandle(addHandle(value))
                       .setId(RemoteObject.getObjectId(value)))
        .build();
  }
}
