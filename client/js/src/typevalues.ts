import type { RemoteObject } from "./objects.js";
import type { RemoteEnum, RemoteStruct, ScalarKind } from "./types.js";

/** A type of a server side function's values, named in place of a TypeValue: a service's class,
 * such as `conn.spaceCenter.Vessel`, the object of an enumeration's values, such as
 * `SpaceCenter.SASMode`, or a structure's class. */
export type NamedType =
  | (abstract new (
      ...args: any[]
    ) => RemoteObject)
  | (abstract new (
      ...args: any[]
    ) => RemoteStruct)
  | { [Symbol.hasInstance](value: unknown): boolean }
  | Readonly<Record<string, RemoteEnum>>;

/** A type of a server side function's values: a TypeValue or a NamedType. */
export type TypeLike = TypeValue | NamedType;

/** What a TypeValue names. */
export type TypeDescriptor =
  | { readonly kind: ScalarKind }
  | { readonly kind: "list" | "set"; readonly item: TypeLike }
  | { readonly kind: "dictionary"; readonly key: TypeLike; readonly value: TypeLike }
  | { readonly kind: "tuple"; readonly items: readonly TypeLike[] }
  | { readonly kind: "nullable"; readonly type: TypeLike };

/** The type of a parameter of a compiled function, or of a value `cast` types. The type
 * parameter is the type of the values in the client. */
export class TypeValue<T = unknown> {
  declare readonly __value?: T;
  /** @internal */
  readonly descriptor: TypeDescriptor;

  /** @internal */
  constructor(descriptor: TypeDescriptor) {
    this.descriptor = descriptor;
    Object.freeze(this);
  }

  /** Returns the value. In a compiled function, gives the value this type, such as an empty
   * array no use types. */
  cast(value: T): T {
    return value;
  }
}

/** The value type of a TypeLike. */
export type ValueOf<T> =
  T extends TypeValue<infer V>
    ? V
    : T extends { [Symbol.hasInstance](value: unknown): value is infer I }
      ? I
      : T extends abstract new (
            ...args: any[]
          ) => infer I
        ? I
        : T extends Readonly<Record<string, infer E>>
          ? E
          : never;

/** The value types of a tuple of TypeLikes. */
export type ValuesOf<L extends readonly unknown[]> = { -readonly [K in keyof L]: ValueOf<L[K]> };

/** The types of the values of server side functions, for the parameters of a compiled function
 * and for `cast`. */
export const types = Object.freeze({
  double: new TypeValue<number>({ kind: "double" }),
  float: new TypeValue<number>({ kind: "float" }),
  int32: new TypeValue<number>({ kind: "sint32" }),
  uint32: new TypeValue<number>({ kind: "uint32" }),
  int64: new TypeValue<bigint>({ kind: "sint64" }),
  uint64: new TypeValue<bigint>({ kind: "uint64" }),
  bool: new TypeValue<boolean>({ kind: "bool" }),
  string: new TypeValue<string>({ kind: "string" }),
  bytes: new TypeValue<Uint8Array>({ kind: "bytes" }),
  /** A list of values of the given type, an array in the client. */
  list<T extends TypeLike>(item: T): TypeValue<ValueOf<T>[]> {
    return new TypeValue({ kind: "list", item });
  },
  /** A set of values of the given type. */
  set<T extends TypeLike>(item: T): TypeValue<Set<ValueOf<T>>> {
    return new TypeValue({ kind: "set", item });
  },
  /** A dictionary from keys of one type to values of another. */
  map<K extends TypeLike, V extends TypeLike>(
    key: K,
    value: V,
  ): TypeValue<Map<ValueOf<K>, ValueOf<V>>> {
    return new TypeValue({ kind: "dictionary", key, value });
  },
  /** A tuple of values of the given types, an array in the client. */
  tuple<T extends TypeLike[]>(...items: T): TypeValue<{ [K in keyof T]: ValueOf<T[K]> }> {
    return new TypeValue({ kind: "tuple", items });
  },
  /** The values of the given type, and null. */
  nullable<T extends TypeLike>(type: T): TypeValue<ValueOf<T> | null> {
    return new TypeValue({ kind: "nullable", type });
  },
});
