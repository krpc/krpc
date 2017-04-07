package krpc.client;

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

import com.google.protobuf.ByteString;
import com.google.protobuf.CodedInputStream;
import com.google.protobuf.CodedOutputStream;
import com.google.protobuf.Message;

import krpc.schema.KRPC;

// TODO: remove all the ByteString.copyFrom calls

public class Encoder {
    static final byte[] RPC_HELLO_MESSAGE = new byte[] { 0x48, 0x45, 0x4C, 0x4C, 0x4F, 0x2D, 0x52, 0x50, 0x43, 0x00, 0x00, 0x00 };
    static final byte[] STREAM_HELLO_MESSAGE = new byte[] { 0x48, 0x45, 0x4C, 0x4C, 0x4F, 0x2D, 0x53, 0x54, 0x52, 0x45, 0x41, 0x4D };
    static final int CLIENT_IDENTIFIER_LENGTH = 16;
    static final int CLIENT_NAME_LENGTH = 32;
    static final byte[] OK_MESSAGE = { 0x4F, 0x4B };

    static byte[] encodeClientName(String name) throws UnsupportedEncodingException {
        byte[] clientName = new byte[CLIENT_NAME_LENGTH];
        byte[] tmp = name.getBytes("UTF-8");
        for (int i = 0; i < tmp.length && i < CLIENT_NAME_LENGTH; i++)
            clientName[i] = tmp[i];
        return clientName;
    }

    static String guidToString(byte[] guid) {
        StringBuilder builder = new StringBuilder();
        for (int i = 3; i >= 0; i--)
            builder.append(String.format("%02x", guid[i]));
        builder.append("-");
        for (int i = 5; i >= 4; i--)
            builder.append(String.format("%02x", guid[i]));
        builder.append("-");
        for (int i = 7; i >= 6; i--)
            builder.append(String.format("%02x", guid[i]));
        builder.append("-");
        for (int i = 8; i <= 9; i++)
            builder.append(String.format("%02x", guid[i]));
        builder.append("-");
        for (int i = 10; i <= 15; i++)
            builder.append(String.format("%02x", guid[i]));
        return builder.toString();
    }

    public static ByteString encode(Object value) throws IOException {
        // TODO: cannot distinguish whether to encode integers as
        // signed/unsigned
        if (value == null)
            return encodeUInt64(0); // null remote object
        else if (value instanceof Integer)
            return encodeInt32((int) value);
        else if (value instanceof Long)
            return encodeInt64((long) value);
        else if (value instanceof Float)
            return encodeFloat((float) value);
        else if (value instanceof Double)
            return encodeDouble((double) value);
        else if (value instanceof Boolean)
            return encodeBoolean((boolean) value);
        else if (value instanceof String)
            return encodeString((String) value);
        else if (value instanceof byte[])
            return encodeBytes((byte[]) value);
        else if (value instanceof Message)
            return encodeMessage((Message) value);
        else if (value instanceof RemoteObject)
            return encodeObject((RemoteObject) value);
        else if (value instanceof RemoteEnum)
            return encodeEnum((RemoteEnum) value);
        else if (value instanceof List<?>)
            return encodeList((List<?>) value);
        else if (value instanceof Map<?, ?>)
            return encodeDictionary((Map<?, ?>) value);
        else if (value instanceof Tuple)
            return encodeTuple((Tuple) value);
        else if (value instanceof Set<?>)
            return encodeSet((Set<?>) value);
        throw new IOException("Failed to encode value of type " + value.getClass().toString());
    }

    public static Object decode(ByteString data, TypeSpecification typeSpec, Connection connection) throws IOException {
        Class<?> type = typeSpec.getType();
        if (type == Integer.class)
            return decodeInt32(data);
        else if (type == Long.class)
            return decodeInt64(data);
        else if (type == Float.class)
            return decodeFloat(data);
        else if (type == Double.class)
            return decodeDouble(data);
        else if (type == Boolean.class)
            return decodeBoolean(data);
        else if (type == String.class)
            return decodeString(data);
        else if (type == byte[].class)
            return decodeBytes(data);
        else if (Message.class.isAssignableFrom(type))
            try {
                Message.Builder builder = (Message.Builder) type.getMethod("newBuilder").invoke(null);
                return decodeMessage(builder, data);
            } catch (InvocationTargetException e) {
                throw new IOException("Failed to decode message", e);
            } catch (IllegalAccessException e) {
                throw new IOException("Failed to decode message", e);
            } catch (NoSuchMethodException e) {
                throw new IOException("Failed to decode message", e);
            }
        else if (RemoteObject.class.isAssignableFrom(type))
            return decodeObject(data, type, connection);
        else if (RemoteEnum.class.isAssignableFrom(type))
            return decodeEnum(data, type);
        else if (type == List.class)
            return decodeList(data, typeSpec, connection);
        else if (type == Map.class)
            return decodeDictionary(data, typeSpec, connection);
        else if (Tuple.class.isAssignableFrom(type))
            return decodeTuple(data, typeSpec, connection);
        else if (type == Set.class)
            return decodeSet(data, typeSpec, connection);
        throw new IOException("Failed to decode value " + type);
    }

