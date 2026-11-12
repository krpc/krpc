package krpc.client;

import com.google.protobuf.ByteString;
import com.google.protobuf.CodedOutputStream;
import com.google.protobuf.Message;
import com.google.protobuf.UnsafeByteOperations;
import java.io.IOException;
import java.util.List;
import java.util.Map;
import java.util.Set;
import krpc.schema.KRPC;
import org.javatuples.Tuple;

/** Encodes values for kRPC procedure calls. */
public class Encoder {
  // The presence bool a nullable position carries ahead of its value, written exactly as a
  // bool value is
  private static final ByteString ABSENT = ByteString.copyFrom(new byte[] { 0 });

  private static final ByteString PRESENT = ByteString.copyFrom(new byte[] { 1 });

  /** Encode an object. */
  public static ByteString encode(Object value, KRPC.Type type) {
    // A null value is signaled out-of-band by is_null; return null to indicate this.
    if (value == null) {
      return null;
    }
    return encodeValue(value, type);
  }

  /**
   * Encode one of the values a collection or a structure holds, at the position the given type
   * describes. A position that can hold null carries a presence bool before its value, and
   * holds nothing else when that bool is false.
   */
  static ByteString encodeItem(Object value, KRPC.Type type) {
    if (!type.getNullable()) {
      if (value == null) {
        throw new EncodingException("Failed to encode a null at a position that cannot hold one");
      }
      return encodeValue(value, type);
    }
    return value == null ? ABSENT : PRESENT.concat(encodeValue(value, type));
  }

