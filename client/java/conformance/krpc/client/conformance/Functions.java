package krpc.client.conformance;

import java.util.HashMap;
import java.util.Map;
import java.util.function.Function;
import krpc.client.Connection;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.TestService.TestClass;
import krpc.client.services.TestService.TestEnum;
import krpc.conformance.Testee.AddFunctionEvent;
import krpc.conformance.Testee.AddFunctionStream;
import krpc.conformance.Testee.Invoke;
import krpc.conformance.Testee.RunFunction;
import krpc.conformance.Testee.Value;

/** Server side functions, with their values converted at the types the driver names. */
final class Functions {
  private Functions() {}

  static Value call(Connection connection, Invoke invoke) throws Exception {
    return Values.resultObject(Expression.call(connection, Dispatch.call(connection, invoke)));
  }

  static StreamHandle addStream(Connection connection, AddFunctionStream command)
      throws Exception {
    Function<Object, Value> result = result(command.getType());
    Function<Object, Value> convert = value -> {
      if (value == null && !command.getType().startsWith("nullable ")) {
        throw new Testee.UnsupportedException("The client's type can hold null");
      }
      return result.apply(value);
    };
    return new StreamHandle(connection.addStream(function(command.getFunction())), convert);
  }

  static Value addEvent(Connection connection, AddFunctionEvent command) throws Exception {
    return Values.resultEvent(connection.addEvent(function(command.getFunction())));
  }

  static Value run(Connection connection, RunFunction command) throws Exception {
    Object[] args = new Object[command.getArgumentsCount()];
    for (int i = 0; i < args.length; i++) {
      Value value = command.getArguments(i);
      args[i] = value.hasNull() ? null : type(command.getArgumentTypes(i)).arg.apply(value);
    }
    Object value = connection.run(function(command.getFunction()), args);
    if (command.getType().isEmpty()) {
      return Values.resultNone();
    }
    if (value == null && !command.getType().startsWith("nullable ")) {
      throw new Testee.UnsupportedException("The client's type can hold null");
    }
    return result(command.getType()).apply(value);
  }

  private static Expression function(Value value) {
    return Values.arg(Values.argObject(Expression.class), value);
  }

  /** The conversions of a type the driver names, to an argument and from a value. */
  private static final class Type {
    final Function<Value, ?> arg;
    final Function<Object, Value> result;

    @SuppressWarnings("unchecked")
    <T> Type(Function<Value, T> arg, Function<T, Value> result) {
      this.arg = arg;
      this.result = x -> result.apply((T) x);
    }
  }

  private static final Map<String, Type> TYPES = new HashMap<>();

  static {
    TYPES.put("double", new Type(Values::argDouble, Values::resultDouble));
    TYPES.put("float", new Type(Values::argFloat, Values::resultFloat));
    TYPES.put("sint32", new Type(Values::argSint32, Values::resultSint32));
    TYPES.put("sint64", new Type(Values::argSint64, Values::resultSint64));
    TYPES.put("uint32", new Type(Values::argUint32, Values::resultUint32));
    TYPES.put("uint64", new Type(Values::argUint64, Values::resultUint64));
    TYPES.put("bool", new Type(Values::argBool, Values::resultBool));
    TYPES.put("string", new Type(Values::argString, Values::resultString));
    TYPES.put("bytes", new Type(Values::argBytes, Values::resultBytes));
    TYPES.put("TestClass", new Type(Values.argObject(TestClass.class), Values::resultObject));
    TYPES.put("TestEnum", new Type(Values.argEnum(TestEnum::fromValue), Values::resultEnum));
    TYPES.put(
        "TestStruct",
        new Type(
            TestServiceDispatch::argTestServiceTestStruct,
            TestServiceDispatch::resultTestServiceTestStruct));
    TYPES.put("nullable sint32", new Type(Values::argSint32, Values::resultSint32));
    TYPES.put(
        "list(sint32)",
        new Type(Values.argList(Values::argSint32), Values.resultList(Values::resultSint32)));
    TYPES.put(
        "list(sint64)",
        new Type(Values.argList(Values::argSint64), Values.resultList(Values::resultSint64)));
    TYPES.put(
        "list(nullable sint32)",
        new Type(
            Values.argList(Values.argNullable(Values::argSint32)),
            Values.<Integer>resultList(Values.resultNullable(Values::resultSint32))));
    TYPES.put(
        "list(string)",
        new Type(Values.argList(Values::argString), Values.resultList(Values::resultString)));
    TYPES.put(
        "list(nullable TestClass)",
        new Type(
            Values.argList(Values.argNullable(Values.argObject(TestClass.class))),
            Values.<TestClass>resultList(Values::resultObject)));
    TYPES.put(
        "tuple(sint32)",
        new Type(Values.argUnit(Values::argSint32), Values.resultUnit(Values::resultSint32)));
    TYPES.put(
        "set(sint32)",
        new Type(Values.argSet(Values::argSint32), Values.resultSet(Values::resultSint32)));
    TYPES.put(
        "tuple(sint32,bool)",
        new Type(
            Values.argPair(Values::argSint32, Values::argBool),
            Values.resultPair(Values::resultSint32, Values::resultBool)));
    TYPES.put(
        "dictionary(string,list(sint32))",
        new Type(
            Values.argMap(Values::argString, Values.argList(Values::argSint32)),
            Values.resultMap(Values::resultString, Values.resultList(Values::resultSint32))));
    TYPES.put(
        "dictionary(string,nullable TestClass)",
        new Type(
            Values.argMap(Values::argString, Values.argNullable(Values.argObject(TestClass.class))),
            Values.<String, TestClass>resultMap(Values::resultString, Values::resultObject)));
    TYPES.put(
        "tuple(sint32,nullable TestClass)",
        new Type(
            Values.argPair(
                Values::argSint32, Values.argNullable(Values.argObject(TestClass.class))),
            Values.<Integer, TestClass>resultPair(Values::resultSint32, Values::resultObject)));
  }

  private static Type type(String name) {
    Type type = TYPES.get(name);
    if (type == null) {
      throw new Testee.UnsupportedException("No function of type " + name);
    }
    return type;
  }

  private static Function<Object, Value> result(String name) {
    Function<Object, Value> convert = type(name).result;
    return x -> x == null ? Value.newBuilder().setNull(true).build() : convert.apply(x);
  }
}
