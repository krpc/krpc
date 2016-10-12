package krpc.client;

import static krpc.client.TestUtils.hexlify;
import static krpc.client.TestUtils.repeatedString;
import static krpc.client.TestUtils.unhexlify;
import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNull;

import com.google.protobuf.ByteString;

import krpc.client.Types;
import krpc.client.services.TestService;
import krpc.schema.KRPC;
import krpc.schema.KRPC.Type;
import krpc.schema.KRPC.Type.TypeCode;

import org.javatuples.Triplet;
import org.javatuples.Unit;
import org.junit.Test;
import org.junit.runner.RunWith;
import org.junit.runners.Suite;

import java.io.IOException;
import java.io.UnsupportedEncodingException;

public class EncoderTest {
  @Test
  public void testEncodeMessage() throws IOException {
    KRPC.ProcedureCall call = KRPC.ProcedureCall.newBuilder()
        .setService("ServiceName").setProcedure("ProcedureName").build();
    Type type = Types.createMessage(TypeCode.PROCEDURE_CALL);
    ByteString data = Encoder.encode(call, type);
    String expected = "0a0b536572766963654e616d65120d50726f6365647572654e616d65";
    assertEquals(expected, hexlify(data));
  }

  @Test
  public void testEncodeValue() throws IOException {
    Type type = Types.createValue(TypeCode.UINT32);
    ByteString data = Encoder.encode(300, type);
    assertEquals("ac02", hexlify(data));
  }

  @Test
  @SuppressWarnings("checkstyle:avoidescapedunicodecharacters")
  public void testEncodeUnicodeString() throws IOException {
    Type type = Types.createValue(TypeCode.STRING);
    ByteString data = Encoder.encode("\u2122", type);
    assertEquals("03e284a2", hexlify(data));
  }

  @Test
  public void testEncodeClass() throws IOException {
    TestService.TestClass obj = new TestService.TestClass(null, 300);
    Type type = Types.createClass("TestService", "TestClass");
    ByteString data = Encoder.encode(obj, type);
    assertEquals("ac02", hexlify(data));
  }

  @Test
  public void testEncodeClassNull() throws IOException {
    Type type = Types.createClass("TestService", "TestClass");
    ByteString data = Encoder.encode(null, type);
    assertEquals("00", hexlify(data));
  }

  @Test
  public void testDecodeMessage() throws IOException {
    ByteString message = unhexlify("0a0b536572766963654e616d65120d50726f6365647572654e616d65");
    Type type = Types.createMessage(TypeCode.PROCEDURE_CALL);
    KRPC.ProcedureCall call = (KRPC.ProcedureCall) Encoder.decode(message, type, null);
    assertEquals("ServiceName", call.getService());
    assertEquals("ProcedureName", call.getProcedure());
  }

  @Test
  public void testDecodeValue() throws IOException {
    Type type = Types.createValue(TypeCode.UINT32);
    int value = (int) Encoder.decode(unhexlify("ac02"), type, null);
    assertEquals(300, value);
  }

  @Test
  @SuppressWarnings("checkstyle:avoidescapedunicodecharacters")
  public void testDecodeUnicodeString() throws IOException {
    Type type = Types.createValue(TypeCode.STRING);
    String value = (String) Encoder.decode(unhexlify("03e284a2"), type, null);
    assertEquals("\u2122", value);
  }

  @Test
  public void testDecodeClass() throws IOException {
    Type type = Types.createClass("TestService", "TestClass");
    TestService.TestClass value =
        (TestService.TestClass) Encoder.decode(unhexlify("ac02"), type, null);
    assertEquals(new TestService.TestClass(null, 300), value);
  }

  @Test
  public void testDecodeClassNull() throws IOException {
    Type type = Types.createClass("TestService", "TestClass");
    TestService.TestClass value =
        (TestService.TestClass) Encoder.decode(unhexlify("00"), type, null);
    assertNull(value);
  }

  @Test
  public void testGuid() throws IOException {
    assertEquals(
        "6f271b39-00dd-4de4-9732-f0d3a68838df",
        Encoder.guidToString(unhexlify("391b276fdd00e44d9732f0d3a68838df").toByteArray()));
  }

  @SuppressWarnings({ "unchecked" })
  @Test
  public void testTupleCollection1() throws IOException {
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
  public void testTupleCollection2() throws IOException {
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
