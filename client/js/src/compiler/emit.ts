import type { KRPCCall } from "../functions.js";
import type { RemoteObject } from "../objects.js";
import type { Type } from "../types.js";
import { type Argument, Node, TypeArgument } from "./ir.js";

const SCALAR_TYPES: Readonly<Record<string, string>> = {
  double: "Double",
  float: "Float",
  sint32: "Int",
  uint32: "UInt",
  sint64: "Long",
  uint64: "ULong",
  bool: "Bool",
  string: "String",
  bytes: "Bytes",
};

/** Builds the KRPC.Expression objects of a typed tree on the server. The children of a node are
 * built concurrently, so a tree takes about one round trip per level. */
export class Emitter {
  readonly #call: KRPCCall;
  readonly #nodes = new Map<Node, Promise<RemoteObject>>();
  // The KRPC.Type objects, by the name of the type
  readonly #types = new Map<string, Promise<RemoteObject>>();

  constructor(call: KRPCCall) {
    this.#call = call;
  }

  /** The server's expression for a node. A node is built once, however many nodes use it. */
  node(node: Node): Promise<RemoteObject> {
    let result = this.#nodes.get(node);
    if (result === undefined) {
      result = this.#build(node);
      this.#nodes.set(node, result);
    }
    return result;
  }

  async #build(node: Node): Promise<RemoteObject> {
    const args = await Promise.all(node.args.map((x) => this.#argument(x)));
    return (await this.#call(`Expression_static_${node.method}`, args)) as RemoteObject;
  }

  #argument(argument: Argument): unknown {
    if (argument instanceof Node) {
      return this.node(argument);
    }
    if (argument instanceof TypeArgument) {
      return this.type(argument.type);
    }
    if (Array.isArray(argument)) {
      return Promise.all(argument.map((x: Argument) => this.#argument(x)));
    }
    if (argument instanceof Set) {
      return Promise.all([...argument].map((x: Node) => this.node(x))).then((x) => new Set(x));
    }
    if (argument instanceof Map) {
      const entries = [...argument].map(
        async ([key, value]: [number | string, Node]) => [key, await this.node(value)] as const,
      );
      return Promise.all(entries).then((x) => new Map(x));
    }
    return argument;
  }

  /** The server's KRPC.Type object for a type. */
  type(type: Type): Promise<RemoteObject> {
    const key = JSON.stringify(typeKey(type));
    let result = this.#types.get(key);
    if (result === undefined) {
      result = this.#buildType(type);
      this.#types.set(key, result);
    }
    return result;
  }

  async #buildType(type: Type): Promise<RemoteObject> {
    if (type.nullable) {
      const inner = await this.type({ ...type, nullable: false });
      return this.#typeCall("NullableType", [inner]);
    }
    switch (type.kind) {
      case "class":
        return this.#typeCall("ClassType", [type.service, type.name]);
      case "enumeration":
        return this.#typeCall("EnumerationType", [type.enumeration.service, type.enumeration.name]);
      case "struct":
        return this.#typeCall("StructType", [type.struct.service, type.struct.name]);
      case "list":
        return this.#typeCall("ListType", [await this.type(type.item)]);
      case "set":
        return this.#typeCall("SetType", [await this.type(type.item)]);
      case "dictionary":
        return this.#typeCall(
          "DictionaryType",
          await Promise.all([this.type(type.key), this.type(type.value)]),
        );
      case "tuple":
        return this.#typeCall("TupleType", [
          await Promise.all(type.items.map((x) => this.type(x))),
        ]);
      default: {
        const name = SCALAR_TYPES[type.kind];
        if (name === undefined) {
          throw new TypeError(`A function cannot hold a value of type ${type.kind}`);
        }
        return this.#typeCall(name, []);
      }
    }
  }

  async #typeCall(name: string, args: unknown[]): Promise<RemoteObject> {
    return (await this.#call(`Type_static_${name}`, args)) as RemoteObject;
  }
}

/** A value naming a type, for the cache of type objects. */
function typeKey(type: Type): unknown {
  const nullable = type.nullable;
  switch (type.kind) {
    case "class":
      return ["class", type.service, type.name, nullable];
    case "enumeration":
      return ["enumeration", type.enumeration.service, type.enumeration.name, nullable];
    case "struct":
      return ["struct", type.struct.service, type.struct.name, nullable];
    case "list":
    case "set":
      return [type.kind, typeKey(type.item), nullable];
    case "dictionary":
      return [type.kind, typeKey(type.key), typeKey(type.value), nullable];
    case "tuple":
      return [type.kind, type.items.map(typeKey), nullable];
    default:
      return [type.kind, nullable];
  }
}
