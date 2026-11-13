import { create } from "@bufbuild/protobuf";
import { namedOf } from "../registry.js";
import { Type_TypeCode as TypeCode, TypeSchema } from "../schema/krpc_pb.js";
import { type ScalarKind, type Type, type Types, UnknownTypeError } from "../types.js";
import { type TypeLike, TypeValue } from "../typevalues.js";

export function scalar(kind: ScalarKind, nullable = false): Type {
  return { kind, nullable };
}

export const DOUBLE = scalar("double");
export const BOOL = scalar("bool");
export const STRING = scalar("string");
export const SINT32 = scalar("sint32");
export const UINT32 = scalar("uint32");

// The server types whose values are a number in the client, and those that are a bigint
const NUMBERS: ReadonlySet<string> = new Set(["double", "float", "sint32", "uint32"]);
const BIGINTS: ReadonlySet<string> = new Set(["sint64", "uint64"]);

/** Whether the values of a type are a number in the client. */
export function isNumber(type: Type | null): boolean {
  return type !== null && NUMBERS.has(type.kind);
}

/** Whether the values of a type are a bigint in the client. */
export function isBigint(type: Type | null): boolean {
  return type !== null && BIGINTS.has(type.kind);
}

export function isKind(type: Type | null, kind: Type["kind"]): boolean {
  return type !== null && type.kind === kind;
}

/** A type with the given nullability. */
export function withNullable(type: Type, nullable: boolean): Type {
  return type.nullable === nullable ? type : { ...type, nullable };
}

/** Whether two types are the same, optionally ignoring whether they can hold null. */
export function sameType(a: Type, b: Type, ignoreNullable = false): boolean {
  if (!ignoreNullable && a.nullable !== b.nullable) {
    return false;
  }
  if (a.kind !== b.kind) {
    return false;
  }
  switch (a.kind) {
    case "class":
      return b.kind === "class" && a.service === b.service && a.name === b.name;
    case "enumeration":
      return (
        b.kind === "enumeration" &&
        a.enumeration.service === b.enumeration.service &&
        a.enumeration.name === b.enumeration.name
      );
    case "struct":
      return (
        b.kind === "struct" &&
        a.struct.service === b.struct.service &&
        a.struct.name === b.struct.name
      );
    case "list":
    case "set":
      return (b.kind === "list" || b.kind === "set") && sameType(a.item, b.item);
    case "tuple":
      return (
        b.kind === "tuple" &&
        a.items.length === b.items.length &&
        a.items.every((x, i) => sameType(x, b.items[i]))
      );
    case "dictionary":
      return b.kind === "dictionary" && sameType(a.key, b.key) && sameType(a.value, b.value);
    default:
      return true;
  }
}

const SCALAR_NAMES: Readonly<Record<string, string>> = {
  double: "double",
  float: "float",
  sint32: "int32",
  uint32: "uint32",
  sint64: "int64",
  uint64: "uint64",
  bool: "bool",
  string: "string",
  bytes: "bytes",
};

/** The name of a type in messages, as the `types` values name it. */
export function typeName(type: Type | null): string {
  if (type === null) {
    return "no value";
  }
  const name = baseName(type);
  return type.nullable ? `nullable ${name}` : name;
}

function baseName(type: Type): string {
  switch (type.kind) {
    case "class":
      return `${type.service}.${type.name}`;
    case "enumeration":
      return `${type.enumeration.service}.${type.enumeration.name}`;
    case "struct":
      return `${type.struct.service}.${type.struct.name}`;
    case "list":
    case "set":
      return `${type.kind}<${typeName(type.item)}>`;
    case "tuple":
      return `tuple<${type.items.map(typeName).join(", ")}>`;
    case "dictionary":
      return `map<${typeName(type.key)}, ${typeName(type.value)}>`;
    default:
      return SCALAR_NAMES[type.kind] ?? type.kind;
  }
}

/** The type of a class a service defines. Throws an UnknownTypeError for a class the server
 * does not define. */
export function classType(types: Types, service: string, name: string): Type {
  return types.get(create(TypeSchema, { code: TypeCode.CLASS, service, name }));
}

/** The type a TypeLike names. Throws a TypeError for a value that names no type, and an
 * UnknownTypeError for a type the server does not define. */
export function resolveType(types: Types, like: TypeLike): Type {
  if (like instanceof TypeValue) {
    const descriptor = like.descriptor;
    switch (descriptor.kind) {
      case "list":
      case "set":
        return {
          kind: descriptor.kind,
          item: resolveType(types, descriptor.item),
          nullable: false,
        };
      case "dictionary":
        return {
          kind: "dictionary",
          key: resolveType(types, descriptor.key),
          value: resolveType(types, descriptor.value),
          nullable: false,
        };
      case "tuple":
        return {
          kind: "tuple",
          items: descriptor.items.map((x) => resolveType(types, x)),
          nullable: false,
        };
      case "nullable":
        return withNullable(resolveType(types, descriptor.type), true);
      default:
        return scalar(descriptor.kind);
    }
  }
  const named = namedOf(like);
  switch (named?.kind) {
    case "class":
      return classType(types, named.service, named.name);
    case "enumeration": {
      const { service, name } = named.enumeration;
      const enumeration = types.enumeration(service, name);
      if (enumeration === undefined) {
        throw new UnknownTypeError(`No enumeration ${service}.${name}`);
      }
      return { kind: "enumeration", enumeration, nullable: false };
    }
    case "struct": {
      const { service, name } = named.declaration;
      return { kind: "struct", struct: types.struct(`${service}.${name}`), nullable: false };
    }
    default:
      throw new TypeError(
        `${String(like)} is not a type, such as types.double or a service's class`,
      );
  }
}
