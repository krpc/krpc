import type { DescMessage } from "@bufbuild/protobuf";
import { EncodingError } from "./errors.js";
import { camelCase } from "./naming.js";
import {
  ProcedureCallSchema,
  type Type as ProtobufType,
  type Service,
  ServicesSchema,
  Type_TypeCode as TypeCode,
} from "./schema/krpc_pb.js";

/** The value types, carried as a single protobuf scalar. */
export type ScalarKind =
  | "double"
  | "float"
  | "sint32"
  | "sint64"
  | "uint32"
  | "uint64"
  | "bool"
  | "string"
  | "bytes";

/** A kRPC type, as the codec encodes and decodes values of it. A nullable type can also hold
 * null. */
export type Type = { readonly nullable: boolean } & (
  | { readonly kind: ScalarKind }
  | { readonly kind: "class"; readonly service: string; readonly name: string }
  | { readonly kind: "enumeration"; readonly enumeration: Enumeration }
  | { readonly kind: "struct"; readonly struct: Struct }
  | { readonly kind: "list" | "set"; readonly item: Type }
  | { readonly kind: "tuple"; readonly items: readonly Type[] }
  | { readonly kind: "dictionary"; readonly key: Type; readonly value: Type }
  | { readonly kind: "message"; readonly schema: DescMessage }
  | { readonly kind: "stream" | "event" }
);

/** The key of the property that tells the values of two enumerations, or two structures,
 * apart in their types. The property exists in types only. */
declare const valueBrand: unique symbol;

/** A value of an enumeration. Each value is one object, so values compare with `===`. The type
 * parameter names the enumeration, such as `"SpaceCenter.VesselSituation"`. */
export class RemoteEnum<E extends string = string> {
  declare readonly [valueBrand]?: E;
  /** The enumeration the value belongs to. */
  readonly enumeration: Enumeration;
  /** The name of the value, in lower camel case. */
  readonly name: string;
  /** The value's number. */
  readonly value: number;

  constructor(enumeration: Enumeration, name: string, value: number) {
    this.enumeration = enumeration;
    this.name = name;
    this.value = value;
    Object.freeze(this);
  }

  toString(): string {
    return `${this.enumeration.service}.${this.enumeration.name}.${this.name}`;
  }
}

/** An enumeration a service defines, with its values by name and by number. */
export class Enumeration {
  readonly service: string;
  readonly name: string;
  /** The values, by name in lower camel case. */
  readonly values: Readonly<Record<string, RemoteEnum>>;
  readonly #byNumber = new Map<number, RemoteEnum>();

  constructor(service: string, name: string, values: readonly { name: string; value: number }[]) {
    this.service = service;
    this.name = name;
    const byName: Record<string, RemoteEnum> = {};
    for (const x of values) {
      const value = new RemoteEnum(this, camelCase(x.name), x.value);
      byName[value.name] = value;
      this.#byNumber.set(x.value, value);
    }
    this.values = Object.freeze(byName);
  }

  /** The value with the given number. Throws an EncodingError for a number the enumeration
   * does not declare. */
  fromNumber(value: number): RemoteEnum {
    const result = this.#byNumber.get(value);
    if (result === undefined) {
      throw new EncodingError(`${this.service}.${this.name} has no value ${value}`);
    }
    return result;
  }
}

/** A structure a service defines. Its values are instances of its class, `valueClass`. */
export class Struct {
  readonly service: string;
  readonly name: string;
  /** The fields, in the order the structure declares them. */
  readonly fields: readonly { readonly name: string; readonly type: Type }[];
  /** The class of the structure's values. */
  readonly valueClass: new (
    fields: Readonly<Record<string, unknown>>,
  ) => RemoteStruct;

  constructor(service: string, name: string, fields: readonly { name: string; type: Type }[]) {
    this.service = service;
    this.name = name;
    this.fields = fields;
    const struct = this;
    const cls = class extends RemoteStruct {
      constructor(values: Readonly<Record<string, unknown>>) {
        super(struct, values);
      }
    };
    Object.defineProperty(cls, "name", { value: name });
    this.valueClass = cls;
  }
}

/** The base class of the values of structures. A value is frozen, with a property for each
 * field of its structure in lower camel case. The type parameter names the structure, such as
 * `"SpaceCenter.BoundingBox"`. */
export class RemoteStruct<S extends string = string> {
  declare readonly [valueBrand]?: S;
  readonly #struct: Struct;

  constructor(struct: Struct, values: Readonly<Record<string, unknown>>) {
    this.#struct = struct;
    if (typeof values !== "object" || values === null) {
      throw new TypeError(`A ${struct.service}.${struct.name} needs an object of its fields`);
    }
    for (const key of Object.keys(values)) {
      if (!struct.fields.some((field) => field.name === key)) {
        throw new TypeError(`A ${struct.service}.${struct.name} has no field ${key}`);
      }
    }
    const fields = this as unknown as Record<string, unknown>;
    for (const field of struct.fields) {
      if (!(field.name in values)) {
        throw new TypeError(`A ${struct.service}.${struct.name} needs the field ${field.name}`);
      }
      fields[field.name] = values[field.name];
    }
    Object.freeze(this);
  }

  /** The structure of a value. */
  static structOf(value: RemoteStruct): Struct {
    return value.#struct;
  }
}

/** A type a client cannot use: a type code from a later version of the protocol, or a type that
 * names a class, enumeration or structure no service defines. */
