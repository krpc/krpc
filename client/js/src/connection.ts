import { create, fromBinary, isMessage } from "@bufbuild/protobuf";
import { decode, encode, type ObjectFactory } from "./codec.js";
import {
  ConnectionError,
  EncodingError,
  exceptionClass,
  ServerError,
  type ServerErrorDetails,
} from "./errors.js";
import { accessorName, camelCase } from "./naming.js";
import { ObjectCache, RemoteObject, RemoteService } from "./objects.js";
import {
  type Argument,
  ArgumentSchema,
  type Procedure,
  type Service,
  ServicesSchema,
} from "./schema/krpc_pb.js";
import type { Services } from "./services/index.js";
import { Session } from "./session.js";
import {
  MalformedDefinitionsError,
  type Struct,
  type Type,
  Types,
  UnknownTypeError,
} from "./types.js";

/** The options of a connection. */
export interface ConnectOptions {
  /** A name for the connection, shown in the server window in the game. */
  name?: string;
  /** The address of the server. Defaults to `127.0.0.1`. */
  address?: string;
  /** The port of the server's WebSockets protocol. Defaults to 50000. */
  port?: number;
  /** The time to wait for the server to accept the connection, in milliseconds. Defaults to 5000.
   * `Infinity` waits for as long as it takes. */
  handshakeTimeout?: number;
}

// The type of a connection holds the services a kRPC release ships
export interface Connection extends Services {}

/** A connection to a kRPC server. It has a property for each service of the server, in lower
 * camel case, such as `conn.spaceCenter`. */
// biome-ignore lint/suspicious/noUnsafeDeclarationMerging: the connection defines the services at run time
export class Connection {
  readonly #session: Session;

  /** @internal */
  constructor(session: Session) {
    this.#session = session;
  }

  /** Closes the connection. Each call waiting for a response rejects with a ConnectionError.
   * Closing a closed connection does nothing. */
  close(): Promise<void> {
    return this.#session.close();
  }
}

/** A connection, typed to call any service the server has, such as one a mod adds. Its
 * members have no static types. */
export type DynamicConnection = Connection & { readonly [service: string]: any };

/** What the services of a connection need to make calls, and what the conformance testee
 * reads. */
export interface Context extends ObjectFactory {
  readonly session: Session;
  readonly types: Types;
  readonly services: readonly Service[];
}

const contexts = new WeakMap<Connection, Context>();

/** The context of a connection. Not part of the public API. */
export function contextOf(connection: Connection): Context {
  const context = contexts.get(connection);
  if (context === undefined) {
    throw new TypeError("Not a connection");
  }
  return context;
}

const DEFAULT_ADDRESS = "127.0.0.1";
const DEFAULT_PORT = 50000;
const DEFAULT_HANDSHAKE_TIMEOUT = 5000;

/** Connects to a kRPC server over WebSockets, and builds its services. Rejects with a
 * ConnectionError when the server cannot be reached, refuses the connection or sends service
 * definitions the client cannot build its services from. */
export async function connect(options: ConnectOptions = {}): Promise<Connection> {
  const timeout = options.handshakeTimeout ?? DEFAULT_HANDSHAKE_TIMEOUT;
  if (!(timeout > 0)) {
    throw new RangeError(`The handshake timeout ${timeout} is not positive`);
  }
  let address = options.address ?? DEFAULT_ADDRESS;
  if (address.includes(":") && !address.startsWith("[")) {
    address = `[${address}]`;
  }
  const url = `ws://${address}:${options.port ?? DEFAULT_PORT}/`;
  const session = await Session.open(url, options.name ?? "", timeout);
  try {
    const result = await session.call("KRPC", "GetServices", []);
    const services = fromBinary(ServicesSchema, result.value).services;
    const connection = new Connection(session);
    const objects = new ObjectCache();
    const classes = new Map<string, new (id: bigint) => RemoteObject>();
    const context: Context = {
      session,
      types: new Types(services),
      services,
      object(service, className, id) {
        const cls = classes.get(`${service}.${className}`);
        if (cls === undefined) {
          throw new EncodingError(`No class ${service}.${className}`);
        }
        return new cls(id);
      },
    };
    contexts.set(connection, context);
    for (const service of services) {
      for (const x of service.classes) {
        classes.set(`${service.name}.${x.name}`, remoteClass(objects, service.name, x.name));
      }
    }
    addClassMembers(context, classes);
    const exceptions = new Map<string, new (error: ServerErrorDetails) => Error>();
    for (const service of services) {
      for (const x of service.exceptions) {
        exceptions.set(`${service.name}.${x.name}`, exceptionClass(x.name, x.mappedType));
      }
    }
    session.errorFactory = (error) => {
      const cls = exceptions.get(`${error.service}.${error.exception}`);
      return cls === undefined ? new ServerError(error) : new cls(error);
    };
    for (const service of services) {
      const members = serviceMembers(context, service, classes, exceptions);
      if (members !== null) {
        Object.defineProperty(connection, camelCase(service.name), {
          value: members,
          enumerable: true,
        });
      }
    }
    return connection;
  } catch (error) {
    await session.close();
    if (error instanceof MalformedDefinitionsError) {
      throw new ConnectionError(
        `The server's service definitions are malformed: ${error.message}`,
        { cause: error },
      );
    }
    throw error;
  }
}

