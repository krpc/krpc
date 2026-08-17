package krpc.client;

import com.google.protobuf.ByteString;
import com.google.protobuf.CodedInputStream;
import com.google.protobuf.CodedOutputStream;
import com.google.protobuf.Message;
import com.google.protobuf.UnsafeByteOperations;
import java.io.IOException;
import java.io.UnsupportedEncodingException;
import java.lang.reflect.Constructor;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;
import krpc.client.EncodingException;
import krpc.schema.KRPC;
import org.javatuples.Decade;
import org.javatuples.Ennead;
import org.javatuples.Octet;
import org.javatuples.Pair;
import org.javatuples.Quartet;
import org.javatuples.Quintet;
import org.javatuples.Septet;
import org.javatuples.Sextet;
import org.javatuples.Triplet;
import org.javatuples.Tuple;
import org.javatuples.Unit;

/** Encodes and decodes values for kRPC procedure calls. */
public class Encoder {
  static String guidToString(byte[] guid) {
    StringBuilder builder = new StringBuilder();
    for (int i = 3; i >= 0; i--) {
      builder.append(String.format("%02x", guid[i]));
    }
    builder.append("-");
    for (int i = 5; i >= 4; i--) {
      builder.append(String.format("%02x", guid[i]));
    }
    builder.append("-");
    for (int i = 7; i >= 6; i--) {
      builder.append(String.format("%02x", guid[i]));
    }
    builder.append("-");
    for (int i = 8; i <= 9; i++) {
      builder.append(String.format("%02x", guid[i]));
    }
    builder.append("-");
    for (int i = 10; i <= 15; i++) {
      builder.append(String.format("%02x", guid[i]));
    }
    return builder.toString();
  }

  /** Encode an object. */
  public static ByteString encode(Object value, KRPC.Type type) {
    // A null value is signaled out-of-band by is_null; return null to indicate this.
    if (value == null) {
      return null;
    }
    try {
      switch (type.getCode()) {
        case DOUBLE:
          return encodeDouble((double) value);
        case FLOAT:
          return encodeFloat((float) value);
        case SINT32:
          return encodeSint32((int) value);
        case SINT64:
          return encodeSint64((long) value);
        case UINT32:
          return encodeUint32((int) value);
        case UINT64:
          return encodeUint64((long) value);
        case BOOL:
          return encodeBoolean((boolean) value);
        case STRING:
          return encodeString((String) value);
        case BYTES:
          return encodeBytes((byte[]) value);
        case CLASS:
          return encodeObject((RemoteObject) value);
        case ENUMERATION:
          return encodeEnum((RemoteEnum) value);
        case STRUCT:
          return encodeStruct((RemoteStruct) value, type);
        case TUPLE:
          return encodeTuple((Tuple) value, type.getTypesList());
        case LIST:
          return encodeList((List<?>) value, type.getTypes(0));
        case SET:
          return encodeSet((Set<?>) value, type.getTypes(0));
        case DICTIONARY:
          return encodeDictionary((Map<?, ?>) value, type.getTypes(0), type.getTypes(1));
        case PROCEDURE_CALL:
        case SERVICES:
        case STREAM:
        case STATUS:
          return encodeMessage((Message) value);
        default:
          throw new EncodingException("Failed to encode value");
      }
    } catch (IOException exn) {
      throw new EncodingException("Failed to encode value", exn);
    }
  }

  /** Decode an object. */
  public static Object decode(ByteString data, KRPC.Type type, Connection connection) {
    try {
      switch (type.getCode()) {
        case DOUBLE:
          return decodeDouble(data);
        case FLOAT:
          return decodeFloat(data);
        case SINT32:
          return decodeSint32(data);
        case SINT64:
          return decodeSint64(data);
        case UINT32:
          return decodeUint32(data);
        case UINT64:
          return decodeUint64(data);
        case BOOL:
          return decodeBoolean(data);
        case STRING:
          return decodeString(data);
        case BYTES:
          return decodeBytes(data);
        case CLASS:
          return decodeObject(data, type, connection);
        case ENUMERATION:
          return decodeEnum(data, type);
        case STRUCT:
          return decodeStruct(data, type, connection);
        case TUPLE:
          return decodeTuple(data, type.getTypesList(), connection);
        case LIST:
          return decodeList(data, type.getTypes(0), connection);
        case SET:
          return decodeSet(data, type.getTypes(0), connection);
        case DICTIONARY:
          return decodeDictionary(data, type.getTypes(0), type.getTypes(1), connection);
        case PROCEDURE_CALL:
          return decodeMessage(KRPC.ProcedureCall.newBuilder(), data);
        case SERVICES:
          return decodeMessage(KRPC.Services.newBuilder(), data);
        case STREAM:
          return decodeMessage(KRPC.Stream.newBuilder(), data);
        case EVENT:
          return new Event(connection, KRPC.Event.newBuilder().mergeFrom(data).build());
        case STATUS:
          return decodeMessage(KRPC.Status.newBuilder(), data);
        default:
          throw new EncodingException("Failed to decode value");
      }
    } catch (IOException exn) {
      throw new EncodingException("Failed to decode value", exn);
    }
  }

