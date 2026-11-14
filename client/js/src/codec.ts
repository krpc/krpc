import { fromBinary, isMessage, type Message, toBinary } from "@bufbuild/protobuf";
import { BinaryReader, BinaryWriter, WireType } from "@bufbuild/protobuf/wire";
import { EncodingError } from "./errors.js";
import { RemoteObject } from "./objects.js";
import { DictionarySchema, ListSchema, SetSchema, TupleSchema } from "./schema/krpc_pb.js";
import {
  type Enumeration,
  RemoteEnum,
  RemoteStruct,
  type Struct,
  StructDeclaration,
  type Type,
} from "./types.js";

/** Makes the objects a decoded value holds, for a connection. */
export interface ObjectFactory {
  object(service: string, className: string, id: bigint): RemoteObject;
}

const RANGES = {
  sint32: [-(2n ** 31n), 2n ** 31n - 1n],
  uint32: [0n, 2n ** 32n - 1n],
  sint64: [-(2n ** 63n), 2n ** 63n - 1n],
  uint64: [0n, 2n ** 64n - 1n],
} as const;

// A byte order mark is part of the string
const utf8 = new TextDecoder("utf-8", { fatal: true, ignoreBOM: true });

/** Encodes a value of a type that is not null. Throws a TypeError for a value of another
 * type, and a RangeError for a number outside the range of the type. */
export function encode(type: Type, value: unknown): Uint8Array {
  if (type.kind === "message") {
    return toBinary(type.schema, message(type.schema.typeName, value));
  }
  const writer = new BinaryWriter();
  write(writer, type, value);
  return writer.finish();
}

/** Decodes a value of a type that is not null. Throws an EncodingError for malformed data. */
export function decode(type: Type, data: Uint8Array, objects: ObjectFactory): unknown {
  try {
    return read(type, data, objects);
  } catch (error) {
    if (error instanceof EncodingError) {
      throw error;
    }
    throw new EncodingError(`Failed to decode a value: ${errorMessage(error)}`, { cause: error });
  }
}

function write(writer: BinaryWriter, type: Type, value: unknown): void {
  switch (type.kind) {
    case "double":
      writer.double(toNumber(value));
      return;
    case "float": {
      const x = toNumber(value);
      if (Number.isFinite(x) && !Number.isFinite(Math.fround(x))) {
        throw new RangeError(`${x} is outside the range of a float`);
      }
      writer.float(Math.fround(x));
      return;
    }
    case "sint32":
      writer.sint32(Number(toInteger(value, type.kind)));
      return;
    case "uint32":
      writer.uint32(Number(toInteger(value, type.kind)));
      return;
    case "sint64":
      writer.sint64(toInteger(value, type.kind));
      return;
    case "uint64":
      writer.uint64(toInteger(value, type.kind));
      return;
    case "bool":
      if (typeof value !== "boolean") {
        throw new TypeError(`${describe(value)} is not a boolean`);
      }
      writer.bool(value);
      return;
    case "string":
      if (typeof value !== "string") {
        throw new TypeError(`${describe(value)} is not a string`);
      }
      writer.string(value);
      return;
    case "bytes":
      if (!(value instanceof Uint8Array)) {
        throw new TypeError(`${describe(value)} is not a Uint8Array`);
      }
      writer.bytes(value);
      return;
    case "class":
      if (!RemoteObject.isInstance(value, type.service, type.name)) {
        throw new TypeError(`${describe(value)} is not a ${type.service}.${type.name}`);
      }
      writer.uint64(RemoteObject.idOf(value));
      return;
    case "enumeration":
      writer.sint32(enumNumber(type.enumeration, value));
      return;
    case "struct":
      writeItems(writer, structFields(type.struct, value));
      return;
    case "list":
      if (!Array.isArray(value)) {
        throw new TypeError(`${describe(value)} is not an Array`);
      }
      writeItems(
        writer,
        value.map((x) => [type.item, x]),
      );
      return;
    case "set":
      if (!(value instanceof Set)) {
        throw new TypeError(`${describe(value)} is not a Set`);
      }
      writeItems(
        writer,
        [...value].map((x) => [type.item, x]),
      );
      return;
    case "tuple":
      if (!Array.isArray(value)) {
        throw new TypeError(`${describe(value)} is not an Array`);
      }
      if (value.length !== type.items.length) {
        throw new TypeError(`A tuple of ${type.items.length} items has ${value.length}`);
      }
      writeItems(
        writer,
        type.items.map((x, i) => [x, value[i]]),
      );
      return;
    case "dictionary":
      if (!(value instanceof Map)) {
        throw new TypeError(`${describe(value)} is not a Map`);
      }
      for (const [k, v] of value) {
        const entry = new BinaryWriter();
        entry.tag(1, WireType.LengthDelimited).bytes(item(type.key, k));
        entry.tag(2, WireType.LengthDelimited).bytes(item(type.value, v));
        writer.tag(1, WireType.LengthDelimited).bytes(entry.finish());
      }
      return;
    case "message":
      writer.raw(encode(type, value));
      return;
    case "stream":
    case "event":
      throw new TypeError(`A ${type.kind} is not an argument`);
  }
}

