package krpc.client;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

@Retention(RetentionPolicy.RUNTIME)
@Target({ ElementType.METHOD })
@SuppressWarnings("checkstyle:abbreviationaswordinname")
public @interface RPCInfo {
  /** The name of the service. */
  String service();

  /** The name of the procedure. */
  String procedure();

  /** Parameter and return types. */
  Class<?> types();
}
