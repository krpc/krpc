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
  | ReadonlyMap<number, Node>
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

/** The result of compiling an expression: a node of the server's tree, or a value the client
 * computed. */
export type Value = Node | ClientValue;
