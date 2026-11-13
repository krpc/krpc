import type { BlockStatement, Expression as SyntaxExpression } from "acorn";
import type { KRPCCall } from "../functions.js";
import type { RemoteObject } from "../objects.js";
import type { Service } from "../schema/krpc_pb.js";
import type { Type, Types } from "../types.js";
import type { TypeLike } from "../typevalues.js";
import { Builder } from "./builder.js";
import { Emitter } from "./emit.js";
import { ExpressionCompiler } from "./expressions.js";
import { ClientValue, type Node } from "./ir.js";
import { MemberIndex } from "./members.js";
import { parseFunction } from "./parse.js";
import { Scope } from "./scope.js";
import { BOOL, sameType, typeName } from "./typing.js";

/** What the compiler reads from a connection, and the calls it builds functions with. */
export interface CompilerContext {
  readonly call: KRPCCall;
  readonly types: Types;
  readonly services: readonly Service[];
  readonly warn: (message: string) => void;
}

/** How a compiled function is used, which limits what it can be. */
export type Use = "compile" | "run" | "stream" | "event";

/** A compiled function, with the types of its parameters and value. */
export interface CompiledFunction {
  readonly expression: RemoteObject;
  readonly parameters: readonly Type[];
  readonly result: Type | null;
}

// The members of each connection's services, built on its first compile
const indexes = new WeakMap<Types, MemberIndex>();

/** Compiles a JavaScript function into a server side function. The function is first built
 * into a typed tree, which raises every CompilationError, and then on the server. */
export async function compileFunction(
  context: CompilerContext,
  fn: (...args: any[]) => unknown,
  captures: object,
  parameterTypes: readonly TypeLike[],
  use: Use,
): Promise<CompiledFunction> {
  const tree = buildTree(context, fn, captures, parameterTypes, use);
  const emitter = new Emitter(context.call);
  const expression = await emitter.node(tree.root);
  return {
    expression,
    parameters: tree.parameters.map((x) => x.type as Type),
    result: tree.root.type,
  };
}

interface Tree {
  readonly root: Node;
  readonly parameters: readonly Node[];
}

function buildTree(
  context: CompilerContext,
  fn: (...args: any[]) => unknown,
  captures: object,
  parameterTypes: readonly TypeLike[],
  use: Use,
): Tree {
  if (typeof fn !== "function") {
    throw new TypeError(`${String(fn)} is not a function`);
  }
  if (typeof captures !== "object" || captures === null) {
    throw new TypeError("The captured values are an object, such as { vessel }");
  }
  let members = indexes.get(context.types);
  if (members === undefined) {
    members = new MemberIndex(context.services, context.types);
    indexes.set(context.types, members);
  }
  const parsed = parseFunction(fn);
  const builder = new Builder({ types: context.types, members, warn: context.warn }, parsed.offset);
  const scope = new Scope(builder, parsed, captures, parameterTypes);
  const node = parsed.node;
  if ((use === "stream" || use === "event" || use === "run") && scope.parameters.length > 0) {
    const what =
      use === "run"
        ? "A function run with no arguments"
        : `The function of ${use === "stream" ? "a stream" : "an event"}`;
    throw builder.error(
      node,
      `${what} cannot take parameters. Compile it with compile, and pass its parameter types`,
    );
  }
  const body = returnedExpression(builder, node.body);
  const compiler = new ExpressionCompiler(builder, scope);
  const value = compiler.expression(body);
  let root: Node;
  if (value instanceof ClientValue) {
    root = builder.toNode(value, undefined, body);
  } else {
    root = value;
  }
  if (use === "stream" || use === "event") {
    if (builder.remoteCalls === 0) {
      throw builder.error(
        node,
        "The function makes no remote procedure call, so its value is the same on every update",
      );
    }
  }
  if (use === "event" && (root.type === null || !sameType(root.type, BOOL))) {
    throw builder.error(
      body,
      `The function of an event returns a bool, and this one returns ${typeName(root.type)}`,
    );
  }
  if (scope.parameters.length > 0) {
    root = builder.node("Lambda", [scope.parameters, root], root.type);
  }
  return { root, parameters: scope.parameters };
}

/** The expression a function returns: its body, or the expression of a body that is one return
 * statement. */
function returnedExpression(
  builder: Builder,
  body: BlockStatement | SyntaxExpression,
): SyntaxExpression {
  if (body.type !== "BlockStatement") {
    return body;
  }
  const [statement] = body.body;
  if (body.body.length === 1 && statement.type === "ReturnStatement" && statement.argument) {
    return statement.argument;
  }
  throw builder.error(
    body,
    "A function body of statements is not supported yet. Write the body as one expression",
  );
}
