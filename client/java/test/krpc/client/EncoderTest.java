package krpc.client;

import static krpc.client.TestUtils.hexlify;
import static krpc.client.TestUtils.repeatedString;
import static krpc.client.TestUtils.unhexlify;
import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNull;
import static org.junit.Assert.assertThrows;

import com.google.protobuf.ByteString;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import krpc.client.Types;
import krpc.client.services.TestService;
import krpc.schema.KRPC;
import krpc.schema.KRPC.Type;
import krpc.schema.KRPC.Type.TypeCode;
import org.javatuples.Pair;
import org.javatuples.Triplet;
import org.javatuples.Unit;
import org.junit.Test;
import org.junit.runner.RunWith;
import org.junit.runners.Suite;

/** Tests for Encoder. */
public class EncoderTest {
  @Test
  public void testEncodeMessage() {
    KRPC.ProcedureCall call = KRPC.ProcedureCall.newBuilder()
        .setService("ServiceName").setProcedure("ProcedureName").build();
    Type type = Types.createMessage(TypeCode.PROCEDURE_CALL);
    ByteString data = Encoder.encode(call, type);
    String expected = "0a0b536572766963654e616d65120d50726f6365647572654e616d65";
    assertEquals(expected, hexlify(data));
  }

  @Test
  public void testEncodeValue() {
    Type type = Types.createValue(TypeCode.UINT32);
    ByteString data = Encoder.encode(300, type);
    assertEquals("ac02", hexlify(data));
  }

  @Test
  @SuppressWarnings("checkstyle:avoidescapedunicodecharacters")
  public void testEncodeUnicodeString() {
    Type type = Types.createValue(TypeCode.STRING);
    ByteString data = Encoder.encode("\u2122", type);
    assertEquals("03e284a2", hexlify(data));
  }

  @Test
  public void testEncodeClass() {
    TestService.TestClass obj = new TestService.TestClass(null, 300);
    Type type = Types.createClass("TestService", "TestClass");
    ByteString data = Encoder.encode(obj, type);
    assertEquals("ac02", hexlify(data));
  }

  @Test
  public void testEncodeNull() {
    // A null value is signaled out-of-band by is_null; the encoder returns null to indicate
    // this, regardless of the type.
    assertNull(Encoder.encode(null, Types.createClass("TestService", "TestClass")));
    assertNull(Encoder.encode(null, Types.createValue(TypeCode.STRING)));
    assertNull(Encoder.encode(null, Types.createValue(TypeCode.SINT32)));
    assertNull(Encoder.encode(null, Types.createList(Types.createValue(TypeCode.SINT32))));
  }

  @Test
  public void testDecodeMessage() {
    ByteString message = unhexlify("0a0b536572766963654e616d65120d50726f6365647572654e616d65");
    Type type = Types.createMessage(TypeCode.PROCEDURE_CALL);
    KRPC.ProcedureCall call = (KRPC.ProcedureCall) Encoder.decode(message, type, null);
    assertEquals("ServiceName", call.getService());
    assertEquals("ProcedureName", call.getProcedure());
  }

  @Test
  public void testDecodeValue() {
    Type type = Types.createValue(TypeCode.UINT32);
    int value = (int) Encoder.decode(unhexlify("ac02"), type, null);
    assertEquals(300, value);
  }

  @Test
  @SuppressWarnings("checkstyle:avoidescapedunicodecharacters")
  public void testDecodeUnicodeString() {
    Type type = Types.createValue(TypeCode.STRING);
    String value = (String) Encoder.decode(unhexlify("03e284a2"), type, null);
    assertEquals("\u2122", value);
  }

  @Test
  public void testDecodeClass() {
    Type type = Types.createClass("TestService", "TestClass");
    TestService.TestClass value =
        (TestService.TestClass) Encoder.decode(unhexlify("ac02"), type, null);
    assertEquals(new TestService.TestClass(null, 300), value);
  }


  // Check that the value encodes to the given data, and that the data decodes back to it
  private static void checkValue(Object value, String data, Type type) {
    assertEquals(data, hexlify(Encoder.encode(value, type)));
    assertEquals(value, Encoder.decode(unhexlify(data), type, null));
  }

  @Test
  public void testNullableListElements() {
    Type type = Types.createList(Types.nullable(Types.createValue(TypeCode.UINT32)));
    checkValue(new ArrayList<Integer>(), "", type);
    checkValue(Arrays.asList((Integer) null), "0a0100", type);
    // A zero is a value like any other, and is told apart from a null by the presence bool
    checkValue(Arrays.asList(0), "0a020100", type);
    checkValue(Arrays.asList(1, null, 3), "0a0201010a01000a020103", type);
  }