/** The class for a class of a service. Constructing it with an id gives the connection's
 * object for that id. */
function remoteClass(
  objects: ObjectCache,
  service: string,
  className: string,
): new (
  id: bigint,
) => RemoteObject {
  const cls = class extends RemoteObject {
    constructor(id: bigint | number) {
      super(objects, service, className, BigInt(id));
    }
  };
  Object.defineProperty(cls, "name", { value: className });
  return cls;
}

/** A procedure, with the types its arguments and result are encoded as. */
interface Signature {
  readonly service: string;
  readonly procedure: string;
  readonly parameters: readonly {
    readonly name: string;
    readonly type: Type;
    readonly optional: boolean;
  }[];
  readonly result: Type | null;
}

/** The signature of a procedure, or null for a procedure with a type the client cannot use. */
function signature(types: Types, service: string, procedure: Procedure): Signature | null {
  try {
    return {
      service,
      procedure: procedure.name,
      parameters: procedure.parameters.map((x) => {
        if (x.type === undefined) {
          throw new UnknownTypeError(`Parameter ${x.name} has no type`);
        }
        return { name: camelCase(x.name), type: types.get(x.type), optional: x.hasDefaultValue };
      }),
      result: procedure.returnType === undefined ? null : types.get(procedure.returnType),
    };
  } catch (error) {
    if (error instanceof UnknownTypeError) {
      return null;
    }
    throw error;
  }
}

/** Calls a procedure with arguments in the order of its parameters, then optionally a plain
 * object of arguments by name. An argument left out, or undefined, takes the parameter's default
 * value. The first parameter of a method, the object, has no name to pass it by. */
async function invoke(
  context: Context,
  signature: Signature,
  args: readonly unknown[],
  first = 0,
): Promise<unknown> {
  const { parameters } = signature;
  const resultKind = signature.result?.kind;
  if (resultKind === "stream" || resultKind === "event") {
    throw new TypeError(
      `${signature.procedure} returns a ${resultKind}, which this client does not support`,
    );
  }
  const values = [...args];
  const last = values.length > first ? values[values.length - 1] : undefined;
  if (isPlainObject(last)) {
    values.pop();
    nameArguments(signature, values, last as Record<string, unknown>, first);
  }
  if (values.length > parameters.length) {
    throw new TypeError(
      `${signature.procedure} takes ${parameters.length} arguments, and was passed ${values.length}`,
    );
  }
  const encoded: Argument[] = [];
  parameters.forEach((parameter, position) => {
    const value = values[position];
    if (value === undefined) {
      if (!parameter.optional) {
        throw new TypeError(`${signature.procedure} is missing argument ${parameter.name}`);
      }
    } else if (value === null) {
      if (!parameter.type.nullable) {
        throw new TypeError(`Argument ${parameter.name} of ${signature.procedure} cannot be null`);
      }
      encoded.push(create(ArgumentSchema, { position, isNull: true }));
    } else {
      encoded.push(create(ArgumentSchema, { position, value: encode(parameter.type, value) }));
    }
  });
  const result = await context.session.call(signature.service, signature.procedure, encoded);
  if (signature.result === null) {
    return undefined;
  }
  if (result.isNull) {
    if (!signature.result.nullable) {
      throw new EncodingError(`${signature.procedure} returned null, which its type cannot hold`);
    }
    if (result.value.length > 0) {
      throw new EncodingError(`${signature.procedure} returned null with a value`);
    }
    return null;
  }
  return decode(signature.result, result.value, context);
}

/** Whether a value is a plain object, which only arguments by name are. Every value of a
 * parameter is a primitive, an instance of a class or a protobuf message. */
