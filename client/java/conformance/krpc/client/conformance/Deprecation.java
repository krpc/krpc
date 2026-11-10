package krpc.client.conformance;

import java.lang.reflect.AnnotatedElement;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.util.Locale;
import krpc.conformance.Testee.DeprecationSignals;
import krpc.conformance.Testee.Items;
import krpc.conformance.Testee.Value;

/** The {@code @Deprecated} annotation on the generated stub of a member, which javac warns from. */
final class Deprecation {
  private Deprecation() {}

  static Value signals(DeprecationSignals command) throws ClassNotFoundException {
    if (command.getReason()) {
      throw new Testee.UnsupportedException("@Deprecated has no reason");
    }
    if (!command.getUse().isEmpty()) {
      throw new Testee.UnsupportedException("A stream or a call names its method in a string");
    }
    Items.Builder items = Items.newBuilder();
    if (element(command.getService(), command.getMember()).isAnnotationPresent(Deprecated.class)) {
      items.addItems(Value.newBuilder().setString("Deprecated"));
    }
    return Value.newBuilder().setList(items).build();
  }

  private static AnnotatedElement element(String service, String member)
      throws ClassNotFoundException {
    Class<?> serviceClass = Class.forName("krpc.client.services." + service);
    if (member.isEmpty()) {
      return serviceClass;
    }
    String[] parts = member.split("\\.");
    Class<?> type = nestedClass(serviceClass, parts[0]);
    if (parts.length == 1) {
      return type != null ? type : member(serviceClass, parts[0]);
    }
    return member(type, parts[1]);
  }

  private static Class<?> nestedClass(Class<?> outer, String name) {
    for (Class<?> nested : outer.getClasses()) {
      if (nested.getSimpleName().equals(name)) {
        return nested;
      }
    }
    return null;
  }

  // A member by its name on the server: an enumeration value, a method or a getter
  private static AnnotatedElement member(Class<?> type, String name) {
    for (Field field : type.getFields()) {
      if (field.isEnumConstant() && matches(field.getName(), name)) {
        return field;
      }
    }
    for (Method method : type.getMethods()) {
      if (matches(method.getName(), name) || method.getName().equals("get" + name)) {
        return method;
      }
    }
    throw new IllegalArgumentException(type.getName() + " has no member " + name);
  }

  private static boolean matches(String javaName, String name) {
    return javaName.replace("_", "").toLowerCase(Locale.ROOT).equals(name.toLowerCase(Locale.ROOT));
  }
}
