import { Deprecations } from "./deprecation.js";
import { exceptionClass, isStreamRemoved, type ServerErrorDetails, StreamError } from "./errors.js";
import { register } from "./registry.js";
import { Enumeration, type RemoteEnum, type RemoteStruct, StructDeclaration } from "./types.js";

// The values of generated modules belong to no connection, so warn once per process
const deprecations = new Deprecations();
const warn = (message: string) => deprecations.warn(message);

/** Declares an enumeration of a service, for a generated module. Each value is a name in lower
 * camel case, its number, and the warning for a read of it when deprecated. Returns the object of
 * the values, which a connection uses where the server's definition matches. The type parameter
 * is the type of the object, from the module's declarations. */
export function declareEnumeration<T = Readonly<Record<string, RemoteEnum>>>(
  service: string,
  name: string,
  values: readonly (readonly [name: string, value: number, deprecation?: string])[],
  flags = false,
): T {
  const enumeration = new Enumeration(
    service,
    name,
    values.map(([name, value]) => ({ name, value })),
    flags,
  );
  const result: Record<string, RemoteEnum> = {};
  for (const [key, , deprecation] of values) {
    const value = enumeration.values[key];
    Object.defineProperty(
      result,
      key,
      deprecation === undefined
        ? { value, enumerable: true }
        : {
            get: () => {
              warn(deprecation);
              return value;
            },
            enumerable: true,
          },
    );
  }
  Object.freeze(result);
  register(service, name, { kind: "enumeration", enumeration, values: result });
  return result as T;
}

/** Declares a structure of a service, for a generated module. Each field is a name in lower
 * camel case, and the warning for a read of it when deprecated. Returns the class of the
 * structure's values, which a connection uses where the server's definition matches. */
export function declareStruct<T = new (fields: Readonly<Record<string, unknown>>) => RemoteStruct>(
  service: string,
  name: string,
  fields: readonly (readonly [name: string, deprecation?: string])[],
): T {
  const declaration = new StructDeclaration(
    service,
    name,
    fields.map(([name, deprecation]) => ({ name, deprecation })),
    warn,
  );
  register(service, name, { kind: "struct", declaration });
  return declaration.valueClass as T;
}

/** Declares an exception of a service, for a generated module, given the JavaScript error it
 * extends, or undefined for a ServerError. Returns its class, which a connection uses where the
 * server maps the exception to the same JavaScript error. */
export function declareException<T = new (error: ServerErrorDetails) => Error>(
  service: string,
  name: string,
  native?: ErrorConstructor,
): T {
  if (isStreamRemoved(service, name)) {
    return StreamError as T;
  }
  const cls = exceptionClass(name, native);
  register(service, name, { kind: "exception", cls, native });
  return cls as T;
}
