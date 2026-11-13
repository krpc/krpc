import { create } from "@bufbuild/protobuf";
import { decode, encode, type ObjectFactory } from "./codec.js";
import { EncodingError } from "./errors.js";
import { RemoteObject } from "./objects.js";
import type { Service } from "./schema/krpc_pb.js";
import {
  type Type as ProtobufType,
  Type_TypeCode as TypeCode,
  TypeSchema,
} from "./schema/krpc_pb.js";
import type { Expression } from "./services/KRPC.js";
import type { Event, Stream, StreamManager } from "./streams.js";
import { type RemoteEnum, type Type, type Types, UnknownTypeError } from "./types.js";
import type { TypeLike } from "./typevalues.js";

/** A JavaScript function to compile into a server side function. Its first parameter receives
 * the captured values. */
export type JSFunction = (...args: any[]) => unknown;

/** Calls a procedure of the KRPC service, and returns its result. */
export type KRPCCall = (procedure: string, args: readonly unknown[]) => Promise<unknown>;

/** The types of a server side function's parameters, and of its value or null for none. */
export interface FunctionTypes {
  readonly parameters: readonly Type[];
  readonly result: Type | null;
}

const EXPRESSION: Type = { kind: "class", service: "KRPC", name: "Expression", nullable: false };

// The type codes of the types a service defines, and of those that hold other types
const DEFINED = new Set([TypeCode.CLASS, TypeCode.ENUMERATION, TypeCode.STRUCT]);
const COLLECTIONS = new Set([TypeCode.TUPLE, TypeCode.LIST, TypeCode.SET, TypeCode.DICTIONARY]);

/** The server side functions of a connection, built with the KRPC.Expression API. */
export class Functions {
  readonly #call: KRPCCall;
  readonly #types: Types;
  readonly #streams: StreamManager;
  readonly #objects: ObjectFactory;
  readonly #services: readonly Service[];
  readonly #warn: (message: string) => void;
  // Read from the server once per function, or given by the compiler
  readonly #functionTypes = new WeakMap<RemoteObject, Promise<FunctionTypes>>();

  constructor(
    call: KRPCCall,
    types: Types,
    streams: StreamManager,
    objects: ObjectFactory,
    services: readonly Service[],
    warn: (message: string) => void,
  ) {
    this.#call = call;
    this.#types = types;
    this.#streams = streams;
    this.#objects = objects;
    this.#services = services;
    this.#warn = warn;
  }

