package krpc.client;

import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.util.List;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;
import krpc.schema.KRPC;

/**
 * What encoding a structure needs from the generated class that defines it: the types of its
 * fields, in the order they are encoded in, and the method that builds one from their values.
 * A struct type carries only the service and the name of the structure, so this is found by
 * reflection, once per structure rather than for every value encoded or decoded.
 */
final class StructInfo {
  private static final ConcurrentMap<String, StructInfo> STRUCT_INFOS =
      new ConcurrentHashMap<>();

  final List<KRPC.Type> fieldTypes;
  final Method fromFieldValues;

  @SuppressWarnings("unchecked")
  private StructInfo(KRPC.Type type) {
    try {
      Class<?> structType = Class.forName(
          "krpc.client.services." + type.getService() + "$" + type.getName());
      fieldTypes = (List<KRPC.Type>) structType.getMethod("fieldTypes").invoke(null);
      fromFieldValues = structType.getMethod("fromFieldValues", Object[].class);
    } catch (ClassNotFoundException | NoSuchMethodException | IllegalAccessException
        | InvocationTargetException exn) {
      throw new EncodingException("Failed to find the fields of a struct", exn);
    }
  }

  /** The encoding of the structure a struct type names. */
  static StructInfo of(KRPC.Type type) {
    return STRUCT_INFOS.computeIfAbsent(
        type.getService() + "$" + type.getName(), name -> new StructInfo(type));
  }
}
