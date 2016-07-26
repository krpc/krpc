package krpc.client;

import krpc.schema.KRPC.Type;
import krpc.schema.KRPC.Type.TypeCode;

public class Types {

    public static Type CreateValue(TypeCode code) {
        return Type.newBuilder().setCode(code).build();
    }

    public static Type CreateMessage(TypeCode code) {
        return Type.newBuilder().setCode(code).build();
    }

    public static Type CreateClass(String service, String name) {
        return Type.newBuilder()
            .setCode(TypeCode.CLASS)
            .setService(service)
            .setName(name)
            .build();
    }

    public static Type CreateEnumeration(String service, String name) {
        return Type.newBuilder()
            .setCode(TypeCode.ENUMERATION)
            .setService(service)
            .setName(name)
            .build();
    }

    public static Type CreateTuple(Type... valueTypes) {
        Type.Builder type = Type.newBuilder();
        type.setCode(TypeCode.TUPLE);
        for (Type valueType : valueTypes)
            type.addTypes(valueType);
        return type.build();
    }

    public static Type CreateList(Type valueType) {
        Type.Builder type = Type.newBuilder();
        type.setCode(TypeCode.LIST);
        type.addTypes(valueType);
        return type.build();
    }

    public static Type CreateSet(Type valueType) {
        Type.Builder type = Type.newBuilder();
        type.setCode(TypeCode.SET);
        type.addTypes(valueType);
        return type.build();
    }

    public static Type CreateDictionary(Type keyType, Type valueType) {
        Type.Builder type = Type.newBuilder();
        type.setCode(TypeCode.DICTIONARY);
        type.addTypes(keyType);
        type.addTypes(valueType);
        return type.build();
    }
}
