package krpc.client;

import com.google.protobuf.ByteString;
import com.google.protobuf.CodedInputStream;
import com.google.protobuf.Message;
import java.io.IOException;
import java.lang.reflect.Constructor;
import java.lang.reflect.InvocationTargetException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
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

/** Decodes values from kRPC procedure results. */
public class Decoder {
  /**
   * Decode one of the values a collection or a structure holds, which encodeItem wrote.
   */
  static Object decodeItem(ByteString data, KRPC.Type type, Connection connection) {
    if (!type.getNullable()) {
      return decode(data, type, connection);
    }
    if (data.isEmpty()) {
      throw new EncodingException("A nullable value carries no presence bool");
    }
    // The presence bool is one byte, as a bool value always is, and is read from the data
    // directly
    if (data.byteAt(0) == 0) {
      return null;
    }
    return decode(data.substring(1), type, connection);
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
          return decodeUint64(data);
        case EVENT:
          return new Event(connection, decodeUint64(data));
        default:
          throw new EncodingException("Failed to decode value");
      }
    } catch (IOException exn) {
      throw new EncodingException("Failed to decode value", exn);
    }
  }

  /**
   * Fields are only ever appended to a structure, so a value from a newer server may carry
   * more items than this client knows about, and those are ignored.
   */
  @SuppressWarnings("unchecked")
  static <T> T decodeStruct(ByteString data, KRPC.Type type, Connection connection)
      throws IOException {
    StructInfo info = StructInfo.of(type);
    KRPC.Tuple structMessage = KRPC.Tuple.newBuilder().mergeFrom(data).build();
    if (structMessage.getItemsCount() < info.fieldTypes.size()) {
      throw new EncodingException("Failed to decode struct");
    }
    Object[] values = new Object[info.fieldTypes.size()];
    for (int i = 0; i < values.length; i++) {
      values[i] = decodeItem(structMessage.getItems(i), info.fieldTypes.get(i), connection);
    }
    try {
      return (T) info.fromFieldValues.invoke(null, (Object) values);
    } catch (IllegalAccessException | InvocationTargetException exn) {
      throw new EncodingException("Failed to decode struct", exn);
    }
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
      es[i] = decodeItem(tupleMessage.getItems(i), valueTypes.get(i), connection);
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
      list.add((T) decodeItem(item, valueType, connection));
    }
    return list;
  }

  @SuppressWarnings("unchecked")
  static <T> Set<T> decodeSet(ByteString data, KRPC.Type valueType, Connection connection)
      throws IOException {
    KRPC.Set setMessage = KRPC.Set.newBuilder().mergeFrom(data).build();
    Set<T> set = new HashSet<T>(setMessage.getItemsCount());
    for (ByteString item : setMessage.getItemsList()) {
      set.add((T) decodeItem(item, valueType, connection));
    }
    return set;
  }

  @SuppressWarnings("unchecked")
  static <K, V> Map<K, V> decodeDictionary(ByteString data, KRPC.Type keyType, KRPC.Type valueType,
                                           Connection connection) throws IOException {
    KRPC.Dictionary dictionaryMessage = KRPC.Dictionary.newBuilder().mergeFrom(data).build();
    Map<K, V> dictionary = new HashMap<K, V>(dictionaryMessage.getEntriesCount());
    for (KRPC.DictionaryEntry entry : dictionaryMessage.getEntriesList()) {
      K key = (K) decodeItem(entry.getKey(), keyType, connection);
      V value = (V) decodeItem(entry.getValue(), valueType, connection);
      dictionary.put(key, value);
    }
    return dictionary;
  }

  static Message decodeMessage(Message.Builder builder, ByteString data) throws IOException {
    builder.mergeFrom(data);
    return builder.build();
  }
}
