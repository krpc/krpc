import type { Node as SyntaxNode } from "acorn";
import type { ProcedureCall } from "../schema/krpc_pb.js";
import type { Type } from "../types.js";

/** A server type, as an argument of a KRPC.Expression factory. */
export class TypeArgument {
  readonly type: Type;

  constructor(type: Type) {
    this.type = type;
  }
}

/** An argument of a KRPC.Expression factory. */
export type Argument =
  | Node
  | TypeArgument
  | readonly Node[]
  | readonly (readonly Node[])[]
  | ReadonlySet<Node>
  | ReadonlyMap<number | string, Node>
  | null
  | ProcedureCall
  | string
  | number
  | bigint
  | boolean
  | Uint8Array;

/** A node of the typed tree: a call of a KRPC.Expression factory, such as `Add`, with the type
 * of its value. A node used in several places, such as a variable, is one server node. */
export class Node {
  /** The factory, a static method of KRPC.Expression. */
  readonly method: string;
  readonly args: readonly Argument[];
  /** The type of the value, or null for a node with no value. */
  readonly type: Type | null;

  constructor(method: string, args: readonly Argument[], type: Type | null) {
    this.method = method;
    this.args = args;
    this.type = type;
  }

  /** Whether reading the node twice reads one value with no effects: a variable, a parameter
   * or a constant. */
  get isPlain(): boolean {
    return (
      this.method === "Variable" ||
      this.method === "Parameter" ||
      this.method === "StateVariable" ||
      this.method.startsWith("Constant")
    );
  }
}

/** A value computed on the client while compiling, such as a captured value or a constant. */
export class ClientValue {
  readonly value: unknown;

  constructor(value: unknown) {
    this.value = value;
  }
}

/** A collection written in the function, or captured, whose type comes from where it is used:
 * an array is a tuple where a tuple goes, and a list elsewhere. */
export class Collection {
  readonly form: "array" | "set" | "map";
  /** The values, or for a map, the values of its entries. */
  readonly items: readonly Operand[];
  /** The keys of a map's entries. */
  readonly keys: readonly Operand[];
  readonly at: SyntaxNode;

  constructor(
    form: "array" | "set" | "map",
    items: readonly Operand[],
    keys: readonly Operand[],
    at: SyntaxNode,
  ) {
    this.form = form;
    this.items = items;
    this.keys = keys;
    this.at = at;
  }
}

/** The result of compiling an expression: a node of the server's tree, or a value the client
 * computed. */
export type Value = Node | ClientValue;

/** A value, or a collection typed where it is used, as the operands that take a type from their
 * position are: arguments, assignments, returns and the values of collections. */
export type Operand = Value | Collection;
