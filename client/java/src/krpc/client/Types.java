package krpc.client;

import krpc.schema.KRPC.Type;
import krpc.schema.KRPC.Type.TypeCode;

public class Types {

  public static Type createValue(TypeCode code) {
    return Type.newBuilder().setCode(code).build();
  }

  public static Type createMessage(TypeCode code) {
    return Type.newBuilder().setCode(code).build();
  }

  /** Create a class type with the given name. */
  public static Type createClass(String service, String name) {
    return Type.newBuilder()
      .setCode(TypeCode.CLASS)
      .setService(service)
      .setName(name)
      .build();
  }

  /** Create an enumeration type with the given name. */
  public static Type createEnumeration(String service, String name) {
    return Type.newBuilder()
      .setCode(TypeCode.ENUMERATION)
      .setService(service)
      .setName(name)
      .build();
  }

  /** Create a tuple type with the given element types. */
  public static Type createTuple(Type... valueTypes) {
    Type.Builder type = Type.newBuilder();
    type.setCode(TypeCode.TUPLE);
    for (Type valueType : valueTypes) {
      type.addTypes(valueType);
    }
    return type.build();
  }

  /** Create a list type with the given value type. */
  public static Type createList(Type valueType) {
    Type.Builder type = Type.newBuilder();
    type.setCode(TypeCode.LIST);
    type.addTypes(valueType);
    return type.build();
  }

  /** Create a set type with the given value type. */
  public static Type createSet(Type valueType) {
    Type.Builder type = Type.newBuilder();
    type.setCode(TypeCode.SET);
    type.addTypes(valueType);
    return type.build();
  }

  /** Create a dictionary type with the given key and value types. */
  public static Type createDictionary(Type keyType, Type valueType) {
    Type.Builder type = Type.newBuilder();
    type.setCode(TypeCode.DICTIONARY);
    type.addTypes(keyType);
    type.addTypes(valueType);
    return type.build();
  }
}
