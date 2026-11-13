// The conformance testee for the JavaScript client. Reads commands from the conformance driver
// on standard input, runs them against the client, and writes the replies to standard output.
// See testee.proto in the conformance suite for the protocol.
//
// Each named task of the protocol is a chain of async tasks. Commands on one chain run in order,
// and chains interleave at each await.

import * as readline from "node:readline";
import {
  create,
  fromJsonString,
  isMessage,
  type MessageInitShape,
  toBinary,
  toJson,
} from "@bufbuild/protobuf";
import { decode, encode } from "../src/codec.js";
import { type Connection, connect, contextOf, type DynamicConnection } from "../src/connection.js";
import {
  ConnectionError,
  EncodingError,
  RemoteError,
  ServerError,
  type ServerErrorDetails,
} from "../src/errors.js";
import { camelCase } from "../src/naming.js";
import { getObjectId, RemoteObject } from "../src/objects.js";
import {
  ProcedureCallSchema,
  ServicesSchema,
  Type_TypeCode as TypeCode,
  TypeSchema,
} from "../src/schema/krpc_pb.js";
import { RemoteEnum, RemoteStruct, type Type } from "../src/types.js";
import { equals } from "../src/values.js";
import {
  type Command,
  CommandSchema,
  type Connect,
  Error_Kind as ErrorKind,
  type Error as ErrorMessage,
  ErrorSchema,
  type Invoke,
  type Members,
  OutputSchema,
  type Reply,
  ReplySchema,
  type Value,
  ValueSchema,
} from "./testee_pb.js";

const PROFILES = [
  "core",
  "error-types",
  "dynamic",
  "definitions",
  "defaults",
  "named-arguments",
  "connections",
  "concurrency",
];

/** The error classes an error can be an instance of, by name. */
const NATIVE_ERRORS: [string, new (...args: never[]) => object][] = [
  ["RemoteError", RemoteError],
  ["ServerError", ServerError],
  ["Error", Error],
  ["TypeError", TypeError],
  ["RangeError", RangeError],
  ["ReferenceError", ReferenceError],
  ["SyntaxError", SyntaxError],
  ["EvalError", EvalError],
  ["URIError", URIError],
];

/** The testee offers no way to do what a command asks. */
class Unsupported extends Error {}

/** The open connections, by the name the driver gives them. The default connection has the
 * empty name. */
const connections = new Map<string, DynamicConnection>();
/** The objects the testee holds, by handle. Each object a call returns gets a new handle, so two
 * handles can refer to one server object. */
const objects = new Map<bigint, RemoteObject>();
let nextHandle = 1n;
/** The end of the chain of each task, which the task's next command runs after. */
const tasks = new Map<string, Promise<void>>();

/** Stands for a negative zero in JSON, which JSON.stringify writes as 0. */
const NEGATIVE_ZERO = "\u0000-0";

function write(message: MessageInitShape<typeof OutputSchema>): void {
  const json = toJson(OutputSchema, create(OutputSchema, message));
  const text = JSON.stringify(json, (_, x) => (Object.is(x, -0) ? NEGATIVE_ZERO : x));
  process.stdout.write(`${text.replaceAll(JSON.stringify(NEGATIVE_ZERO), "-0")}\n`);
}

function connection(command: Command): DynamicConnection {
  const conn = connections.get(command.connection);
  if (conn === undefined) {
    throw new ConnectionError("Not connected");
  }
  return conn;
}

/** The value of a service member, by the name the server gives it. */
function serviceType(conn: DynamicConnection, service: string, name: string): any {
  const members = conn[camelCase(service)];
  if (members === undefined || !(name in members)) {
    throw new Unsupported(`No ${service}.${name}`);
  }
  return members[name];
}

