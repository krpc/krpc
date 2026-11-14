import { CompilationError } from "../errors.js";
import type { KRPCCall } from "../functions.js";
import type { RemoteObject } from "../objects.js";
import type { Service } from "../schema/krpc_pb.js";
import type { Type, Types } from "../types.js";
import type { TypeLike } from "../typevalues.js";
import { Builder, type Environment, Recompile } from "./builder.js";
import { CollectionCompiler } from "./collections.js";
import { Emitter } from "./emit.js";
import { ExpressionCompiler } from "./expressions.js";
import type { Node } from "./ir.js";
import { MemberIndex } from "./members.js";
import { parseFunction } from "./parse.js";
import { Scope } from "./scope.js";
import { StatementCompiler } from "./statements.js";
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
    result: tree.result,
  };
}

interface Tree {
  readonly root: Node;
  readonly result: Type | null;
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
  const env = { types: context.types, members, warn: context.warn };
  // A use that types a value with none, such as an empty array, notes the type, and the
  // function is compiled again with it
  const hints = new Map<string, Type>();
  for (let attempt = 0; ; attempt++) {
    try {
      return buildOnce(env, parsed, captures, parameterTypes, use, hints);
    } catch (error) {
      if (!(error instanceof Recompile)) {
        throw error;
      }
      if (attempt >= MAX_COMPILES) {
        throw new CompilationError(
          `The function has more than ${MAX_COMPILES} values typed by a later use. Give some a type with a cast`,
        );
      }
    }
  }
}

// The most compiles of a function, each with the types of more values noted
const MAX_COMPILES = 100;

function buildOnce(
  env: Environment,
  parsed: ReturnType<typeof parseFunction>,
  captures: object,
  parameterTypes: readonly TypeLike[],
  use: Use,
  hints: Map<string, Type>,
): Tree {
  const builder = new Builder(env, parsed.offset, hints);
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
  const expressions = new ExpressionCompiler(builder, scope);
  const statements = new StatementCompiler(builder, scope, expressions, node.body);
  const collections = new CollectionCompiler(builder, expressions, statements);
  expressions.statements = statements;
  expressions.collections = collections;
  statements.collections = collections;
  let built: ReturnType<StatementCompiler["topLevel"]>;
  try {
    built = statements.topLevel(node, scope.parameters);
  } catch (error) {
    // An error after a read of a variable with no type comes of reading it as null
    if (error instanceof CompilationError && statements.untypedRead !== null) {
      throw statements.untypedRead;
    }
    throw error;
  }
  if (statements.untypedRead !== null) {
    throw statements.untypedRead;
  }
  const { root, result } = built;
  if (use === "stream" || use === "event") {
    if (builder.remoteCalls === 0 && !statements.hasState) {
      throw builder.error(
        node,
        "The function makes no remote procedure call, so its value is the same on every update",
      );
    }
  }
  if (use === "event" && (result === null || !sameType(result, BOOL))) {
    throw builder.error(
      node,
      `The function of an event returns a bool, and this one returns ${typeName(result)}`,
    );
  }
  return { root, result, parameters: scope.parameters };
}