  @Test
  public void testNullableDictionaryValues() {
    Type type = Types.createDictionary(
        Types.createValue(TypeCode.STRING), Types.nullable(Types.createValue(TypeCode.UINT32)));
    Map<String, Integer> value = new HashMap<String, Integer>();
    value.put("", null);
    checkValue(value, "0a060a0100120100", type);
  }

  @Test
  public void testNullableTupleItem() {
    Type type = Types.createTuple(
        Types.createValue(TypeCode.UINT32), Types.nullable(Types.createValue(TypeCode.STRING)));
    checkValue(new Pair<Integer, String>(1, "jeb"), "0a01010a0501036a6562", type);
    checkValue(new Pair<Integer, String>(1, null), "0a01010a0100", type);
  }

  @Test
  public void testNullableCollectionValues() {
    // A nullable position holding a collection carries the presence bool ahead of the
    // collection's own encoding
    Type type = Types.createList(
        Types.nullable(Types.createList(Types.createValue(TypeCode.UINT32))));
    checkValue(Arrays.asList((List<Integer>) null), "0a0100", type);
    checkValue(Arrays.asList(new ArrayList<Integer>()), "0a0101", type);
    checkValue(Arrays.asList(Arrays.asList(1)), "0a04010a0101", type);
  }

  @Test
  public void testNullableStructFields() {
    // Each nullable field carries its own presence bool, and a null field is that bool alone
    Type type = Types.createStruct("TestService", "TestNullableStruct");
    TestService.TestNullableStruct value =
        new TestService.TestNullableStruct(1, 2, null, null, null);
    checkValue(value, "0a01020a0201040a01000a01000a0100", type);
  }

  @Test
  public void testNullabilityIsReadAtEveryPosition() {
    // A service declares no nullable set element and no nullable dictionary key. The client
    // reads what the type says at every position rather than naming the ones a value can be
    // null at
    Type setType = Types.createSet(Types.nullable(Types.createValue(TypeCode.UINT32)));
    checkValue(new HashSet<Integer>(Arrays.asList((Integer) null)), "0a0100", setType);
    Type dictionaryType = Types.createDictionary(
        Types.nullable(Types.createValue(TypeCode.STRING)), Types.createValue(TypeCode.UINT32));
    Map<String, Integer> dictionary = new HashMap<String, Integer>();
    dictionary.put(null, 1);
    checkValue(dictionary, "0a060a0100120101", dictionaryType);
  }

  @Test
  public void testNullAtNonNullablePosition() {
    Type type = Types.createList(Types.createValue(TypeCode.UINT32));
    assertThrows(
        EncodingException.class, () -> Encoder.encode(Arrays.asList((Integer) null), type));
  }

  @Test
  public void testNullableValueWithoutPresenceBool() {
    // A list holding one item of zero length
    Type type = Types.createList(Types.nullable(Types.createValue(TypeCode.UINT32)));
    assertThrows(EncodingException.class, () -> Encoder.decode(unhexlify("0a00"), type, null));
  }

  @Test
  public void testGuid() {
    assertEquals(
        "6f271b39-00dd-4de4-9732-f0d3a68838df",
        Encoder.guidToString(unhexlify("391b276fdd00e44d9732f0d3a68838df").toByteArray()));
  }

  @SuppressWarnings({ "unchecked" })
  @Test
  public void testTupleCollection1() {
    Unit<Integer> value = new Unit<Integer>(1);
    String data = "0a0101";
    Type type = Types.createTuple(Types.createValue(TypeCode.UINT32));
    ByteString encodeResult = Encoder.encode(value, type);
    assertEquals(data, hexlify(encodeResult));
    Unit<Integer> decodeResult = (Unit<Integer>) Encoder.decode(unhexlify(data), type, null);
    assertEquals(value, decodeResult);
  }

  @SuppressWarnings({ "unchecked" })
  @Test
  public void testTupleCollection2() {
    Triplet<Integer, String, Boolean> value =
        new Triplet<Integer, String, Boolean>(1, "jeb", false);
    String data = "0a01010a04036a65620a0100";
    Type type = Types.createTuple(
        Types.createValue(TypeCode.UINT32),
        Types.createValue(TypeCode.STRING),
        Types.createValue(TypeCode.BOOL));
    ByteString encodeResult = Encoder.encode(value, type);
    assertEquals(data, hexlify(encodeResult));
    Triplet<Integer, String, Boolean> decodeResult =
        (Triplet<Integer, String, Boolean>) Encoder.decode(unhexlify(data), type, null);
    assertEquals(value, decodeResult);
  }
}