function fromValue(conn: DynamicConnection, value: Value | undefined): unknown {
  if (value === undefined) {
    return null;
  }
  const content = value.value;
  switch (content.case) {
    case undefined:
    case "null":
      return null;
    case "object": {
      const obj = content.value;
      if (obj.handle === 0n) {
        const cls = serviceType(conn, obj.service, obj.name);
        return new cls(obj.id);
      }
      const result = objects.get(obj.handle);
      if (result === undefined) {
        throw new Error(`No object with handle ${obj.handle}`);
      }
      return result;
    }
    case "enum": {
      const values: Record<string, RemoteEnum> = serviceType(
        conn,
        content.value.service,
        content.value.name,
      );
      const result = Object.values(values).find((x) => x.value === content.value.value);
      if (result === undefined) {
        throw new TypeError(
          `${content.value.service}.${content.value.name} has no value ${content.value.value}`,
        );
      }
      return result;
    }
    case "list":
    case "tuple":
      return content.value.items.map((x) => fromValue(conn, x));
    case "set":
      return new Set(content.value.items.map((x) => fromValue(conn, x)));
    case "dictionary":
      return new Map(
        content.value.entries.map((x) => [fromValue(conn, x.key), fromValue(conn, x.value)]),
      );
    case "struct": {
      const { service, name, fields } = content.value;
      const struct = contextOf(conn).types.struct(`${service}.${name}`);
      const values: Record<string, unknown> = {};
      fields.forEach((field, i) => {
        // A field past the end of the structure, for a test that passes one too many
        const key = struct.fields[i]?.name ?? `field${i}`;
        values[key] = fromValue(conn, field);
      });
      return new struct.valueClass(values);
    }
    case "event":
    case "message":
      throw new Unsupported(`No ${content.case} arguments`);
    default:
      return content.value;
  }
}

/** A value as a conformance Value. The type the value was decoded as, where the testee knows it,
 * tells a floating point number from an integer. */
function toValue(value: unknown, type?: Type): Value {
  return create(ValueSchema, { value: toValueCase(value, type) });
}

function toValueCase(
  value: unknown,
  type: Type | undefined,
): MessageInitShape<typeof ValueSchema>["value"] {
  if (value === undefined) {
    return { case: undefined };
  }
  if (value === null) {
    return { case: "null", value: true };
  }
  switch (typeof value) {
    case "boolean":
      return { case: "bool", value };
    case "string":
      return { case: "string", value };
    case "bigint":
      return value < 0n ? { case: "sint64", value } : { case: "uint64", value };
    case "number": {
      // The driver compares numbers by value, whatever their type
      const floating = type?.kind === "double" || type?.kind === "float";
      if (!floating && Number.isSafeInteger(value) && !Object.is(value, -0)) {
        return { case: "sint64", value: BigInt(value) };
      }
      return { case: "double", value };
    }
  }
  if (value instanceof Uint8Array) {
    return { case: "bytes", value };
  }
  if (value instanceof RemoteObject) {
    const handle = nextHandle++;
    objects.set(handle, value);
    return { case: "object", value: { handle, id: getObjectId(value) } };
  }
  if (value instanceof RemoteEnum) {
    const { service, name } = value.enumeration;
    return { case: "enum", value: { service, name, value: value.value } };
  }
  if (Array.isArray(value)) {
    const items = value.map((x, i) => toValue(x, itemType(type, i)));
    return { case: "list", value: { items } };
  }
  if (value instanceof Set) {
    return { case: "set", value: { items: [...value].map((x) => toValue(x, itemType(type, 0))) } };
  }
  if (value instanceof Map) {
    const [key, item] = type?.kind === "dictionary" ? [type.key, type.value] : [];
    return {
      case: "dictionary",
      value: {
        entries: [...value].map(([k, v]) => ({ key: toValue(k, key), value: toValue(v, item) })),
      },
    };
  }
  if (isMessage(value, ServicesSchema)) {
    return { case: "message", value: toBinary(ServicesSchema, value) };
  }
  if (isMessage(value, ProcedureCallSchema)) {
    return { case: "message", value: toBinary(ProcedureCallSchema, value) };
  }
  if (value instanceof RemoteStruct) {
    const fields = Object.values(value).map((x, i) =>
      toValue(x, type?.kind === "struct" ? type.struct.fields[i]?.type : undefined),
    );
    return { case: "struct", value: { fields } };
  }
  throw new Error(`No conformance value for ${String(value)}`);
}

/** The type of an item of a list, set or tuple. */
function itemType(type: Type | undefined, index: number): Type | undefined {
  switch (type?.kind) {
    case "list":
    case "set":
      return type.item;
    case "tuple":
      return type.items[index];
    default:
      return undefined;
  }
}

/** The type a procedure returns, or undefined for none. */
function resultType(conn: Connection, command: Invoke): Type | undefined {
  const context = contextOf(conn);
  const procedure = context.services
    .find((x) => x.name === command.service)
    ?.procedures.find((x) => x.name === command.procedure);
  return procedure?.returnType === undefined ? undefined : context.types.get(procedure.returnType);
}

/** Calls a procedure, given the name the server gives it. The arguments include the instance of
 * an instance method. */