  static ByteString encodeDouble(double value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeDoubleSizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeDoubleNoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return UnsafeByteOperations.unsafeWrap(data);
  }

  static ByteString encodeFloat(float value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeFloatSizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeFloatNoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return UnsafeByteOperations.unsafeWrap(data);
  }

  static ByteString encodeSint32(int value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeSInt32SizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeSInt32NoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return UnsafeByteOperations.unsafeWrap(data);
  }

  static ByteString encodeSint64(long value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeSInt64SizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeSInt64NoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return UnsafeByteOperations.unsafeWrap(data);
  }

  static ByteString encodeUint32(int value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeUInt32SizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeUInt32NoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return UnsafeByteOperations.unsafeWrap(data);
  }

  static ByteString encodeUint64(long value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeUInt64SizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeUInt64NoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return UnsafeByteOperations.unsafeWrap(data);
  }

  static ByteString encodeBoolean(boolean value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeBoolSizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeBoolNoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return UnsafeByteOperations.unsafeWrap(data);
  }

  static ByteString encodeString(String value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeStringSizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeStringNoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return UnsafeByteOperations.unsafeWrap(data);
  }

  static ByteString encodeBytes(byte[] value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeByteArraySizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeByteArrayNoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return UnsafeByteOperations.unsafeWrap(data);
  }

  static ByteString encodeObject(RemoteObject value) throws IOException {
    return encodeUint64(value.id);
  }

  static ByteString encodeEnum(RemoteEnum value) throws IOException {
    return encodeSint32(value.getValue());
  }

  /**
   * What encoding a structure needs from the generated class that defines it: the types of its
   * fields, in the order they are encoded in, and the method that builds one from their values.
   * A struct type carries only the service and the name of the structure, so this is found by
   * reflection, once per structure rather than for every value encoded or decoded.
   */
  private static final class StructInfo {
    final List<KRPC.Type> fieldTypes;
    final Method fromFieldValues;

    @SuppressWarnings("unchecked")
    StructInfo(KRPC.Type type) {
      try {
        Class<?> structType = Class.forName(
            "krpc.client.services." + type.getService() + "$" + type.getName());
        fieldTypes = (List<KRPC.Type>) structType.getMethod("fieldTypes").invoke(null);
        fromFieldValues = structType.getMethod("fromFieldValues", Object[].class);
      } catch (ClassNotFoundException | NoSuchMethodException | IllegalAccessException
          | InvocationTargetException exn) {
        throw new EncodingException("Failed to find the fields of a struct", exn);
      }
    }
  }

  private static final ConcurrentMap<String, StructInfo> STRUCT_INFOS =
      new ConcurrentHashMap<>();

  static StructInfo structInfo(KRPC.Type type) {
    return STRUCT_INFOS.computeIfAbsent(
        type.getService() + "$" + type.getName(), name -> new StructInfo(type));
  }

  /**
   * A structure is encoded as the values of its fields in order, which is the same encoding
   * as a tuple of those values.
   */
  static ByteString encodeStruct(RemoteStruct value, KRPC.Type type) throws IOException {
    List<KRPC.Type> fieldTypes = structInfo(type).fieldTypes;
    Object[] values = value.fieldValues();
    if (values.length != fieldTypes.size()) {
      throw new EncodingException("Failed to encode struct");
    }
    KRPC.Tuple.Builder struct = KRPC.Tuple.newBuilder();
    for (int i = 0; i < values.length; i++) {
      struct.addItems(encode(values[i], fieldTypes.get(i)));
    }
    return UnsafeByteOperations.unsafeWrap(struct.build().toByteArray());
  }