/** Writes the items of a collection, each as field 1 of the message the collection is. */
function writeItems(writer: BinaryWriter, items: [Type, unknown][]): void {
  for (const [type, value] of items) {
    writer.tag(1, WireType.LengthDelimited).bytes(item(type, value));
  }
}

/** An item of a collection or a field of a structure. An item that can hold null carries a
 * presence bool before its value. */
function item(type: Type, value: unknown): Uint8Array {
  const writer = new BinaryWriter();
  const isNull = value === null || value === undefined;
  if (type.nullable) {
    writer.bool(!isNull);
    if (isNull) {
      return writer.finish();
    }
  } else if (isNull) {
    throw new TypeError("A value that cannot be null is null");
  }
  write(writer, type, value);
  return writer.finish();
}

/** The number of an enumeration value. A value of another enumeration of the same service and
 * name, such as one a generated module declares, converts by its number. */
function enumNumber(enumeration: Enumeration, value: unknown): number {
  if (value instanceof RemoteEnum) {
    if (value.enumeration === enumeration) {
      return value.value;
    }
    const other = value.enumeration;
    if (other.service === enumeration.service && other.name === enumeration.name) {
      try {
        return enumeration.fromNumber(value.value).value;
      } catch (error) {
        throw new TypeError(errorMessage(error));
      }
    }
  }
  throw new TypeError(
    `${describe(value)} is not a value of ${enumeration.service}.${enumeration.name}`,
  );
}

/** The fields of a structure value, with their types. A value of the structure from another
 * connection, or from a generated module, passes where it has the same fields. */
function structFields(struct: Struct, value: unknown): [Type, unknown][] {
  if (
    !(value instanceof RemoteStruct) ||
    !StructDeclaration.same(RemoteStruct.structOf(value), struct.declaration)
  ) {
    throw new TypeError(`${describe(value)} is not a ${struct.service}.${struct.name}`);
  }
  return struct.fields.map((field) => [field.type, RemoteStruct.fieldOf(value, field.name)]);
}

function read(type: Type, data: Uint8Array, objects: ObjectFactory): unknown {
  switch (type.kind) {
    case "string":
      return utf8.decode(readScalar(data, (reader) => reader.bytes()));
    case "bytes":
      return readScalar(data, (reader) => reader.bytes());
    case "message":
      return fromBinary(type.schema, data);
    case "list":
      return items(ListSchema, data).map((x) => readItem(type.item, x, objects));
    case "set":
      return new Set(items(SetSchema, data).map((x) => readItem(type.item, x, objects)));
    case "tuple":
      return readTuple(type.items, data, objects);
    case "struct": {
      const values = readTuple(
        type.struct.fields.map((x) => x.type),
        data,
        objects,
      );
      const fields: Record<string, unknown> = {};
      type.struct.fields.forEach((field, i) => {
        fields[field.name] = values[i];
      });
      return new type.struct.valueClass(fields);
    }
    case "dictionary": {
      const result = new Map<unknown, unknown>();
      for (const entry of fromBinary(DictionarySchema, data).entries) {
        result.set(
          readItem(type.key, entry.key, objects),
          readItem(type.value, entry.value, objects),
        );
      }
      return result;
    }
    default:
      return readScalar(data, (reader) => readValue(reader, type, objects));
  }
}