export class UnknownTypeError extends Error {}

/** Service definitions the client cannot build its services from. */
export class MalformedDefinitionsError extends Error {}

const SCALARS: ReadonlyMap<TypeCode, ScalarKind> = new Map([
  [TypeCode.DOUBLE, "double"],
  [TypeCode.FLOAT, "float"],
  [TypeCode.SINT32, "sint32"],
  [TypeCode.SINT64, "sint64"],
  [TypeCode.UINT32, "uint32"],
  [TypeCode.UINT64, "uint64"],
  [TypeCode.BOOL, "bool"],
  [TypeCode.STRING, "string"],
  [TypeCode.BYTES, "bytes"],
]);

const MESSAGES: ReadonlyMap<TypeCode, DescMessage> = new Map<TypeCode, DescMessage>([
  [TypeCode.PROCEDURE_CALL, ProcedureCallSchema],
  [TypeCode.SERVICES, ServicesSchema],
]);

/** The types the services of a server define, and the types built from them. */
export class Types {
  readonly #classes = new Set<string>();
  readonly #enumerations = new Map<string, Enumeration>();
  readonly #structs = new Map<string, Struct | null>();
  readonly #structDefinitions = new Map<string, Service["structs"][number]>();

  constructor(services: readonly Service[]) {
    for (const service of services) {
      for (const x of service.classes) {
        this.#classes.add(`${service.name}.${x.name}`);
      }
      for (const x of service.enumerations) {
        this.#enumerations.set(
          `${service.name}.${x.name}`,
          new Enumeration(service.name, x.name, x.values),
        );
      }
      for (const x of service.structs) {
        const key = `${service.name}.${x.name}`;
        if (this.#structDefinitions.has(key)) {
          throw new MalformedDefinitionsError(`Structure ${key} is defined twice`);
        }
        this.#structDefinitions.set(key, x);
      }
    }
  }

  /** The type for a protobuf type. Throws an UnknownTypeError for a type the client cannot
   * use, and a MalformedDefinitionsError for a malformed one. */
  get(type: ProtobufType): Type {
    const nullable = type.nullable;
    const scalar = SCALARS.get(type.code);
    if (scalar !== undefined) {
      return { kind: scalar, nullable };
    }
    const schema = MESSAGES.get(type.code);
    if (schema !== undefined) {
      return { kind: "message", schema, nullable };
    }
    const key = `${type.service}.${type.name}`;
    switch (type.code) {
      case TypeCode.STREAM:
        return { kind: "stream", nullable };
      case TypeCode.EVENT:
        return { kind: "event", nullable };
      case TypeCode.CLASS:
        if (!this.#classes.has(key)) {
          throw new UnknownTypeError(`No class ${key}`);
        }
        return { kind: "class", service: type.service, name: type.name, nullable };
      case TypeCode.ENUMERATION: {
        const enumeration = this.#enumerations.get(key);
        if (enumeration === undefined) {
          throw new UnknownTypeError(`No enumeration ${key}`);
        }
        return { kind: "enumeration", enumeration, nullable };
      }
      case TypeCode.STRUCT:
        return { kind: "struct", struct: this.struct(key), nullable };
      case TypeCode.LIST:
      case TypeCode.SET:
        return {
          kind: type.code === TypeCode.LIST ? "list" : "set",
          item: this.get(this.#item(type, 0, 1)),
          nullable,
        };
      case TypeCode.TUPLE:
        return { kind: "tuple", items: type.types.map((x) => this.get(x)), nullable };
      case TypeCode.DICTIONARY:
        return {
          kind: "dictionary",
          key: this.get(this.#item(type, 0, 2)),
          value: this.get(this.#item(type, 1, 2)),
          nullable,
        };
      default:
        throw new UnknownTypeError(`Unknown type code ${type.code}`);
    }
  }

  /** The structure with the given key, `Service.Name`. A structure with a field of a type the
   * client cannot use is unusable too. */
  struct(key: string): Struct {
    const known = this.#structs.get(key);
    if (known === null) {
      throw new UnknownTypeError(`Structure ${key} has a field of an unknown type`);
    }
    if (known !== undefined) {
      return known;
    }
    const definition = this.#structDefinitions.get(key);
    if (definition === undefined) {
      throw new UnknownTypeError(`No structure ${key}`);
    }
    const service = key.slice(0, key.indexOf("."));
    const fields: { name: string; type: Type }[] = [];
    const struct = new Struct(service, definition.name, fields);
    // Registered before its fields, as a field can name the structure it belongs to
    this.#structs.set(key, struct);
    try {
      for (const field of definition.fields) {
        if (field.type === undefined) {
          throw new UnknownTypeError(`Field ${field.name} of ${key} has no type`);
        }
        fields.push({ name: camelCase(field.name), type: this.get(field.type) });
      }
    } catch (error) {
      this.#structs.set(key, null);
      throw error;
    }
    Object.freeze(fields);
    return struct;
  }

  /** The enumeration with the given service and name, or undefined. */
  enumeration(service: string, name: string): Enumeration | undefined {
    return this.#enumerations.get(`${service}.${name}`);
  }

  #item(type: ProtobufType, index: number, count: number): ProtobufType {
    if (type.types.length !== count) {
      throw new MalformedDefinitionsError(`A collection type has ${type.types.length} item types`);
    }
    return type.types[index];
  }
}