  /**
   * Fields are only ever appended to a structure, so a value from a newer server may carry
   * more items than this client knows about, and those are ignored.
   */
  @SuppressWarnings("unchecked")
  static <T> T decodeStruct(ByteString data, KRPC.Type type, Connection connection)
      throws IOException {
    StructInfo info = structInfo(type);
    KRPC.Tuple structMessage = KRPC.Tuple.newBuilder().mergeFrom(data).build();
    if (structMessage.getItemsCount() < info.fieldTypes.size()) {
      throw new EncodingException("Failed to decode struct");
    }
    Object[] values = new Object[info.fieldTypes.size()];
    for (int i = 0; i < values.length; i++) {
      values[i] = decode(structMessage.getItems(i), info.fieldTypes.get(i), connection);
    }
    try {
      return (T) info.fromFieldValues.invoke(null, (Object) values);
    } catch (IllegalAccessException | InvocationTargetException exn) {
      throw new EncodingException("Failed to decode struct", exn);
    }
  }

  static ByteString encodeTuple(Tuple value, List<KRPC.Type> valueTypes) throws IOException {
    if (value.getSize() != valueTypes.size()) {
      throw new EncodingException("Failed to encode tuple");
    }
    KRPC.Tuple.Builder tuple = KRPC.Tuple.newBuilder();
    for (int i = 0; i < value.getSize(); i++) {
      tuple.addItems(encode(value.getValue(i), valueTypes.get(i)));
    }
    return UnsafeByteOperations.unsafeWrap(tuple.build().toByteArray());
  }

  static ByteString encodeList(List<?> value, KRPC.Type valueType) throws IOException {
    KRPC.List.Builder list = KRPC.List.newBuilder();
    for (Object item : value) {
      list.addItems(encode(item, valueType));
    }
    return UnsafeByteOperations.unsafeWrap(list.build().toByteArray());
  }

  static ByteString encodeSet(Set<?> value, KRPC.Type valueType) throws IOException {
    KRPC.Set.Builder set = KRPC.Set.newBuilder();
    for (Object item : value) {
      set.addItems(encode(item, valueType));
    }
    return UnsafeByteOperations.unsafeWrap(set.build().toByteArray());
  }

  static ByteString encodeDictionary(Map<?, ?> value, KRPC.Type keyType, KRPC.Type valueType)
      throws IOException {
    KRPC.Dictionary.Builder dictionary = KRPC.Dictionary.newBuilder();
    KRPC.DictionaryEntry.Builder dictionaryEntry = KRPC.DictionaryEntry.newBuilder();
    for (Map.Entry<?, ?> entry : value.entrySet()) {
      dictionaryEntry.setKey(encode(entry.getKey(), keyType));
      dictionaryEntry.setValue(encode(entry.getValue(), valueType));
      dictionary.addEntries(dictionaryEntry.build());
    }
    return UnsafeByteOperations.unsafeWrap(dictionary.build().toByteArray());
  }

  static ByteString encodeMessage(Message value) throws IOException {
    return UnsafeByteOperations.unsafeWrap(value.toByteArray());
  }

  static double decodeDouble(ByteString data) throws IOException {
    CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
    return stream.readDouble();
  }

  static float decodeFloat(ByteString data) throws IOException {
    CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
    return stream.readFloat();
  }

  static int decodeSint32(ByteString data) throws IOException {
    CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
    return stream.readSInt32();
  }

  static long decodeSint64(ByteString data) throws IOException {
    CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
    return stream.readSInt64();
  }

  static int decodeUint32(ByteString data) throws IOException {
    CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
    return stream.readUInt32();
  }

  static long decodeUint64(ByteString data) throws IOException {
    CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
    return stream.readUInt64();
  }

  static boolean decodeBoolean(ByteString data) throws IOException {
    CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
    return stream.readBool();
  }

  static String decodeString(ByteString data) throws IOException {
    CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
    return stream.readString();
  }

  static byte[] decodeBytes(ByteString data) throws IOException {
    CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
    return stream.readByteArray();
  }

  @SuppressWarnings("unchecked")
  static <T> T decodeObject(ByteString data, KRPC.Type type, Connection connection)
      throws IOException {
    try {
      long id = decodeUint64(data);
      Class<?> classType = Class.forName(
          "krpc.client.services." + type.getService() + "$" + type.getName());
      Constructor<?> ctor = classType.getConstructor(Connection.class, long.class);
      return (T) ctor.newInstance(connection, id);
    } catch (ClassNotFoundException exn) {
      throw new EncodingException("Failed to decode object", exn);
    } catch (NoSuchMethodException exn) {
      throw new EncodingException("Failed to decode object", exn);
    } catch (InstantiationException exn) {
      throw new EncodingException("Failed to decode object", exn);
    } catch (IllegalAccessException exn) {
      throw new EncodingException("Failed to decode object", exn);
    } catch (InvocationTargetException exn) {
      throw new EncodingException("Failed to decode object", exn);
    }
  }