  private static ByteString encodeValue(Object value, KRPC.Type type) {
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
          return encodeMessage((Message) value);
        default:
          throw new EncodingException("Failed to encode value");
      }
    } catch (IOException exn) {
      throw new EncodingException("Failed to encode value", exn);
    }
  }

  /**
   * Describes the first value within a value that encode rejects at the given type, naming its
   * position within the value, or returns null if there is none. The position names the value
   * itself, e.g. "argument 1".
   */
  static String describeMismatch(Object value, KRPC.Type type, String position) {
    if (value == null) {
      return type.getNullable() ? null
          : position + " is null, where the function takes a " + type.getCode()
              + " that is not nullable";
    }
    Class<?> expected = encodedClass(type.getCode());
    if (expected != null && !expected.isInstance(value)) {
      return position + " is a " + value.getClass().getName() + ", where the function takes a "
          + type.getCode();
    }
    KRPC.Type.TypeCode code = type.getCode();
    String mismatch = null;
    if (code == KRPC.Type.TypeCode.STRUCT) {
      List<KRPC.Type> fieldTypes = StructInfo.of(type).fieldTypes;
      Object[] values = ((RemoteStruct) value).fieldValues();
      for (int i = 0; i < values.length && i < fieldTypes.size() && mismatch == null; i++) {
        mismatch = describeMismatch(
            values[i], fieldTypes.get(i), "field " + i + " of " + position);
      }
    } else if (code == KRPC.Type.TypeCode.TUPLE) {
      Tuple tuple = (Tuple) value;
      if (tuple.getSize() != type.getTypesCount()) {
        return position + " has " + tuple.getSize()
            + " items, where the function takes a TUPLE of " + type.getTypesCount();
      }
      for (int i = 0; i < tuple.getSize() && mismatch == null; i++) {
        mismatch = describeMismatch(
            tuple.getValue(i), type.getTypes(i), "the item at index " + i + " of " + position);
      }
    } else if (code == KRPC.Type.TypeCode.LIST) {
      List<?> list = (List<?>) value;
      for (int i = 0; i < list.size() && mismatch == null; i++) {
        mismatch = describeMismatch(
            list.get(i), type.getTypes(0), "the item at index " + i + " of " + position);
      }
    } else if (code == KRPC.Type.TypeCode.SET) {
      for (Object item : (Set<?>) value) {
        mismatch = describeMismatch(item, type.getTypes(0), "an item of " + position);
        if (mismatch != null) {
          break;
        }
      }
    } else if (code == KRPC.Type.TypeCode.DICTIONARY) {
      for (Map.Entry<?, ?> entry : ((Map<?, ?>) value).entrySet()) {
        mismatch = describeMismatch(entry.getKey(), type.getTypes(0), "a key of " + position);
        if (mismatch == null) {
          mismatch = describeMismatch(entry.getValue(), type.getTypes(1),
              "the value for key " + entry.getKey() + " of " + position);
        }
        if (mismatch != null) {
          break;
        }
      }
    }
    return mismatch;
  }

  /** The class encodeValue casts a value of the given type to. */
  private static Class<?> encodedClass(KRPC.Type.TypeCode code) {
    switch (code) {
      case DOUBLE:
        return Double.class;
      case FLOAT:
        return Float.class;
      case SINT32:
      case UINT32:
        return Integer.class;
      case SINT64:
      case UINT64:
        return Long.class;
      case BOOL:
        return Boolean.class;
      case STRING:
        return String.class;
      case BYTES:
        return byte[].class;
      case CLASS:
        return RemoteObject.class;
      case ENUMERATION:
        return RemoteEnum.class;
      case STRUCT:
        return RemoteStruct.class;
      case TUPLE:
        return Tuple.class;
      case LIST:
        return List.class;
      case SET:
        return Set.class;
      case DICTIONARY:
        return Map.class;
      default:
        return null;
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
    return encodeUint64(RemoteObject.getObjectId(value));
  }

  static ByteString encodeEnum(RemoteEnum value) throws IOException {
    return encodeSint32(value.getValue());
  }

  /**
   * A structure is encoded as the values of its fields in order, which is the same encoding
   * as a tuple of those values.
   */
  static ByteString encodeStruct(RemoteStruct value, KRPC.Type type) throws IOException {
    List<KRPC.Type> fieldTypes = StructInfo.of(type).fieldTypes;
    Object[] values = value.fieldValues();
    if (values.length != fieldTypes.size()) {
      throw new EncodingException("Failed to encode struct");
    }
    KRPC.Tuple.Builder struct = KRPC.Tuple.newBuilder();
    for (int i = 0; i < values.length; i++) {
      struct.addItems(encodeItem(values[i], fieldTypes.get(i)));
    }
    return UnsafeByteOperations.unsafeWrap(struct.build().toByteArray());
  }

  static ByteString encodeTuple(Tuple value, List<KRPC.Type> valueTypes) throws IOException {
    if (value.getSize() != valueTypes.size()) {
      throw new EncodingException("Failed to encode tuple");
    }
    KRPC.Tuple.Builder tuple = KRPC.Tuple.newBuilder();
    for (int i = 0; i < value.getSize(); i++) {
      tuple.addItems(encodeItem(value.getValue(i), valueTypes.get(i)));
    }
    return UnsafeByteOperations.unsafeWrap(tuple.build().toByteArray());
  }

  static ByteString encodeList(List<?> value, KRPC.Type valueType) throws IOException {
    KRPC.List.Builder list = KRPC.List.newBuilder();
    for (Object item : value) {
      list.addItems(encodeItem(item, valueType));
    }
    return UnsafeByteOperations.unsafeWrap(list.build().toByteArray());
  }

  static ByteString encodeSet(Set<?> value, KRPC.Type valueType) throws IOException {
    KRPC.Set.Builder set = KRPC.Set.newBuilder();
    for (Object item : value) {
      set.addItems(encodeItem(item, valueType));
    }
    return UnsafeByteOperations.unsafeWrap(set.build().toByteArray());
  }

  static ByteString encodeDictionary(Map<?, ?> value, KRPC.Type keyType, KRPC.Type valueType)
      throws IOException {
    KRPC.Dictionary.Builder dictionary = KRPC.Dictionary.newBuilder();
    KRPC.DictionaryEntry.Builder dictionaryEntry = KRPC.DictionaryEntry.newBuilder();
    for (Map.Entry<?, ?> entry : value.entrySet()) {
      dictionaryEntry.setKey(encodeItem(entry.getKey(), keyType));
      dictionaryEntry.setValue(encodeItem(entry.getValue(), valueType));
      dictionary.addEntries(dictionaryEntry.build());
    }
    return UnsafeByteOperations.unsafeWrap(dictionary.build().toByteArray());
  }

  static ByteString encodeMessage(Message value) throws IOException {
    return UnsafeByteOperations.unsafeWrap(value.toByteArray());
  }
}