    static ByteString encodeInt32(int value) throws IOException {
        byte[] data = new byte[CodedOutputStream.computeInt32SizeNoTag(value)];
        CodedOutputStream stream = CodedOutputStream.newInstance(data);
        stream.writeInt32NoTag(value);
        stream.flush();
        stream.checkNoSpaceLeft();
        return ByteString.copyFrom(data);
    }

    static ByteString encodeInt64(long value) throws IOException {
        byte[] data = new byte[CodedOutputStream.computeInt64SizeNoTag(value)];
        CodedOutputStream stream = CodedOutputStream.newInstance(data);
        stream.writeInt64NoTag(value);
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

    static ByteString encodeFloat(float value) throws IOException {
        byte[] data = new byte[CodedOutputStream.computeFloatSizeNoTag(value)];
        CodedOutputStream stream = CodedOutputStream.newInstance(data);
        stream.writeFloatNoTag(value);
        stream.flush();
        stream.checkNoSpaceLeft();
        return ByteString.copyFrom(data);
    }

    static ByteString encodeDouble(double value) throws IOException {
        byte[] data = new byte[CodedOutputStream.computeDoubleSizeNoTag(value)];
        CodedOutputStream stream = CodedOutputStream.newInstance(data);
        stream.writeDoubleNoTag(value);
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

    static ByteString encodeMessage(Message value) throws IOException {
        return ByteString.copyFrom(value.toByteArray());
    }

    static ByteString encodeObject(RemoteObject value) throws IOException {
        return encodeUInt64(value.id);
    }

    static ByteString encodeEnum(RemoteEnum value) throws IOException {
        return encodeInt32(value.getValue());
    }

    static ByteString encodeList(List<?> value) throws IOException {
        KRPC.List.Builder list = KRPC.List.newBuilder();
        for (Object item : value)
            list.addItems(encode(item));
        return ByteString.copyFrom(list.build().toByteArray());
    }

    static ByteString encodeDictionary(Map<?, ?> value) throws IOException {
        KRPC.Dictionary.Builder dictionary = KRPC.Dictionary.newBuilder();
        KRPC.DictionaryEntry.Builder dictionaryEntry = KRPC.DictionaryEntry.newBuilder();
        for (Map.Entry<?, ?> entry : value.entrySet()) {
            dictionaryEntry.setKey(encode(entry.getKey()));
            dictionaryEntry.setValue(encode(entry.getValue()));
            dictionary.addEntries(dictionaryEntry.build());
        }
        return ByteString.copyFrom(dictionary.build().toByteArray());
    }

    static ByteString encodeTuple(Tuple value) throws IOException {
        KRPC.Tuple.Builder tuple = KRPC.Tuple.newBuilder();
        for (Object item : value)
            tuple.addItems(encode(item));
        return ByteString.copyFrom(tuple.build().toByteArray());
    }

    static ByteString encodeSet(Set<?> value) throws IOException {
        KRPC.Set.Builder set = KRPC.Set.newBuilder();
        for (Object item : value)
            set.addItems(encode(item));
        return ByteString.copyFrom(set.build().toByteArray());
    }

    static int decodeInt32(ByteString data) throws IOException {
        CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
        return stream.readInt32();
    }

    static long decodeInt64(ByteString data) throws IOException {
        CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
        return stream.readInt64();
    }

    static int decodeUInt32(ByteString data) throws IOException {
        CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
        return stream.readUInt32();
    }

    static long decodeUInt64(ByteString data) throws IOException {
        CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
        return stream.readUInt64();
    }

    static float decodeFloat(ByteString data) throws IOException {
        CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
        return stream.readFloat();
    }

    static double decodeDouble(ByteString data) throws IOException {
        CodedInputStream stream = CodedInputStream.newInstance(data.toByteArray());
        return stream.readDouble();
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

    static Message decodeMessage(Message.Builder builder, ByteString data) throws IOException {
        builder.mergeFrom(data);
        return builder.build();
    }

    @SuppressWarnings({ "unchecked" })
    static <T> T decodeObject(ByteString data, Class<T> type, Connection connection) throws IOException {
        try {
            long id = decodeUInt64(data);
            Constructor<?> ctor = type.getConstructor(Connection.class, long.class);
            return id == 0 ? null : (T) ctor.newInstance(connection, id);
        } catch (NoSuchMethodException e) {
            throw new IOException("Failed to decode object", e);
        } catch (InstantiationException e) {
            throw new IOException("Failed to decode object", e);
        } catch (IllegalAccessException e) {
            throw new IOException("Failed to decode object", e);
        } catch (InvocationTargetException e) {
            throw new IOException("Failed to decode object", e);
        }
    }

    @SuppressWarnings({ "unchecked" })
    static <T> T decodeEnum(ByteString data, Class<T> type) throws IOException {
        try {
            int value = decodeInt32(data);
            return (T) type.getMethod("fromValue", int.class).invoke(null, value);
        } catch (NoSuchMethodException e) {
            throw new IOException("Failed to decode object", e);
        } catch (IllegalAccessException e) {
            throw new IOException("Failed to decode object", e);
        } catch (InvocationTargetException e) {
            throw new IOException("Failed to decode object", e);
        }
    }

    @SuppressWarnings({ "unchecked" })
    static <T> List<T> decodeList(ByteString data, TypeSpecification typeSpec, Connection connection) throws IOException {
        KRPC.List listMessage = KRPC.List.newBuilder().mergeFrom(data).build();
        List<T> list = new ArrayList<T>(listMessage.getItemsCount());
        for (ByteString item : listMessage.getItemsList())
            list.add((T) decode(item, typeSpec.getGenericTypes()[0], connection));
        return list;
    }

    @SuppressWarnings({ "unchecked", "rawtypes" })
    static Tuple decodeTuple(ByteString data, TypeSpecification typeSpec, Connection connection) throws IOException {
        KRPC.Tuple tupleMessage = KRPC.Tuple.newBuilder().mergeFrom(data).build();
        int numElements = tupleMessage.getItemsCount();
        Object[] e = new Object[numElements];
        for (int i = 0; i < numElements; i++) {
            ByteString item = tupleMessage.getItems(i);
            e[i] = decode(item, typeSpec.getGenericTypes()[i], connection);
        }
        switch (numElements) {
        case 1:
            return new Unit(e[0]);
        case 2:
            return new Pair(e[0], e[1]);
        case 3:
            return new Triplet(e[0], e[1], e[2]);
        case 4:
            return new Quartet(e[0], e[1], e[2], e[3]);
        case 5:
            return new Quintet(e[0], e[1], e[2], e[3], e[4]);
        case 6:
            return new Sextet(e[0], e[1], e[2], e[3], e[4], e[5]);
        case 7:
            return new Septet(e[0], e[1], e[2], e[3], e[4], e[5], e[6]);
        case 8:
            return new Octet(e[0], e[1], e[2], e[3], e[4], e[5], e[6], e[7]);
        case 9:
            return new Ennead(e[0], e[1], e[2], e[3], e[4], e[5], e[6], e[7], e[8]);
        case 10:
            return new Decade(e[0], e[1], e[2], e[3], e[4], e[5], e[6], e[7], e[8], e[9]);
        }
        throw new IOException("Failed to decode tuple");
    }

    @SuppressWarnings({ "unchecked" })
    static <K, V> Map<K, V> decodeDictionary(ByteString data, TypeSpecification typeSpec, Connection connection) throws IOException {
        KRPC.Dictionary dictionaryMessage = KRPC.Dictionary.newBuilder().mergeFrom(data).build();
        Map<K, V> dictionary = new HashMap<K, V>(dictionaryMessage.getEntriesCount());
        for (KRPC.DictionaryEntry entry : dictionaryMessage.getEntriesList()) {
            K key = (K) decode(entry.getKey(), typeSpec.getGenericTypes()[0], connection);
            V value = (V) decode(entry.getValue(), typeSpec.getGenericTypes()[1], connection);
            dictionary.put(key, value);
        }
        return dictionary;
    }

    @SuppressWarnings({ "unchecked" })
    static <T> Set<T> decodeSet(ByteString data, TypeSpecification typeSpec, Connection connection) throws IOException {
        KRPC.Set setMessage = KRPC.Set.newBuilder().mergeFrom(data).build();
        Set<T> set = new HashSet<T>(setMessage.getItemsCount());
        for (ByteString item : setMessage.getItemsList())
            set.add((T) decode(item, typeSpec.getGenericTypes()[0], connection));
        return set;
    }
}
