package krpc.client;

import com.google.protobuf.ByteString;
import com.google.protobuf.CodedInputStream;
import com.google.protobuf.CodedOutputStream;
import com.google.protobuf.Message;

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

import java.io.IOException;
import java.io.UnsupportedEncodingException;
import java.lang.reflect.Constructor;
import java.lang.reflect.InvocationTargetException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

// TODO: remove all the ByteString.copyFrom calls

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
    try {
      switch (type.getCode()) {
        case DOUBLE:
          return encodeDouble((double)value);
        case FLOAT:
          return encodeFloat((float)value);
        case SINT32:
          return encodeSInt32((int)value);
        case SINT64:
          return encodeSInt64((long)value);
        case UINT32:
          return encodeUInt32((int)value);
        case UINT64:
          return encodeUInt64((long)value);
        case BOOL:
          return encodeBoolean((boolean)value);
        case STRING:
          return encodeString((String)value);
        case BYTES:
          return encodeBytes((byte[])value);
        case CLASS:
          if (value == null) {
            return encodeUInt64(0);
          } else {
            return encodeObject((RemoteObject) value);
          }
        case ENUMERATION:
          return encodeEnum((RemoteEnum) value);
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
          return decodeSInt32(data);
        case SINT64:
          return decodeSInt64(data);
        case UINT32:
          return decodeUInt32(data);
        case UINT64:
          return decodeUInt64(data);
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
        case STATUS:
          return decodeMessage(KRPC.Status.newBuilder(), data);
        default:
          throw new EncodingException("Failed to decode value");
      }
    } catch (IOException exn) {
      throw new EncodingException("Failed to encode value", exn);
    }
  }

  static ByteString encodeDouble(double value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeDoubleSizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeDoubleNoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return ByteString.copyFrom(data);
  }

  static ByteString encodeFloat(float value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeFloatSizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeFloatNoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return ByteString.copyFrom(data);
  }

  static ByteString encodeSInt32(int value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeSInt32SizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeSInt32NoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return ByteString.copyFrom(data);
  }

  static ByteString encodeSInt64(long value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeSInt64SizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeSInt64NoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return ByteString.copyFrom(data);
  }

  static ByteString encodeUInt32(int value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeUInt32SizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeUInt32NoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return ByteString.copyFrom(data);
  }

  static ByteString encodeUInt64(long value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeUInt64SizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeUInt64NoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return ByteString.copyFrom(data);
  }

  static ByteString encodeBoolean(boolean value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeBoolSizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeBoolNoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return ByteString.copyFrom(data);
  }

  static ByteString encodeString(String value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeStringSizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeStringNoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return ByteString.copyFrom(data);
  }

  static ByteString encodeBytes(byte[] value) throws IOException {
    byte[] data = new byte[CodedOutputStream.computeByteArraySizeNoTag(value)];
    CodedOutputStream stream = CodedOutputStream.newInstance(data);
    stream.writeByteArrayNoTag(value);
    stream.flush();
    stream.checkNoSpaceLeft();
    return ByteString.copyFrom(data);
  }

  static ByteString encodeObject(RemoteObject value) throws IOException {
    return encodeUInt64(value.id);
  }

  static ByteString encodeEnum(RemoteEnum value) throws IOException {
    return encodeSInt32(value.getValue());
  }

  static ByteString encodeTuple(Tuple value, List<KRPC.Type> valueTypes) throws IOException {
    if (value.getSize() != valueTypes.size()) {
      throw new EncodingException("Failed to encode tuple");
    }
    KRPC.Tuple.Builder tuple = KRPC.Tuple.newBuilder();
    for (int i = 0; i < value.getSize(); i++) {
      tuple.addItems(encode(value.getValue(i), valueTypes.get(i)));
    }
    return ByteString.copyFrom(tuple.build().toByteArray());
  }

  static ByteString encodeList(List<?> value, KRPC.Type valueType) throws IOException {
    KRPC.List.Builder list = KRPC.List.newBuilder();
    for (Object item : value) {
      list.addItems(encode(item, valueType));
    }
    return ByteString.copyFrom(list.build().toByteArray());
  }

  static ByteString encodeSet(Set<?> value, KRPC.Type valueType) throws IOException {
    KRPC.Set.Builder set = KRPC.Set.newBuilder();
    for (Object item : value) {
      set.addItems(encode(item, valueType));
    }
    return ByteString.copyFrom(set.build().toByteArray());
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
    return ByteString.copyFrom(dictionary.build().toByteArray());
  }

  static ByteString encodeMessage(Message value) throws IOException {
    return ByteString.copyFrom(value.toByteArray());
  }

  static double decodeDouble(ByteString data) throws IOException {
    CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
    return stream.readDouble();
  }

  static float decodeFloat(ByteString data) throws IOException {
    CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
    return stream.readFloat();
  }

  static int decodeSInt32(ByteString data) throws IOException {
    CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
    return stream.readSInt32();
  }

  static long decodeSInt64(ByteString data) throws IOException {
    CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
    return stream.readSInt64();
  }

  static int decodeUInt32(ByteString data) throws IOException {
    CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
    return stream.readUInt32();
  }

  static long decodeUInt64(ByteString data) throws IOException {
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

  @SuppressWarnings({ "unchecked" })
  static <T> T decodeObject(ByteString data, KRPC.Type type, Connection connection)
      throws IOException {
    try {
      long id = decodeUInt64(data);
      Class<?> classType = Class.forName(
          "krpc.client.services." + type.getService() + "$" + type.getName());
      Constructor<?> ctor = classType.getConstructor(Connection.class, long.class);
      return id == 0 ? null : (T) ctor.newInstance(connection, id);
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

  @SuppressWarnings({ "unchecked" })
  static <T> T decodeEnum(ByteString data, KRPC.Type type) {
    try {
      int value = decodeSInt32(data);
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

  @SuppressWarnings({ "unchecked" })
  static <T> List<T> decodeList(ByteString data, KRPC.Type valueType, Connection connection)
      throws IOException {
    KRPC.List listMessage = KRPC.List.newBuilder().mergeFrom(data).build();
    List<T> list = new ArrayList<T>(listMessage.getItemsCount());
    for (ByteString item : listMessage.getItemsList()) {
      list.add((T) decode(item, valueType, connection));
    }
    return list;
  }

  @SuppressWarnings({ "unchecked" })
  static <T> Set<T> decodeSet(ByteString data, KRPC.Type valueType, Connection connection)
      throws IOException {
    KRPC.Set setMessage = KRPC.Set.newBuilder().mergeFrom(data).build();
    Set<T> set = new HashSet<T>(setMessage.getItemsCount());
    for (ByteString item : setMessage.getItemsList()) {
      set.add((T) decode(item, valueType, connection));
    }
    return set;
  }

  @SuppressWarnings({ "unchecked" })
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
