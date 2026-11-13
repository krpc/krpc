import type { Node as SyntaxNode } from "acorn";
import type { Argument, Builder } from "./builder.js";
import { ClientValue, Node, type Value } from "./ir.js";
import { BOOL, DOUBLE, isBigint, isNumber, STRING, scalar, typeName } from "./typing.js";

/** The Math functions of one StdLib procedure, by name, with the procedure's name. */
const MATH_PROCEDURES: Readonly<Record<string, string>> = {
  abs: "Abs",
  floor: "Floor",
  ceil: "Ceiling",
  // Math.round rounds a half toward positive infinity
  round: "RoundHalfUp",
  sqrt: "Sqrt",
  sin: "Sin",
  cos: "Cos",
  tan: "Tan",
  asin: "Asin",
  acos: "Acos",
  atan: "Atan",
  atan2: "Atan2",
  log: "Log",
  log10: "Log10",
  exp: "Exp",
};

/** A call of a function of a builtin global, such as `Math.max`, or null for an object that is
 * no builtin global. */
export function builtinMethod(
  b: Builder,
  object: unknown,
  key: string,
  compileArgs: () => readonly Argument[],
  node: SyntaxNode,
): Value | null {
  if (!isBuiltin(object)) {
    return null;
  }
  const args = compileArgs();
  if (object === Math) {
    return math(b, key, args, node);
  }
  if (object === console) {
    if (key !== "log") {
      throw b.error(
        node,
        `console.${key} is not supported in a compiled function. Use console.log`,
      );
    }
    return log(b, args, node);
  }
  if (object === Number) {
    const procedure = { isNaN: "IsNan", isFinite: "IsFinite" }[key];
    if (procedure === undefined) {
      throw b.error(node, `Number.${key} is not supported in a compiled function`);
    }
    const [x] = numbers(b, `Number.${key}`, args, 1, node);
    return fold(args, (v) => Number[key as "isNaN"](v)) ?? b.stdlib(procedure, [x]);
  }
  const name = object === console ? "console" : (object as { name: string }).name;
  throw b.error(node, `${name}.${key} is not supported in a compiled function`);
}

/** A call of a builtin function, such as `String(x)`, or null for a function that is none. */
export function builtinFunction(
  b: Builder,
  fn: unknown,
  args: readonly Argument[],
  node: SyntaxNode,
): Value | null {
  if (fn === String) {
    return args.length === 0 ? new ClientValue("") : b.string(args[0].value, args[0].at);
  }
  if (fn === Boolean) {
    return args.length === 0 ? new ClientValue(false) : b.truth(args[0].value, args[0].at);
  }
  if (fn === Number) {
    if (args.length === 0) {
      return new ClientValue(0);
    }
    const { value, at } = args[0];
    if (value instanceof ClientValue) {
      return new ClientValue(Number(value.value));
    }
    b.notNull(value, at);
    const type = value.type;
    if (isNumber(type)) {
      return b.double(value, at);
    }
    if (isBigint(type)) {
      return b.cast(value, DOUBLE);
    }
    if (type?.kind === "bool") {
      return b.node(
        "Conditional",
        [value, b.constant(1, DOUBLE, at), b.constant(0, DOUBLE, at)],
        DOUBLE,
      );
    }
    throw b.error(at, `Number of a ${typeName(type)} is not supported in a compiled function`);
  }
  if (fn === BigInt) {
    if (args.length !== 1) {
      throw b.error(node, "BigInt takes one argument");
    }
    const { value, at } = args[0];
    if (value instanceof ClientValue) {
      try {
        return new ClientValue(BigInt(value.value as number));
      } catch (error) {
        throw b.error(at, (error as Error).message);
      }
    }
    b.notNull(value, at);
    if (isBigint(value.type)) {
      return value;
    }
    // JavaScript throws for a fraction, so only an integer type converts
    if (isNumber(value.type) && value.type?.kind !== "double" && value.type?.kind !== "float") {
      return b.cast(value, scalar("sint64"));
    }
    throw b.error(
      at,
      `BigInt of a ${typeName(value.type)} is not supported in a compiled function`,
    );
  }
  if (isBuiltin(fn)) {
    throw b.error(node, `${(fn as { name: string }).name} cannot be called in a compiled function`);
  }
  return null;
}

function isBuiltin(value: unknown): boolean {
  return [
    Math,
    console,
    Number,
    String,
    Boolean,
    BigInt,
    Array,
    Set,
    Map,
    Error,
    TypeError,
    RangeError,
  ].includes(value as never);
}

/** The arguments of a function of numbers, as doubles. */
function numbers(
  b: Builder,
  name: string,
  args: readonly Argument[],
  count: number | null,
  node: SyntaxNode,
): Node[] {
  if (count !== null && args.length !== count) {
    throw b.error(
      node,
      `${name} takes ${count} argument${count === 1 ? "" : "s"} in a compiled function`,
    );
  }
  return args.map((x) => b.double(x.value, x.at));
}