async function invoke(conn: DynamicConnection, command: Invoke): Promise<unknown> {
  const name = command.procedure;
  const args = command.arguments.map((x) => fromValue(conn, x));
  if (command.namedArguments.length > 0) {
    const named: Record<string, unknown> = {};
    for (const x of command.namedArguments) {
      named[camelCase(x.name)] = fromValue(conn, x.value);
    }
    args.push(named);
  }
  const service = () => {
    const result = conn[camelCase(command.service)];
    if (result === undefined) {
      throw new Unsupported(`No service ${command.service}`);
    }
    return result;
  };
  let target: any;
  let member: string;
  if (name.startsWith("get_") || name.startsWith("set_")) {
    target = service();
    member = name.slice(0, 3) + name.slice(4);
  } else if (name.includes("_")) {
    const memberName = name.slice(name.lastIndexOf("_") + 1);
    if (name.includes("_static_")) {
      if (command.through !== undefined) {
        throw new Unsupported("A static method is called through its class");
      }
      target = service()[name.slice(0, name.indexOf("_"))];
      member = camelCase(memberName);
    } else {
      target = args.shift();
      member = camelCase(memberName);
      if (name.includes("_get_")) {
        member = `get${memberName}`;
      } else if (name.includes("_set_")) {
        member = `set${memberName}`;
      }
    }
  } else {
    target = service();
    member = camelCase(name);
  }
  if (target === null || target === undefined || typeof target[member] !== "function") {
    throw new Unsupported(`No member ${member} for ${command.service}.${name}`);
  }
  return await target[member](...args);
}

/** Connecting the default connection closes every connection, and connecting a named one
 * closes only the connection of that name. */
async function connectCommand(command: Connect, name: string): Promise<void> {
  if (name === "") {
    const all = [...connections.values()];
    connections.clear();
    objects.clear();
    await Promise.all(all.map((x) => x.close()));
  } else {
    await connections.get(name)?.close();
    connections.delete(name);
  }
  if (command.protocol !== "websockets") {
    throw new Unsupported(`No protocol ${command.protocol}`);
  }
  const conn = await connect({
    name: command.name,
    address: command.address || undefined,
    port: command.port || undefined,
    handshakeTimeout:
      command.handshakeTimeout === undefined ? undefined : command.handshakeTimeout * 1000,
  });
  connections.set(name, conn);
}

function members(conn: DynamicConnection, command: Members): Value {
  let names: string[];
  if (command.service === "") {
    names = Object.keys(conn);
  } else if (command.name === "") {
    names = Object.keys(conn[camelCase(command.service)] ?? {});
  } else {
    const member = serviceType(conn, command.service, command.name);
    if (typeof member === "function" && member.prototype instanceof RemoteObject) {
      const statics = Object.keys(member);
      names = [...statics, ...Object.keys(member.prototype)];
    } else {
      names = Object.keys(member);
    }
  }
  return toValue(names.sort());
}

/** The client's type for a codec type name, such as "list(nullable uint32)". */
function codecType(conn: Connection, name: string): Type {
  return contextOf(conn).types.get(create(TypeSchema, codecProtobufType(name)));
}

function codecProtobufType(name: string): MessageInitShape<typeof TypeSchema> {
  if (name.startsWith("nullable ")) {
    return { ...codecProtobufType(name.slice(9)), nullable: true };
  }
  const match = /^(\w+)\((.*)\)$/.exec(name);
  if (match !== null) {
    const code = {
      list: TypeCode.LIST,
      set: TypeCode.SET,
      tuple: TypeCode.TUPLE,
      dictionary: TypeCode.DICTIONARY,
    }[match[1]];
    if (code === undefined) {
      throw new Unsupported(`No codec type ${name}`);
    }
    return {
      code,
      types: splitTypes(match[2]).map((x) => create(TypeSchema, codecProtobufType(x))),
    };
  }
  if (name === "TestClass") {
    return { code: TypeCode.CLASS, service: "TestService", name };
  }
  if (name === "TestEnum") {
    return { code: TypeCode.ENUMERATION, service: "TestService", name };
  }
  if (name.startsWith("Test")) {
    return { code: TypeCode.STRUCT, service: "TestService", name };
  }
  const code = TypeCode[name.toUpperCase() as keyof typeof TypeCode];
  if (code === undefined) {
    throw new Unsupported(`No codec type ${name}`);
  }
  return { code };
}

/** The comma separated type names in a list of them, which may nest. */
function splitTypes(names: string): string[] {
  const result: string[] = [];
  let depth = 0;
  let start = 0;
  for (let i = 0; i < names.length; i++) {
    if (names[i] === "(") {
      depth++;
    } else if (names[i] === ")") {
      depth--;
    } else if (names[i] === "," && depth === 0) {
      result.push(names.slice(start, i));
      start = i + 1;
    }
  }
  result.push(names.slice(start));
  return result;
}