function isPlainObject(value: unknown): boolean {
  if (typeof value !== "object" || value === null || isMessage(value)) {
    return false;
  }
  const prototype = Object.getPrototypeOf(value);
  return prototype === Object.prototype || prototype === null;
}

/** Puts each argument passed by name in its position. */
function nameArguments(
  signature: Signature,
  values: unknown[],
  named: Record<string, unknown>,
  first: number,
): void {
  for (const [name, value] of Object.entries(named)) {
    const position = signature.parameters.findIndex((x, i) => i >= first && x.name === name);
    if (position === -1) {
      throw new TypeError(`${signature.procedure} has no parameter ${name}`);
    }
    if (values[position] !== undefined) {
      throw new TypeError(`${signature.procedure} was passed argument ${name} twice`);
    }
    values[position] = value;
  }
}

/** Adds the methods, static methods and properties of each class, from the procedures of every
 * service. A member of a class can belong to another service. */
function addClassMembers(
  context: Context,
  classes: Map<string, new (id: bigint) => RemoteObject>,
): void {
  for (const service of context.services) {
    for (const procedure of service.procedures) {
      const name = procedure.name;
      if (
        procedure.internal ||
        !name.includes("_") ||
        name.startsWith("get_") ||
        name.startsWith("set_")
      ) {
        continue;
      }
      const sig = signature(context.types, service.name, procedure);
      if (sig === null) {
        continue;
      }
      const className = name.slice(0, name.indexOf("_"));
      const member = name.slice(name.lastIndexOf("_") + 1);
      if (name.includes("_static_")) {
        const cls = classes.get(`${service.name}.${className}`);
        if (cls !== undefined) {
          define(cls, camelCase(member), (...args: unknown[]) => invoke(context, sig, args));
        }
        continue;
      }
      const instance = sig.parameters[0]?.type;
      if (instance?.kind !== "class") {
        continue;
      }
      const cls = classes.get(`${instance.service}.${instance.name}`);
      if (cls === undefined) {
        continue;
      }
      let key = camelCase(member);
      if (name.includes("_get_")) {
        key = accessorName("get", member);
      } else if (name.includes("_set_")) {
        key = accessorName("set", member);
      }
      define(cls.prototype, key, function (this: RemoteObject, ...args: unknown[]) {
        return invoke(context, sig, [this, ...args], 1);
      });
    }
  }
}

/** The members of a service, or null for a service with none of its own. A member whose
 * procedure has a type the client cannot use is left out. */
function serviceMembers(
  context: Context,
  service: Service,
  classes: Map<string, new (id: bigint) => RemoteObject>,
  exceptions: Map<string, new (error: ServerErrorDetails) => Error>,
): object | null {
  const members: Record<string, unknown> = Object.create(RemoteService.prototype);
  let hasMembers =
    service.classes.length > 0 || service.enumerations.length > 0 || service.exceptions.length > 0;
  for (const procedure of service.procedures) {
    // An internal procedure is for client libraries only
    if (procedure.internal) {
      continue;
    }
    const name = procedure.name;
    const isClassMember =
      name.includes("_") && !name.startsWith("get_") && !name.startsWith("set_");
    if (isClassMember) {
      hasMembers ||= name.includes("_static_");
      continue;
    }
    hasMembers = true;
    const sig = signature(context.types, service.name, procedure);
    if (sig === null) {
      continue;
    }
    let key = camelCase(name);
    if (name.startsWith("get_")) {
      key = accessorName("get", name.slice(4));
    } else if (name.startsWith("set_")) {
      key = accessorName("set", name.slice(4));
    }
    members[key] = (...args: unknown[]) => invoke(context, sig, args);
  }
  for (const x of service.classes) {
    members[x.name] = classes.get(`${service.name}.${x.name}`);
  }
  for (const x of service.enumerations) {
    members[x.name] = context.types.enumeration(service.name, x.name)?.values;
  }
  for (const x of service.exceptions) {
    members[x.name] = exceptions.get(`${service.name}.${x.name}`);
  }
  for (const x of service.structs) {
    hasMembers = true;
    let struct: Struct;
    try {
      struct = context.types.struct(`${service.name}.${x.name}`);
    } catch (error) {
      if (error instanceof UnknownTypeError) {
        continue;
      }
      throw error;
    }
    members[x.name] = struct.valueClass;
  }
  return hasMembers ? Object.freeze(members) : null;
}

function define(target: object, key: string, value: unknown): void {
  Object.defineProperty(target, key, { value, enumerable: true });
}