/** The value of a function of constants, computed on the client, or null. */
function fold(args: readonly Argument[], fn: (...values: any[]) => unknown): Value | null {
  if (!args.every((x) => x.value instanceof ClientValue && typeof x.value.value === "number")) {
    return null;
  }
  return new ClientValue(fn(...args.map((x) => (x.value as ClientValue).value)));
}

function math(b: Builder, key: string, args: readonly Argument[], node: SyntaxNode): Value {
  const name = `Math.${key}`;
  const procedure = MATH_PROCEDURES[key];
  const folded = (x: () => Value) =>
    fold(args, (...values) => (Math[key as "abs"] as (...v: number[]) => number)(...values)) ?? x();
  if (procedure !== undefined) {
    const arity = key === "atan2" ? 2 : 1;
    const values = numbers(b, name, args, arity, node);
    return folded(() => b.stdlib(procedure, values));
  }
  switch (key) {
    case "log2": {
      numbers(b, name, args, 1, node);
      // The server's logarithm in base 2 is inexact, so only a constant is supported
      return folded(() => {
        throw b.error(
          node,
          "Math.log2 of a value on the server is not supported in a compiled function",
        );
      });
    }
    case "pow": {
      const [x, y] = numbers(b, name, args, 2, node);
      return folded(() => b.node("Power", [x, y], DOUBLE));
    }
    case "sign": {
      const [x] = numbers(b, name, args, 1, node);
      // NaN, 0 and -0 are their own sign
      return folded(() =>
        b.once(x, (p) =>
          b.node(
            "Conditional",
            [
              b.node(
                "ConditionalOr",
                [b.stdlib("IsNan", [p]), b.node("Equal", [p, b.constant(0, DOUBLE, node)], BOOL)],
                BOOL,
              ),
              p,
              b.cast(b.stdlib("Sign", [p]), DOUBLE),
            ],
            DOUBLE,
          ),
        ),
      );
    }
    case "trunc": {
      const [x] = numbers(b, name, args, 1, node);
      return folded(() =>
        b.once(x, (p) =>
          b.node(
            "Conditional",
            [
              b.node("LessThan", [p, b.constant(0, DOUBLE, node)], BOOL),
              b.stdlib("Ceiling", [p]),
              b.stdlib("Floor", [p]),
            ],
            DOUBLE,
          ),
        ),
      );
    }
    case "fround": {
      const [x] = numbers(b, name, args, 1, node);
      return folded(() => b.cast(b.cast(x, scalar("float")), DOUBLE));
    }
    case "hypot":
    case "min":
    case "max": {
      const values = numbers(b, name, args, null, node);
      return folded(() => {
        if (values.length === 0) {
          return b.constant(
            key === "hypot" ? 0 : key === "min" ? Infinity : -Infinity,
            DOUBLE,
            node,
          );
        }
        if (values.length === 1) {
          return key === "hypot" ? b.stdlib("Abs", values) : values[0];
        }
        const procedure = { hypot: "Hypot", min: "Min", max: "Max" }[key];
        return values.slice(1).reduce((x, y) => b.stdlib(procedure, [x, y]), values[0]);
      });
    }
    default:
      throw b.error(node, `${name} is not supported in a compiled function`);
  }
}

/** console.log, as StdLib.Print of the arguments' strings separated by spaces. */
function log(b: Builder, args: readonly Argument[], node: SyntaxNode): Value {
  const parts: Value[] = [];
  args.forEach((x, i) => {
    if (i > 0) {
      parts.push(new ClientValue(" "));
    }
    parts.push(b.string(x.value, x.at));
  });
  const message = concat(b, parts, node);
  return b.stdlib("Print", [b.toNode(message, STRING, node)]);
}

/** The concatenation of strings. Adjacent constants are joined on the client, and the parts of
 * a concatenation are taken into the one that holds it. */
export function concat(b: Builder, parts: readonly Value[], node: SyntaxNode): Value {
  const merged: Value[] = [];
  for (const part of parts) {
    const pieces =
      part instanceof Node && part.method === "StringConcat" ? (part.args[0] as Node[]) : [part];
    for (const piece of pieces) {
      const last = merged[merged.length - 1];
      if (piece instanceof ClientValue && last instanceof ClientValue) {
        merged[merged.length - 1] = new ClientValue(String(last.value) + String(piece.value));
      } else if (!(piece instanceof ClientValue && piece.value === "")) {
        merged.push(piece);
      }
    }
  }
  if (merged.length === 0) {
    return new ClientValue("");
  }
  if (merged.length === 1) {
    return merged[0];
  }
  return b.node("StringConcat", [merged.map((x) => b.toNode(x, STRING, node))], STRING);
}