/** Reads a value of a scalar, class or enumeration type, which is the whole of the data. */
function readValue(reader: BinaryReader, type: Type, objects: ObjectFactory): unknown {
  switch (type.kind) {
    case "double":
      return reader.double();
    case "float":
      return reader.float();
    case "sint32":
      return reader.sint32();
    case "uint32":
      return reader.uint32();
    case "sint64":
      return BigInt(reader.sint64());
    case "uint64":
      return BigInt(reader.uint64());
    case "bool":
      return reader.bool();
    case "class":
      return objects.object(type.service, type.name, BigInt(reader.uint64()));
    case "enumeration":
      return type.enumeration.fromNumber(reader.sint32());
    default:
      throw new EncodingError(`Cannot decode a ${type.kind} as a scalar`);
  }
}

function readScalar<T>(data: Uint8Array, readFn: (reader: BinaryReader) => T): T {
  if (data.length === 0) {
    throw new EncodingError("No data for a value");
  }
  const reader = new BinaryReader(data);
  const result = readFn(reader);
  if (reader.pos !== data.length) {
    throw new EncodingError("Data left over after a value");
  }
  return result;
}

function readItem(type: Type, data: Uint8Array, objects: ObjectFactory): unknown {
  if (!type.nullable) {
    return read(type, data, objects);
  }
  const reader = new BinaryReader(data);
  const present = reader.uint32();
  if (present > 1) {
    throw new EncodingError(`The presence bool of an item is ${present}`);
  }
  if (present === 0) {
    if (reader.pos !== data.length) {
      throw new EncodingError("A null item has a value");
    }
    return null;
  }
  return read(type, data.subarray(reader.pos), objects);
}

function readTuple(types: readonly Type[], data: Uint8Array, objects: ObjectFactory): unknown[] {
  const values = items(TupleSchema, data);
  if (values.length < types.length) {
    throw new EncodingError(`Expected ${types.length} items, got ${values.length}`);
  }
  // Items past the end of the type are from a later version of the server
  return types.map((x, i) => readItem(x, values[i], objects));
}

function items(
  schema: typeof ListSchema | typeof SetSchema | typeof TupleSchema,
  data: Uint8Array,
): Uint8Array[] {
  return fromBinary(schema, data).items;
}

function toNumber(value: unknown): number {
  if (typeof value === "number") {
    return value;
  }
  if (typeof value === "bigint") {
    return Number(value);
  }
  throw new TypeError(`${describe(value)} is not a number`);
}

function toInteger(value: unknown, kind: keyof typeof RANGES): bigint {
  let result: bigint;
  if (typeof value === "bigint") {
    result = value;
  } else if (typeof value === "number" && Number.isInteger(value)) {
    result = BigInt(value);
  } else {
    throw new TypeError(`${describe(value)} is not an integer`);
  }
  const [min, max] = RANGES[kind];
  if (result < min || result > max) {
    throw new RangeError(`${result} is outside the range of a ${kind}`);
  }
  return result;
}

function message(typeName: string, value: unknown): Message {
  if (!isMessage(value) || value.$typeName !== typeName) {
    throw new TypeError(`${describe(value)} is not a ${typeName} message`);
  }
  return value;
}

function describe(value: unknown): string {
  if (typeof value === "string") {
    return JSON.stringify(value);
  }
  try {
    return String(value);
  } catch {
    return typeof value;
  }
}

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}
