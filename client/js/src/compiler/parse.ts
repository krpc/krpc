import {
  type ArrowFunctionExpression,
  type Expression,
  type FunctionExpression,
  type Node,
  parseExpressionAt,
} from "acorn";
import { CompilationError } from "../errors.js";

/** A function's syntax tree, and the column its source starts at on the first line of the text
 * that was parsed. */
export interface ParsedFunction {
  readonly node: ArrowFunctionExpression | FunctionExpression;
  readonly offset: number;
}

// The parse of each function, as a function can be compiled again with other captures
const parsed = new WeakMap<object, ParsedFunction>();

/** The syntax tree of a function, from its source. Throws a CompilationError for a function
 * with no source, such as a built-in one, and for an async function or a generator. */
export function parseFunction(fn: (...args: any[]) => unknown): ParsedFunction {
  let result = parsed.get(fn);
  if (result === undefined) {
    result = parseSource(Function.prototype.toString.call(fn));
    parsed.set(fn, result);
  }
  return result;
}

function parseSource(source: string): ParsedFunction {
  if (/\{\s*\[native code\]\s*\}\s*$/.test(source)) {
    throw new CompilationError("A built-in or bound function has no source to compile");
  }
  let node: Expression;
  let offset = 0;
  try {
    node = parse(source);
  } catch {
    // A method shorthand, such as `f() {}`, is the source of a method of an object
    const prefix = "({";
    try {
      const object = parse(`${prefix}${source}})`);
      if (object.type !== "ObjectExpression" || object.properties[0]?.type !== "Property") {
        throw new SyntaxError();
      }
      node = object.properties[0].value as Expression;
      offset = prefix.length;
    } catch {
      throw new CompilationError("The function's source does not parse as a function");
    }
  }
  if (node.type === "ClassExpression") {
    throw new CompilationError("A class cannot be compiled");
  }
  if (node.type !== "ArrowFunctionExpression" && node.type !== "FunctionExpression") {
    throw new CompilationError("The function's source does not parse as a function");
  }
  if (node.async) {
    throw at(node, offset, "An async function cannot be compiled");
  }
  if (node.generator) {
    throw at(node, offset, "A generator cannot be compiled");
  }
  return { node, offset };
}

function parse(text: string): Expression {
  const node = parseExpressionAt(text, 0, { ecmaVersion: "latest", locations: true });
  if (text.slice(node.end).trim() !== "") {
    throw new SyntaxError("Text after the function");
  }
  return node;
}

/** A CompilationError at a node of a function's source. */
export function at(node: Node, offset: number, message: string): CompilationError {
  const start = node.loc?.start;
  if (start === undefined) {
    return new CompilationError(message);
  }
  const column = start.line === 1 ? start.column - offset : start.column;
  return new CompilationError(message, start.line, column + 1);
}