/** Fails the next write to any connection, as a failed socket write does. */
function failWrite(message: string): void {
  const send = WebSocket.prototype.send;
  WebSocket.prototype.send = () => {
    WebSocket.prototype.send = send;
    throw new Error(message);
  };
}

async function dispatch(command: Command): Promise<Value> {
  const content = command.command;
  switch (content.case) {
    case "connect":
      await connectCommand(content.value, command.connection);
      return toValue(undefined);
    case "close":
      if (content.value.interrupted) {
        throw new Unsupported("No thread to interrupt");
      }
      await connection(command).close();
      return toValue(undefined);
    case "failWrite":
      failWrite(content.value.message);
      return toValue(undefined);
    case "invoke": {
      const conn = connection(command);
      return toValue(await invoke(conn, content.value), resultType(conn, content.value));
    }
    case "compare": {
      const conn = connection(command);
      return toValue(equals(fromValue(conn, content.value.a), fromValue(conn, content.value.b)));
    }
    case "hash":
      // JavaScript has no hash of a value
      return toValue(null);
    case "objectId":
      return create(ValueSchema, {
        value: {
          case: "uint64",
          value: getObjectId(fromValue(connection(command), content.value.a) as RemoteObject),
        },
      });
    case "members":
      return members(connection(command), content.value);
    case "encode": {
      const conn = connection(command);
      const value = fromValue(conn, content.value.value);
      return toValue(encode(codecType(conn, content.value.type), value));
    }
    case "decode": {
      const conn = connection(command);
      const type = codecType(conn, content.value.type);
      return toValue(decode(type, content.value.data, contextOf(conn)), type);
    }
    default:
      throw new Unsupported(`No ${content.case} command`);
  }
}

/** Whether an error is of the class of an exception the server maps to a JavaScript error, which
 * carries the server's details. */
function isMappedException(error: unknown): error is Error & ServerErrorDetails {
  return (
    error instanceof Error && typeof (error as Partial<ServerErrorDetails>).exception === "string"
  );
}

function toError(error: unknown): ErrorMessage {
  const message = error instanceof Error ? error.message : String(error);
  const result = create(ErrorSchema, {
    message,
    nativeTypes: NATIVE_ERRORS.filter(([, cls]) => error instanceof cls).map(([x]) => x),
  });
  if (error instanceof Unsupported) {
    result.kind = ErrorKind.UNSUPPORTED;
  } else if (error instanceof ConnectionError) {
    result.kind = ErrorKind.CONNECTION;
    result.status = error.status;
    result.reason = error.reason;
  } else if (error instanceof ServerError || isMappedException(error)) {
    result.kind = ErrorKind.SERVER;
    result.service = error.service;
    result.name = error.exception;
  } else if (error instanceof EncodingError) {
    result.kind = ErrorKind.ENCODING;
  } else if (error instanceof TypeError || error instanceof RangeError) {
    result.kind = ErrorKind.ARGUMENT;
  }
  if (error instanceof Error && error.cause !== undefined) {
    const cause = error.cause;
    result.cause = cause instanceof Error ? `${cause.name}: ${cause.message}` : String(cause);
  }
  return result;
}

async function run(command: Command, task: string): Promise<void> {
  const reply: Reply = create(ReplySchema, { id: command.id, task });
  try {
    reply.result = { case: "value", value: await dispatch(command) };
  } catch (error) {
    reply.result = { case: "error", value: toError(error) };
  }
  write({ content: { case: "reply", value: reply } });
}

async function main(): Promise<void> {
  write({
    content: {
      case: "hello",
      value: { client: "js", profiles: PROFILES, protocols: ["websockets"] },
    },
  });
  const lines = readline.createInterface({
    input: process.stdin,
    crlfDelay: Number.POSITIVE_INFINITY,
  });
  for await (const line of lines) {
    const command = fromJsonString(CommandSchema, line.replace(/\r$/, ""));
    const task = command.task || "main";
    const previous = tasks.get(task) ?? Promise.resolve();
    tasks.set(
      task,
      previous.then(() => run(command, task)),
    );
  }
  await Promise.all(tasks.values());
  await Promise.all([...connections.values()].map((x) => x.close()));
}

main().then(
  () => process.exit(0),
  (error: unknown) => {
    process.stderr.write(
      `${error instanceof Error ? (error.stack ?? error.message) : String(error)}\n`,
    );
    process.exit(1);
  },
);