  @SuppressWarnings("unchecked")
  static <T> T decodeEnum(ByteString data, KRPC.Type type) {
    try {
      int value = decodeSint32(data);
      Class<?> enumType = Class.forName(
          "krpc.client.services." + type.getService() + "$" + type.getName());
      return (T) enumType.getMethod("fromValue", int.class).invoke(null, value);
    } catch (IOException exn) {
      throw new EncodingException("Failed to decode object", exn);
    } catch (ClassNotFoundException exn) {
      throw new EncodingException("Failed to decode object", exn);
    } catch (NoSuchMethodException exn) {
      throw new EncodingException("Failed to decode object", exn);
    } catch (IllegalAccessException exn) {
      throw new EncodingException("Failed to decode object", exn);
    } catch (InvocationTargetException exn) {
      throw new EncodingException("Failed to decode object", exn);
    }
  }

  @SuppressWarnings({ "unchecked", "rawtypes" })
  static Tuple decodeTuple(ByteString data, List<KRPC.Type> valueTypes, Connection connection)
      throws IOException {
    KRPC.Tuple tupleMessage = KRPC.Tuple.newBuilder().mergeFrom(data).build();
    int numElements = tupleMessage.getItemsCount();
    Object[] es = new Object[numElements];
    for (int i = 0; i < numElements; i++) {
      es[i] = decode(tupleMessage.getItems(i), valueTypes.get(i), connection);
    }
    switch (numElements) {
      case 1:
        return new Unit(es[0]);
      case 2:
        return new Pair(es[0], es[1]);
      case 3:
        return new Triplet(es[0], es[1], es[2]);
      case 4:
        return new Quartet(es[0], es[1], es[2], es[3]);
      case 5:
        return new Quintet(es[0], es[1], es[2], es[3], es[4]);
      case 6:
        return new Sextet(es[0], es[1], es[2], es[3], es[4], es[5]);
      case 7:
        return new Septet(es[0], es[1], es[2], es[3], es[4], es[5], es[6]);
      case 8:
        return new Octet(es[0], es[1], es[2], es[3], es[4], es[5], es[6], es[7]);
      case 9:
        return new Ennead(es[0], es[1], es[2], es[3], es[4], es[5], es[6], es[7], es[8]);
      case 10:
        return new Decade(es[0], es[1], es[2], es[3], es[4], es[5], es[6], es[7], es[8], es[9]);
      default:
        throw new EncodingException("Failed to decode tuple");
    }
  }

  @SuppressWarnings("unchecked")
  static <T> List<T> decodeList(ByteString data, KRPC.Type valueType, Connection connection)
      throws IOException {
    KRPC.List listMessage = KRPC.List.newBuilder().mergeFrom(data).build();
    List<T> list = new ArrayList<T>(listMessage.getItemsCount());
    for (ByteString item : listMessage.getItemsList()) {
      list.add((T) decode(item, valueType, connection));
    }
    return list;
  }

  @SuppressWarnings("unchecked")
  static <T> Set<T> decodeSet(ByteString data, KRPC.Type valueType, Connection connection)
      throws IOException {
    KRPC.Set setMessage = KRPC.Set.newBuilder().mergeFrom(data).build();
    Set<T> set = new HashSet<T>(setMessage.getItemsCount());
    for (ByteString item : setMessage.getItemsList()) {
      set.add((T) decode(item, valueType, connection));
    }
    return set;
  }

  @SuppressWarnings("unchecked")
  static <K, V> Map<K, V> decodeDictionary(ByteString data, KRPC.Type keyType, KRPC.Type valueType,
                                           Connection connection) throws IOException {
    KRPC.Dictionary dictionaryMessage = KRPC.Dictionary.newBuilder().mergeFrom(data).build();
    Map<K, V> dictionary = new HashMap<K, V>(dictionaryMessage.getEntriesCount());
    for (KRPC.DictionaryEntry entry : dictionaryMessage.getEntriesList()) {
      K key = (K) decode(entry.getKey(), keyType, connection);
      V value = (V) decode(entry.getValue(), valueType, connection);
      dictionary.put(key, value);
    }
    return dictionary;
  }

  static Message decodeMessage(Message.Builder builder, ByteString data) throws IOException {
    builder.mergeFrom(data);
    return builder.build();
  }
}
