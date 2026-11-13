// The connection the native tests share, and the checks each compile goes through
import assert from "node:assert/strict";
import { sameType, typeName } from "../src/compiler/typing.js";
import { connect, contextOf, type DynamicConnection } from "../src/connection.js";
import type { Expression } from "../src/services/KRPC.js";
import type { Type } from "../src/types.js";
import type { TypeLike } from "../src/typevalues.js";

export type Fn = (captures: any, ...parameters: any[]) => unknown;

/** Connects to the TestServer the harness started, over WebSockets. */
export function connectToServer(): Promise<DynamicConnection> {
  return connect({
    name: "js_client_test",
    port: Number(process.env.KRPC_TEST_TCPIP_PORT ?? "50000"),
  }) as Promise<DynamicConnection>;
}

/** Compiles a function, and checks that the types the compiler gives its parameters and value
 * are those the server gives them. The client encodes arguments and decodes results with the
 * compiler's types. */
export async function compile(
  conn: DynamicConnection,
  fn: Fn,
  captures: object = {},
  parameters: readonly TypeLike[] = [],
): Promise<Expression> {
  const expression = await conn.compile(fn, captures, parameters);
  const functions = contextOf(conn).functions;
  const [compiled, server] = await Promise.all([
    functions.typesOf(expression),
    functions.readTypes(expression),
  ]);
  const same = (a: Type | null, b: Type | null) =>
    a === null || b === null ? a === b : sameType(a, b);
  assert.ok(
    same(compiled.result, server.result),
    `The compiler gives the function the type ${typeName(compiled.result)}, and the server ${typeName(server.result)}`,
  );
  assert.equal(compiled.parameters.length, server.parameters.length);
  compiled.parameters.forEach((x, i) => {
    assert.ok(
      same(x, server.parameters[i]),
      `The compiler gives parameter ${i} the type ${typeName(x)}, and the server ${typeName(server.parameters[i])}`,
    );
  });
  return expression;
}

/** Compiles a function with no parameters, checking its types, and runs it. */
export async function run(
  conn: DynamicConnection,
  fn: Fn,
  captures: object = {},
): Promise<unknown> {
  return conn.run(await compile(conn, fn, captures));
}

/** Whether two values are the same, with NaN equal to itself and 0 equal to -0. */
export function same(a: unknown, b: unknown): boolean {
  if (typeof a === "number" && typeof b === "number") {
    return a === b || (Number.isNaN(a) && Number.isNaN(b));
  }
  return a === b;
}

/** Compiles a function of constants and parameters only, and checks that it gives the result
 * JavaScript gives for each set of arguments. */
export async function onBothSides(
  conn: DynamicConnection,
  fn: Fn,
  parameters: readonly TypeLike[],
  argumentLists: readonly (readonly unknown[])[],
): Promise<void> {
  const expression = await compile(conn, fn, {}, parameters);
  for (const args of argumentLists) {
    const expected = fn({}, ...args);
    const actual = await conn.run(expression, ...args);
    assert.ok(
      same(actual, expected),
      `${fn} with ${args.map(String).join(", ")}: the server gives ${String(actual)}, and JavaScript ${String(expected)}`,
    );
  }
}

/** The tree of a compiled function, as the TestServer prints it. */
export async function tree(
  conn: DynamicConnection,
  fn: Fn,
  captures: object = {},
  parameters: readonly TypeLike[] = [],
): Promise<string> {
  return conn.testService.dumpExpressionTree(await compile(conn, fn, captures, parameters));
}
