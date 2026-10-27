package krpc.client.conformance;

import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.function.Function;
import krpc.client.Connection;
import krpc.client.Encoder;
import krpc.client.Types;
import krpc.client.services.TestService.TestClass;
import krpc.client.services.TestService.TestEnum;
import krpc.conformance.Testee.Decode;
import krpc.conformance.Testee.Encode;
import krpc.conformance.Testee.Value;
import krpc.schema.KRPC.Type;
import krpc.schema.KRPC.Type.TypeCode;

/** The client's encoder and decoder, for each codec type the driver names. */
final class Codec {
  private Codec() {}

  private static final class Entry {
    final Type type;
    final Function<Value, Object> arg;
    final Function<Object, Value> result;

    Entry(Type type, Function<Value, Object> arg, Function<Object, Value> result) {
      this.type = type;
      this.arg = arg;
      this.result = result;
    }
  }

  @SuppressWarnings("unchecked")
  private static <T> Entry make(Type type, Function<Value, T> arg, Function<T, Value> result) {
    return new Entry(type, arg::apply, x -> result.apply((T) x));
  }

  private static Type value(TypeCode code) {
    return Types.createValue(code);
  }

  private static final Map<String, Entry> ENTRIES = new HashMap<>();

  static {
    Type uint32 = value(TypeCode.UINT32);
    Type string = value(TypeCode.STRING);
    Type testClass = Types.createClass("TestService", "TestClass");
    ENTRIES.put("double", make(value(TypeCode.DOUBLE), Values::argDouble, Values::resultDouble));
    ENTRIES.put("float", make(value(TypeCode.FLOAT), Values::argFloat, Values::resultFloat));
    ENTRIES.put("sint32", make(value(TypeCode.SINT32), Values::argSint32, Values::resultSint32));
    ENTRIES.put("sint64", make(value(TypeCode.SINT64), Values::argSint64, Values::resultSint64));
    ENTRIES.put("uint32", make(uint32, Values::argUint32, Values::resultUint32));
    ENTRIES.put("uint64", make(value(TypeCode.UINT64), Values::argUint64, Values::resultUint64));
    ENTRIES.put("bool", make(value(TypeCode.BOOL), Values::argBool, Values::resultBool));
    ENTRIES.put("string", make(string, Values::argString, Values::resultString));
    ENTRIES.put("bytes", make(value(TypeCode.BYTES), Values::argBytes, Values::resultBytes));
    ENTRIES.put("TestEnum",
        make(Types.createEnumeration("TestService", "TestEnum"),
            Values.argEnum(TestEnum::fromValue), Values::resultEnum));
    ENTRIES.put("TestClass",
        make(testClass, Values.argObject(TestClass.class), Values::resultObject));
    Type sint32 = value(TypeCode.SINT32);
    Type nullableTestClass = Types.nullable(testClass);
    ENTRIES.put("list(sint32)",
        make(Types.createList(sint32), Values.argList(Values::argSint32),
            Values.<Integer>resultList(Values::resultSint32)));
    ENTRIES.put("set(sint32)",
        make(Types.createSet(sint32), Values.argSet(Values::argSint32),
            Values.<Integer>resultSet(Values::resultSint32)));
    ENTRIES.put("dictionary(string,sint32)",
        make(Types.createDictionary(string, sint32),
            Values.argMap(Values::argString, Values::argSint32),
            Values.<String, Integer>resultMap(Values::resultString, Values::resultSint32)));
    ENTRIES.put("tuple(sint32,bool)",
        make(Types.createTuple(sint32, value(TypeCode.BOOL)),
            Values.argPair(Values::argSint32, Values::argBool),
            Values.<Integer, Boolean>resultPair(Values::resultSint32, Values::resultBool)));
    ENTRIES.put("list(list(nullable TestClass))",
        make(Types.createList(Types.createList(nullableTestClass)),
            Values.argList(Values.argList(Values.argObject(TestClass.class))),
            Values.<List<TestClass>>resultList(
                Values.<TestClass>resultList(Values::resultObject))));
    ENTRIES.put("list(nullable sint32)",
        make(Types.createList(Types.nullable(sint32)),
            Values.argList(Values.argNullable(Values::argSint32)),
            Values.<Integer>resultList(Values.resultNullable(Values::resultSint32))));
    ENTRIES.put("list(nullable bool)",
        make(Types.createList(Types.nullable(value(TypeCode.BOOL))),
            Values.argList(Values.argNullable(Values::argBool)),
            Values.<Boolean>resultList(Values.resultNullable(Values::resultBool))));
    ENTRIES.put("list(nullable bytes)",
        make(Types.createList(Types.nullable(value(TypeCode.BYTES))),
            Values.argList(Values.argNullable(Values::argBytes)),
            Values.<byte[]>resultList(Values::resultBytes)));
    ENTRIES.put("dictionary(string,nullable TestClass)",
        make(Types.createDictionary(string, nullableTestClass),
            Values.argMap(Values::argString, Values.argObject(TestClass.class)),
            Values.<String, TestClass>resultMap(Values::resultString, Values::resultObject)));
    ENTRIES.put("tuple(sint32,nullable TestClass)",
        make(Types.createTuple(sint32, nullableTestClass),
            Values.argPair(Values::argSint32, Values.argObject(TestClass.class)),
            Values.<Integer, TestClass>resultPair(Values::resultSint32, Values::resultObject)));
    ENTRIES.put("list(nullable TestClass)",
        make(Types.createList(nullableTestClass),
            Values.argList(Values.argObject(TestClass.class)),
            Values.<TestClass>resultList(Values::resultObject)));
    Type uint32List = Types.createList(uint32);
    ENTRIES.put("tuple(uint32)",
        make(Types.createTuple(uint32), Values.argUnit(Values::argUint32),
            Values.<Integer>resultUnit(Values::resultUint32)));
    ENTRIES.put("tuple(uint32,string,bool)",
        make(Types.createTuple(uint32, string, value(TypeCode.BOOL)),
            Values.argTriplet(Values::argUint32, Values::argString, Values::argBool),
            Values.<Integer, String, Boolean>resultTriplet(
                Values::resultUint32, Values::resultString, Values::resultBool)));
    ENTRIES.put("tuple(sint32,sint64)",
        make(Types.createTuple(sint32, value(TypeCode.SINT64)),
            Values.argPair(Values::argSint32, Values::argSint64),
            Values.<Integer, Long>resultPair(Values::resultSint32, Values::resultSint64)));
    ENTRIES.put("tuple(sint32,nullable string)",
        make(Types.createTuple(sint32, Types.nullable(string)),
            Values.argPair(Values::argSint32, Values.argNullable(Values::argString)),
            Values.<Integer, String>resultPair(
                Values::resultSint32, Values.resultNullable(Values::resultString))));
    ENTRIES.put("dictionary(string,list(uint32))",
        make(Types.createDictionary(string, Types.createList(uint32)),
            Values.argMap(Values::argString, Values.argList(Values::argUint32)),
            Values.<String, List<Integer>>resultMap(
                Values::resultString, Values.<Integer>resultList(Values::resultUint32))));
    ENTRIES.put("dictionary(string,nullable sint32)",
        make(Types.createDictionary(string, Types.nullable(sint32)),
            Values.argMap(Values::argString, Values.argNullable(Values::argSint32)),
            Values.<String, Integer>resultMap(
                Values::resultString, Values.resultNullable(Values::resultSint32))));
    ENTRIES.put("list(nullable list(uint32))",
        make(Types.createList(Types.nullable(uint32List)),
            Values.argList(Values.argNullable(Values.argList(Values::argUint32))),
            Values.<List<Integer>>resultList(
                Values.resultNullable(Values.<Integer>resultList(Values::resultUint32)))));
    ENTRIES.put("list(nullable TestStruct)",
        make(Types.createList(Types.nullable(Types.createStruct("TestService", "TestStruct"))),
            Values.argList(Values.argNullable(TestServiceDispatch::argTestServiceTestStruct)),
            Values.<krpc.client.services.TestService.TestStruct>resultList(
                Values.resultNullable(TestServiceDispatch::resultTestServiceTestStruct))));
    ENTRIES.put("list(string)",
        make(Types.createList(string), Values.argList(Values::argString),
            Values.<String>resultList(Values::resultString)));
    ENTRIES.put("TestStruct",
        make(Types.createStruct("TestService", "TestStruct"),
            TestServiceDispatch::argTestServiceTestStruct,
            TestServiceDispatch::resultTestServiceTestStruct));
    ENTRIES.put("TestNullableStruct",
        make(Types.createStruct("TestService", "TestNullableStruct"),
            TestServiceDispatch::argTestServiceTestNullableStruct,
            TestServiceDispatch::resultTestServiceTestNullableStruct));
  }

  private static Entry find(String type) {
    Entry entry = ENTRIES.get(type);
    if (entry == null) {
      throw new Testee.UnsupportedException("The testee has no codec type " + type);
    }
    return entry;
  }

  static Value encode(Encode command) {
    Entry entry = find(command.getType());
    return Value.newBuilder()
        .setBytes(Encoder.encode(entry.arg.apply(command.getValue()), entry.type))
        .build();
  }

  static Value decode(Connection connection, Decode command) {
    Entry entry = find(command.getType());
    return entry.result.apply(Encoder.decode(command.getData(), entry.type, connection));
  }
}
