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
  StreamError,
} from "../src/errors.js";
import { camelCase } from "../src/naming.js";
import { getObjectId, RemoteObject } from "../src/objects.js";
import { type Declared, registered } from "../src/registry.js";
import {
  ProcedureCallSchema,
  ServicesSchema,
  Type_TypeCode as TypeCode,
  TypeSchema,
} from "../src/schema/krpc_pb.js";
// The generated module of the test service, which declares its enumerations and structures
import * as TestService from "../src/services/TestService.js";
import { Event, Stream } from "../src/streams.js";
import { RemoteEnum, RemoteFlags, RemoteStruct, type Type } from "../src/types.js";
import { equals } from "../src/values.js";
import {
  type AddStream,
  type Behavior,
  type Command,
  CommandSchema,
  type Connect,
  type Declared as DeclaredCommand,
  type DeprecationSignals,
  Error_Kind as ErrorKind,
  type Error as ErrorMessage,
  ErrorSchema,
  type Invoke,
  type Members,
  type NotificationSchema,
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
  "declarations",
  "defaults",
  "named-arguments",
  "connections",
  "concurrency",
  "streams",
  "events",
  "callbacks",
];

/** The error classes an error can be an instance of, by name. */
const NATIVE_ERRORS: [string, new (...args: never[]) => object][] = [
  ["RemoteError", RemoteError],
  ["ServerError", ServerError],
  ["StreamError", StreamError],
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
/** The streams and events the testee holds, by handle, with the type of a stream's values. A copy
 * of a handle names the same object. */
const handles = new Map<bigint, { value: Stream<unknown> | Event; type: Type | undefined }>();
/** The functions that remove the callbacks the driver added, by callback id. */
const removers = new Map<bigint, () => void>();
/** The releases of each callback's blocked calls: those given ahead of a call, and the calls
 * waiting for one. */
const releases = new Map<bigint, { given: number; waiting: (() => void)[] }>();
/** The task whose command is running, while it runs without awaiting, or undefined. */
let currentTask: string | undefined;
/** The task a callback reports when the client calls it outside every command. */
const CALLBACK_TASK = "client";

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

/** The enumeration or structure of a service its generated module declares. */
function declaredType<K extends "enumeration" | "struct">(
  service: string,
  name: string,
  kind: K,
): Extract<Declared, { kind: K }> {
  const declared = registered(service, name);
  if (declared?.kind !== kind) {
    throw new Unsupported(`No generated ${service}.${name}`);
  }
  return declared as Extract<Declared, { kind: K }>;
}

/** Whether the client's type for an enumeration or structure, reached through the connection,
 * is the one its generated module declares. */
function isDeclared(conn: DynamicConnection, command: DeclaredCommand): boolean {
  const modules: Record<string, Record<string, unknown>> = { TestService };
  const generated = modules[command.service]?.[command.name];
  return generated !== undefined && serviceType(conn, command.service, command.name) === generated;
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
      const { service, name, value: number, declared } = content.value;
      const enumeration = declared
        ? declaredType(service, name, "enumeration").enumeration
        : contextOf(conn).types.enumeration(service, name);
      if (enumeration === undefined) {
        throw new Unsupported(`No ${service}.${name}`);
      }
      try {
        return enumeration.fromNumber(number);
      } catch (error) {
        // The client has no way to make the value, as for a wrong argument
        throw error instanceof EncodingError ? new TypeError(error.message) : error;
      }
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
      const { service, name, fields, declared } = content.value;
      const struct = declared
        ? declaredType(service, name, "struct").declaration
        : contextOf(conn).types.struct(`${service}.${name}`);
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
  if (value instanceof Event) {
    return { case: "event", value: addHandle(value, undefined) };
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
    const fields = RemoteStruct.structOf(value).fields.map((field, i) =>
      toValue(
        RemoteStruct.fieldOf(value, field.name),
        type?.kind === "struct" ? type.struct.fields[i]?.type : undefined,
      ),
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

/** A new handle for a stream or an event. */
function addHandle(value: Stream<unknown> | Event, type: Type | undefined): bigint {
  const handle = nextHandle++;
  handles.set(handle, { value, type });
  return handle;
}

function handleOf(handle: bigint): { value: Stream<unknown> | Event; type: Type | undefined } {
  const result = handles.get(handle);
  if (result === undefined) {
    throw new Error(`No stream or event with handle ${handle}`);
  }
  return result;
}

function streamOf(handle: bigint): Stream<unknown> {
  const { value } = handleOf(handle);
  if (!(value instanceof Stream)) {
    throw new Error(`Handle ${handle} is not a stream`);
  }
  return value;
}

function eventOf(handle: bigint): Event {
  const { value } = handleOf(handle);
  if (!(value instanceof Event)) {
    throw new Error(`Handle ${handle} is not an event`);
  }
  return value;
}

/** A timeout in seconds as the client's, in milliseconds. */
function milliseconds(timeout: number | undefined): number | undefined {
  return timeout === undefined ? undefined : timeout * 1000;
}

/** Adds a stream of a call. A scoped stream is read and removed in an await using block. */
async function addStream(conn: DynamicConnection, command: AddStream): Promise<Reply["result"]> {
  if (command.invoke === undefined) {
    throw new Error("No call to stream");
  }
  const call = resolve(conn, command.invoke);
  const type = resultType(conn, command.invoke);
  if (command.scoped) {
    await using stream = await conn.addStream(call);
    await stream.get();
    return { case: "value", value: toValue(null) };
  }
  const stream = await conn.addStream(call);
  return { case: "stream", value: addHandle(stream, type) };
}

type Call = MessageInitShape<typeof NotificationSchema>["call"];

/** The function the client calls for a callback the driver adds. It writes a notification for
 * each call, then does what the behavior says. A behavior that sleeps, blocks or runs a command
 * makes the callback return a promise. */
function callback(id: bigint, behavior: Behavior | undefined, call: (arg: unknown) => Call) {
  if (behavior?.action.case === "failFatally") {
    throw new Unsupported("JavaScript has no kind of error apart from its ordinary ones");
  }
  let calls = 0;
  return (arg?: unknown): unknown => {
    const task = currentTask ?? CALLBACK_TASK;
    write({ content: { case: "notification", value: { callback: id, call: call(arg), task } } });
    calls++;
    if (
      behavior === undefined ||
      calls <= behavior.skip ||
      (0 < behavior.only && behavior.only < calls)
    ) {
      return undefined;
    }
    const action = behavior.action;
    const returned = () => {
      if (behavior.reportReturn) {
        write({ content: { case: "notification", value: { callback: id, task, returned: true } } });
      }
    };
    if (behavior.sleep === 0 && action.case !== "block" && action.case !== "run") {
      try {
        if (action.case === "fail") {
          throw new Error(action.value);
        }
      } finally {
        returned();
      }
      return undefined;
    }
    return (async () => {
      try {
        if (behavior.sleep > 0) {
          await new Promise((resolve) => setTimeout(resolve, behavior.sleep * 1000));
        }
        if (action.case === "block") {
          await blocked(id);
        } else if (action.case === "run") {
          const result = await reply(action.value, task);
          write({ content: { case: "notification", value: { callback: id, task, result } } });
        } else if (action.case === "fail") {
          throw new Error(action.value);
        }
      } finally {
        returned();
      }
    })();
  };
}

/** Waits for a release of a callback's blocked call. */
function blocked(id: bigint): Promise<void> {
  const release = releasesOf(id);
  if (release.given > 0) {
    release.given--;
    return Promise.resolve();
  }
  return new Promise((resolve) => release.waiting.push(resolve));
}

function releasesOf(id: bigint): { given: number; waiting: (() => void)[] } {
  let release = releases.get(id);
  if (release === undefined) {
    release = { given: 0, waiting: [] };
    releases.set(id, release);
  }
  return release;
}

function release(id: bigint): void {
  const release = releasesOf(id);
  const waiting = release.waiting.shift();
  if (waiting === undefined) {
    release.given++;
  } else {
    waiting();
  }
}

function valueCall(type: Type | undefined): (value: unknown) => Call {
  return (value) => ({ case: "value", value: toValue(value, type) });
}

function endingCall(error: unknown): Call {
  return { case: "ending", value: { error: error === undefined ? undefined : toError(error) } };
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
  return await resolve(conn, command)();
}

/** A function that calls a procedure, given the name the server gives it. */
function resolve(conn: DynamicConnection, command: Invoke): () => Promise<unknown> {
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
  return () => target[member](...args);
}

/** Connecting the default connection closes every connection, and connecting a named one
 * closes only the connection of that name. */
async function connectCommand(command: Connect, name: string): Promise<void> {
  if (name === "") {
    const all = [...connections.values()];
    connections.clear();
    objects.clear();
    handles.clear();
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
  if (name === "TestEnum" || name === "TestFlags") {
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

/** A function that uses a member of a service: a call of a procedure, or a read of the service,
 * a type, a value of an enumeration or a field of a structure. */
function memberUse(conn: DynamicConnection, command: DeprecationSignals): () => unknown {
  if (command.use !== "" && (command.use !== "stream" || command.invoke === undefined)) {
    throw new Unsupported(`No ${command.use} of a procedure`);
  }
  if (command.invoke !== undefined) {
    const call = resolve(conn, command.invoke);
    return command.use === "stream" ? () => conn.addStream(call) : call;
  }
  const serviceName = camelCase(command.service);
  if (command.member === "") {
    return () => conn[serviceName];
  }
  const service = conn[serviceName];
  const [typeName, name] = command.member.split(".");
  if (name === undefined) {
    return () => service[typeName];
  }
  const type = service[typeName];
  const key = camelCase(name);
  if (typeof type === "function" && type.prototype instanceof RemoteStruct) {
    const struct = contextOf(conn).types.struct(`${command.service}.${typeName}`);
    const value = new type(Object.fromEntries(struct.fields.map((x) => [x.name, null])));
    return () => value[key];
  }
  return () => type[key];
}

/** The deprecation warnings a use of a member emits. */
async function deprecationSignals(
  conn: DynamicConnection,
  command: DeprecationSignals,
): Promise<Value> {
  const emitWarning = process.emitWarning;
  const signals: string[] = [];
  try {
    // Reaching the member is not part of its use
    process.emitWarning = () => {};
    const use = memberUse(conn, command);
    process.emitWarning = (warning: string | Error, ...rest: unknown[]) => {
      const type = typeof rest[0] === "string" ? rest[0] : undefined;
      if (type === "DeprecationWarning") {
        signals.push(String(warning));
      }
    };
    await use();
  } finally {
    process.emitWarning = emitWarning;
  }
  return toValue(signals);
}

/** A TestService.TestFlags value of a command. */
function flags(conn: DynamicConnection, value: Value | undefined): RemoteFlags {
  const result = fromValue(conn, value);
  if (!(result instanceof RemoteFlags)) {
    throw new TypeError(`${String(result)} is not a flags value`);
  }
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

async function dispatch(command: Command): Promise<Reply["result"]> {
  const content = command.command;
  switch (content.case) {
    case "addStream":
      return await addStream(connection(command), content.value);
    case "get": {
      const { value, type } = handleOf(content.value.stream);
      const stream = value instanceof Event ? value.stream : value;
      return { case: "value", value: toValue(await stream.get(), type) };
    }
    case "wait":
      return {
        case: "value",
        value: toValue(
          await streamOf(content.value.stream).wait(milliseconds(content.value.timeout)),
        ),
      };
    case "waitEvent":
      return {
        case: "value",
        value: toValue(
          await eventOf(content.value.event).wait(milliseconds(content.value.timeout)),
        ),
      };
    case "setRate":
      await streamOf(content.value.stream).setRate(content.value.rate);
      return { case: "value", value: toValue(undefined) };
    case "getRate":
      return {
        case: "value",
        value: create(ValueSchema, {
          value: { case: "float", value: streamOf(content.value.stream).rate },
        }),
      };
    case "remove": {
      const { value } = handleOf(content.value.stream);
      await value.remove();
      return { case: "value", value: toValue(undefined) };
    }
    case "copyStream": {
      const { value, type } = handleOf(content.value.stream);
      return { case: "stream", value: addHandle(value, type) };
    }
    case "eventStream":
      return {
        case: "stream",
        value: addHandle(eventOf(content.value.event).stream, { kind: "bool", nullable: false }),
      };
    case "scopeEvent": {
      const conn = connection(command);
      if (content.value.invoke === undefined) {
        throw new Error("No call");
      }
      const event = await invoke(conn, content.value.invoke);
      if (!(event instanceof Event)) {
        throw new Error("The call returned no event");
      }
      await using scoped = event;
      await scoped.stream.get();
      return { case: "value", value: toValue(null) };
    }
    case "compareHandles":
      return {
        case: "value",
        value: toValue(handleOf(content.value.a).value === handleOf(content.value.b).value),
      };
    case "hashHandle":
      // JavaScript has no hash of a value
      return { case: "value", value: toValue(null) };
    case "dropHandle":
      handles.delete(content.value.stream);
      (globalThis as { gc?: () => void }).gc?.();
      return { case: "value", value: toValue(undefined) };
    case "addCallback": {
      const { stream, callback: id, behavior } = content.value;
      const { type } = handleOf(stream);
      const target = streamOf(stream);
      const fn = callback(id, behavior, valueCall(type));
      removers.set(id, () => target.removeCallback(fn));
      target.addCallback(fn);
      return { case: "value", value: toValue(undefined) };
    }
    case "addEventCallback": {
      const { event, callback: id, behavior } = content.value;
      const target = eventOf(event);
      const fn = callback(id, behavior, () => ({ case: undefined }));
      removers.set(id, () => target.removeCallback(fn));
      target.addCallback(fn);
      return { case: "value", value: toValue(undefined) };
    }
    case "addEndCallback": {
      const { stream, callback: id, behavior } = content.value;
      const target = handleOf(stream).value;
      const fn = callback(id, behavior, endingCall);
      removers.set(id, () => target.removeEndCallback(fn));
      target.addEndCallback(fn);
      return { case: "value", value: toValue(undefined) };
    }
    case "addUpdateCallback": {
      const { callback: id, behavior } = content.value;
      const conn = connection(command);
      const fn = callback(id, behavior, () => ({ case: undefined }));
      removers.set(id, () => conn.removeUpdateCallback(fn));
      conn.addUpdateCallback(fn);
      return { case: "value", value: toValue(undefined) };
    }
    case "removeCallback":
    case "removeEndCallback":
    case "removeUpdateCallback":
      removers.get(content.value.callback)?.();
      return { case: "value", value: toValue(undefined) };
    case "release":
      release(content.value.callback);
      return { case: "value", value: toValue(undefined) };
    default:
      return { case: "value", value: await dispatchValue(command) };
  }
}

async function dispatchValue(command: Command): Promise<Value> {
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
    case "flagsUnion": {
      const conn = connection(command);
      return toValue(flags(conn, content.value.a).union(flags(conn, content.value.b)));
    }
    case "flagsContains": {
      const conn = connection(command);
      return toValue(flags(conn, content.value.a).contains(flags(conn, content.value.b)));
    }
    case "flagsDifference": {
      const conn = connection(command);
      return toValue(flags(conn, content.value.a).difference(flags(conn, content.value.b)));
    }
    case "deprecationSignals":
      return await deprecationSignals(connection(command), content.value);
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
    case "declared":
      return toValue(isDeclared(connection(command), content.value));
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
  } else if (error instanceof StreamError) {
    result.kind = ErrorKind.STREAM;
  } else if (error instanceof TypeError || error instanceof RangeError) {
    result.kind = ErrorKind.ARGUMENT;
  }
  if (error instanceof Error && error.cause !== undefined) {
    const cause = error.cause;
    result.cause = cause instanceof Error ? `${cause.name}: ${cause.message}` : String(cause);
  }
  return result;
}

/** Runs a command on a task, and returns the reply. A callback the client calls before the
 * command first awaits reports the task. */
async function reply(command: Command, task: string): Promise<Reply> {
  const result: Reply = create(ReplySchema, { id: command.id, task });
  const outer = currentTask;
  currentTask = task;
  let pending: Promise<Reply["result"]>;
  try {
    pending = dispatch(command);
  } finally {
    currentTask = outer;
  }
  try {
    result.result = await pending;
  } catch (error) {
    result.result = { case: "error", value: toError(error) };
  }
  return result;
}

async function run(command: Command, task: string): Promise<void> {
  write({ content: { case: "reply", value: await reply(command, task) } });
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