  /** Compiles a JavaScript function into a server side function, for the given use. The
   * compiler is loaded on the first call. */
  async #compileFunction(
    fn: JSFunction,
    captures: object | undefined,
    parameters: readonly TypeLike[] | undefined,
    use: "compile" | "run" | "stream" | "event",
  ): Promise<Expression> {
    const { compileFunction } = await import("./compiler/index.js");
    const compiled = await compileFunction(
      { call: this.#call, types: this.#types, services: this.#services, warn: this.#warn },
      fn,
      captures ?? {},
      parameters ?? [],
      use,
    );
    const expression = compiled.expression as Expression;
    this.#functionTypes.set(
      expression,
      Promise.resolve({ parameters: compiled.parameters, result: compiled.result }),
    );
    return expression;
  }

  async compile(
    fn: Expression | JSFunction,
    captures?: object,
    parameters?: readonly TypeLike[],
  ): Promise<Expression> {
    if (typeof fn === "function") {
      fn = await this.#compileFunction(fn, captures, parameters, "compile");
    }
    check(fn);
    await this.#call("CompileFunction", [fn]);
    return fn;
  }

  /** Runs a JavaScript function on the server once, with the given captured values. */
  async runFunction(fn: JSFunction, captures?: object): Promise<unknown> {
    return this.run(await this.#compileFunction(fn, captures, undefined, "run"), []);
  }

  async run(fn: Expression, args: readonly unknown[]): Promise<unknown> {
    check(fn);
    const { parameters, result } = await this.typesOf(fn);
    if (args.length !== parameters.length) {
      throw new TypeError(
        `The function takes ${parameters.length} arguments, and was passed ${args.length}`,
      );
    }
    const encoded = args.map((value, i) => {
      if (value === undefined) {
        throw new TypeError(`Argument ${i} of the function is undefined`);
      }
      if (value === null) {
        if (!parameters[i].nullable) {
          throw new TypeError(`Argument ${i} of the function cannot be null`);
        }
        return null;
      }
      return encode(parameters[i], value);
    });
    const data = (await this.#call("RunFunction", [fn, encoded])) as Uint8Array | null;
    if (result === null) {
      return undefined;
    }
    if (data === null) {
      if (!result.nullable) {
        throw new EncodingError("The function returned null, which its type cannot hold");
      }
      return null;
    }
    return decode(result, data, this.#objects);
  }

  async addStream<T>(fn: Expression | JSFunction, captures?: object): Promise<Stream<T>> {
    if (typeof fn === "function") {
      fn = await this.#compileFunction(fn, captures, undefined, "stream");
    }
    check(fn);
    const { parameters, result } = await this.typesOf(fn);
    if (parameters.length > 0) {
      throw new TypeError("A stream of a function with parameters has no arguments to pass");
    }
    return await this.#streams.addWith<T>("AddFunctionStream", encode(EXPRESSION, fn), result);
  }

  async addEvent(fn: Expression | JSFunction, captures?: object): Promise<Event> {
    if (typeof fn === "function") {
      fn = await this.#compileFunction(fn, captures, undefined, "event");
    }
    check(fn);
    const { parameters } = await this.typesOf(fn);
    if (parameters.length > 0) {
      throw new TypeError("An event of a function with parameters has no arguments to pass");
    }
    return (await this.#call("AddEvent", [fn])) as Event;
  }

  /** The types of a function, read once. Not part of the public API. */
  typesOf(fn: Expression): Promise<FunctionTypes> {
    let types = this.#functionTypes.get(fn);
    if (types === undefined) {
      types = this.readTypes(fn);
      this.#functionTypes.set(fn, types);
      // A failed read is tried again on the next use
      types.catch(() => this.#functionTypes.delete(fn));
    }
    return types;
  }

  /** The types of a function, read from the server with no cache. Not part of the public
   * API. */
  async readTypes(fn: Expression): Promise<FunctionTypes> {
    const [parameters, hasResult] = (await Promise.all([
      this.#call("Expression_get_ParameterTypes", [fn]),
      this.#call("Expression_get_HasReturnType", [fn]),
    ])) as [RemoteObject[], boolean];
    const [parameterTypes, result] = await Promise.all([
      Promise.all(parameters.map((x) => this.#typeOf(x))),
      hasResult
        ? this.#call("Expression_get_ReturnType", [fn]).then((x) => this.#typeOf(x as RemoteObject))
        : null,
    ]);
    return { parameters: parameterTypes, result };
  }

  /** The client's type for a KRPC.Type object. Each property is a call, so only those its type
   * code needs are read. */
  async #typeOf(type: RemoteObject): Promise<Type> {
    const message = await this.#message(type);
    try {
      return this.#types.get(message);
    } catch (error) {
      if (error instanceof UnknownTypeError) {
        throw new EncodingError(`The function has a type the client cannot use: ${error.message}`);
      }
      throw error;
    }
  }

  async #message(type: RemoteObject): Promise<ProtobufType> {
    const [code, nullable] = (await Promise.all([
      this.#call("Type_get_Code", [type]),
      this.#call("Type_get_Nullable", [type]),
    ])) as [RemoteEnum, boolean];
    const value: TypeCode = code.value;
    let service = "";
    let name = "";
    let types: ProtobufType[] = [];
    if (DEFINED.has(value)) {
      [service, name] = (await Promise.all([
        this.#call("Type_get_Service", [type]),
        this.#call("Type_get_Name", [type]),
      ])) as [string, string];
    } else if (COLLECTIONS.has(value)) {
      const items = (await this.#call("Type_get_Types", [type])) as RemoteObject[];
      types = await Promise.all(items.map((x) => this.#message(x)));
    }
    return create(TypeSchema, { code: value, service, name, types, nullable });
  }
}

/** Throws a TypeError for a value that is not a server side function. */
function check(fn: unknown): asserts fn is Expression {
  if (!RemoteObject.isInstance(fn, "KRPC", "Expression")) {
    throw new TypeError(`${String(fn)} is not a KRPC.Expression`);
  }
}
