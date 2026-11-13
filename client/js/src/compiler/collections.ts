import type {
  ArrowFunctionExpression,
  CallExpression,
  Expression,
  SpreadElement,
  Node as SyntaxNode,
} from "acorn";
import type { Type } from "../types.js";
import type { Builder } from "./builder.js";
import type { ExpressionCompiler } from "./expressions.js";
import { ClientValue, type Node, type Operand, TypeArgument, type Value } from "./ir.js";
import { Variable } from "./scope.js";
import { LocalFunction, type StatementCompiler } from "./statements.js";
import { BOOL, DOUBLE, isNumber, SINT32, STRING, typeName, withNullable } from "./typing.js";

type ListType = Type & { readonly kind: "list" | "set"; readonly item: Type };
type DictionaryType = Type & {
  readonly kind: "dictionary";
  readonly key: Type;
  readonly value: Type;
};

/** The array methods that change an array in place, with the form a function uses. */
const IN_PLACE: Readonly<Record<string, string>> = {
  sort: "toSorted",
  reverse: "toReversed",
  splice: "slice and concat",
  shift: "slice",
  unshift: "concat",
};

/** Compiles the members and methods of lists, tuples, sets and dictionaries: arrays, `Set` and
 * `Map` in JavaScript. */
export class CollectionCompiler {
  readonly #b: Builder;
  readonly #x: ExpressionCompiler;
  readonly #s: StatementCompiler;

  constructor(builder: Builder, expressions: ExpressionCompiler, statements: StatementCompiler) {
    this.#b = builder;
    this.#x = expressions;
    this.#s = statements;
  }

  /** Whether a type is a collection this compiler handles. */
  static handles(type: Type | null): boolean {
    return (
      type !== null &&
      (type.kind === "list" ||
        type.kind === "set" ||
        type.kind === "dictionary" ||
        type.kind === "tuple")
    );
  }

  /** A property of a collection: `length` or `size`. */
  member(object: Node, key: string, node: SyntaxNode): Value {
    const b = this.#b;
    const type = object.type as Type;
    if (type.kind === "tuple" && key === "length") {
      return new ClientValue(type.items.length);
    }
    const expected = type.kind === "list" ? "length" : type.kind === "tuple" ? "" : "size";
    if (key === expected) {
      return b.node("Count", [object], SINT32);
    }
    throw b.error(node, `${key} of a ${typeName(type)} is not supported in a compiled function`);
  }

  /** A value of a tuple or a list at a constant position, for destructuring. */
  element(source: Node, i: number, node: SyntaxNode): Node {
    const b = this.#b;
    const type = source.type as Type;
    if (type.kind === "tuple") {
      if (i >= type.items.length) {
        throw b.error(node, `A ${typeName(type)} has no value ${i}`);
      }
      return b.node("Get", [source, b.constant(i, SINT32, node)], type.items[i]);
    }
    if (type.kind === "list") {
      return b.node("Get", [source, b.constant(i, SINT32, node)], type.item);
    }
    throw b.error(node, `A ${typeName(type)} cannot be destructured by position`);
  }

  /** An element of a list or tuple, `xs[i]`. */
  index(object: Node, index: Value, node: SyntaxNode): Value {
    const b = this.#b;
    const type = object.type as Type;
    if (type.kind === "tuple") {
      if (!(index instanceof ClientValue) || typeof index.value !== "number") {
        throw b.error(node, "A tuple is indexed by a constant");
      }
      return this.element(object, index.value, node);
    }
    if (type.kind === "list") {
      return b.node("Get", [object, b.toNode(index, SINT32, node, "as an index")], type.item);
    }
    throw b.error(
      node,
      `Indexing a ${typeName(type)} is not supported in a compiled function. Use get`,
    );
  }

  /** An assignment to an element of a list, `xs[i] = v`. */
  setElement(object: Node, index: Node, value: Operand, node: SyntaxNode): Node {
    const b = this.#b;
    const type = object.type as Type;
    if (type.kind !== "list") {
      throw b.error(node, `An element of a ${typeName(type)} cannot be assigned`);
    }
    const converted = b.toNode(value, type.item, node, "for the element");
    return b.node("Set", [object, b.toNode(index, SINT32, node, "as an index"), converted], null);
  }

  /** The collection a for...of iterates over, and the type of its values. The entries of a
   * dictionary are tuples of a key and a value. */
  iteration(collection: Node, node: SyntaxNode): [Node, Type] {
    const b = this.#b;
    const type = collection.type as Type;
    if (type.kind === "list" || type.kind === "set") {
      return [collection, type.item];
    }
    if (type.kind === "dictionary") {
      const entries = this.#entries(collection, type, node);
      return [entries, (entries.type as ListType).item];
    }
    throw b.error(
      node,
      `A for...of over a ${typeName(type)} is not supported in a compiled function`,
    );
  }

  /** A method of a list, a set or a dictionary. */
  method(object: Node, key: string, call: CallExpression): Value {
    const b = this.#b;
    const type = object.type as Type;
    const args = call.arguments;
    if (type.kind === "list") {
      return this.#listMethod(object, type, key, args, call);
    }
    if (type.kind === "set") {
      return this.#setMethod(object, type, key, args, call);
    }
    if (type.kind === "dictionary") {
      return this.#mapMethod(object, type, key, args, call);
    }
    throw b.error(
      call,
      `Method ${key} of a ${typeName(type)} is not supported in a compiled function`,
    );
  }

  /** Gives a type to a variable with none from a method that adds to it. */
  typeFromMethod(variable: Variable, key: string, call: CallExpression): void {
    const args = call.arguments.map((x) => this.#x.operand(this.#expression(x)));
    this.#s.typeFromUse(variable, key, args, call);
  }

  #expression(x: Expression | SpreadElement): Expression {
    if (x.type === "SpreadElement") {
      throw this.#b.error(x, "Spread is not supported in a compiled function");
    }
    return x;
  }

  #arity(key: string, args: readonly unknown[], low: number, high: number, node: SyntaxNode): void {
    if (args.length < low || args.length > high) {
      const count = low === high ? `${low}` : `${low} to ${high}`;
      throw this.#b.error(node, `${key} takes ${count} arguments in a compiled function`);
    }
  }

  /** The value of an argument, converted to a type. */
  #argument(x: Expression | SpreadElement, type: Type, where: string): Node {
    const expression = this.#expression(x);
    return this.#b.toNode(this.#x.operand(expression), type, expression, where);
  }

  /** A function passed to a method, called with values of the given types. */
  #callback(
    x: Expression | SpreadElement | undefined,
    types: readonly Type[],
    node: SyntaxNode,
  ): Node {
    const b = this.#b;
    if (x === undefined) {
      throw b.error(node, "The method takes a function");
    }
    if (x.type === "ArrowFunctionExpression" || x.type === "FunctionExpression") {
      return this.#s.lambda(x, types);
    }
    if (x.type === "Identifier") {
      const value = this.#x.lookupFunction(x);
      if (value instanceof LocalFunction) {
        return this.#s.localCallback(value, types, x);
      }
    }
    throw b.error(x, "The method takes a function, written in place or defined in the function");
  }

  /** A Lambda the compiler builds, of parameters of the given types. */
  #lambda(types: readonly Type[], body: (parameters: Node[]) => Node): Node {
    const b = this.#b;
    const parameters = types.map((type, i) =>
      b.node("Parameter", [`p${i}`, new TypeArgument(type)], type),
    );
    const result = body(parameters);
    return b.node("Lambda", [parameters, result], result.type);
  }

  /** The value a callback returns, checked to be a bool. */
  #predicate(lambda: Node, node: SyntaxNode): Node {
    if (lambda.type?.kind !== "bool" || lambda.type.nullable) {
      throw this.#b.error(
        node,
        `The function returns a ${typeName(lambda.type)}, and the method takes one that returns a bool`,
      );
    }
    return lambda;
  }

  #listMethod(
    list: Node,
    type: ListType,
    key: string,
    args: readonly (Expression | SpreadElement)[],
    node: CallExpression,
  ): Value {
    const b = this.#b;
    const item = type.item;
    const listOf = (t: Type): Type => ({ kind: "list", item: t, nullable: false });
    switch (key) {
      case "at": {
        this.#arity(key, args, 1, 1, node);
        const index = b.toNode(
          this.#x.expression(this.#expression(args[0])),
          SINT32,
          node,
          "as an index",
        );
        return b.once(list, (xs) =>
          b.once(index, (i) => {
            const fromEnd = b.node("Add", [b.node("Count", [xs], SINT32), i], SINT32);
            const position = b.node(
              "Conditional",
              [b.node("LessThan", [i, b.constant(0, SINT32, node)], BOOL), fromEnd, i],
              SINT32,
            );
            return b.node("Get", [xs, position], item);
          }),
        );
      }
      case "includes":
        this.#arity(key, args, 1, 1, node);
        return b.node("Contains", [list, this.#argument(args[0], item, "for includes")], BOOL);
      case "indexOf": {
        this.#arity(key, args, 1, 1, node);
        const value = this.#argument(args[0], item, "for indexOf");
        return b.once(value, (v) =>
          this.#position(list, item, (x) => b.node("Equal", [x, v], BOOL), node),
        );
      }
      case "findIndex": {
        this.#arity(key, args, 1, 1, node);
        const lambda = this.#predicate(this.#callback(args[0], [item], node), args[0]);
        return this.#position(list, item, (x) => this.#invoke(lambda, [x]), node);
      }
      case "map": {
        this.#arity(key, args, 1, 1, node);
        const lambda = this.#callback(args[0], [item], node);
        if (lambda.type === null) {
          throw b.error(args[0], "The function passed to map returns no value");
        }
        return b.node("ToList", [b.node("Select", [list, lambda], null)], listOf(lambda.type));
      }
      case "filter": {
        this.#arity(key, args, 1, 1, node);
        const lambda = this.#predicate(this.#callback(args[0], [item], node), args[0]);
        return b.node("ToList", [b.node("Where", [list, lambda], null)], type);
      }
      case "some":
      case "every": {
        this.#arity(key, args, 1, 1, node);
        const lambda = this.#predicate(this.#callback(args[0], [item], node), args[0]);
        return b.node(key === "some" ? "Any" : "All", [list, lambda], BOOL);
      }
      case "find": {
        this.#arity(key, args, 1, 1, node);
        const lambda = this.#predicate(this.#callback(args[0], [item], node), args[0]);
        const found = withNullable(item, true);
        // The test stops at the first match, as JavaScript's does
        return b.once(list, (l) =>
          b.once(
            this.#position(l, item, (x) => this.#invoke(lambda, [x]), node),
            (i) =>
              b.node(
                "Conditional",
                [
                  b.node("LessThan", [i, b.constant(0, SINT32, node)], BOOL),
                  b.constant(null, found, node),
                  b.cast(b.node("Get", [l, i], item), found),
                ],
                found,
              ),
          ),
        );
      }
      case "reduce": {
        this.#arity(key, args, 1, 2, node);
        if (args.length === 1) {
          const lambda = this.#callback(args[0], [item, item], node);
          return b.node("Aggregate", [list, lambda], lambda.type);
        }
        const seedSyntax = this.#expression(args[1]);
        const seedValue = this.#x.operand(seedSyntax);
        const seedType = b.valueType(seedValue, seedSyntax);
        if (seedType === null) {
          throw b.error(seedSyntax, "The initial value of reduce has no type");
        }
        const seed = b.toNode(seedValue, seedType, seedSyntax);
        const lambda = this.#callback(args[0], [seedType, item], node);
        return b.node("AggregateWithSeed", [list, seed, lambda], lambda.type);
      }
      case "slice": {
        this.#arity(key, args, 0, 2, node);
        return b.once(list, (xs) => {
          const length = b.cast(b.node("Count", [xs], SINT32), DOUBLE);
          const [start, count] = this.#range(length, args, node);
          const skipped = b.node("Skip", [xs, start], null);
          return b.node("ToList", [b.node("Take", [skipped, count], null)], type);
        });
      }
      case "concat": {
        this.#arity(key, args, 1, 1, node);
        const other = this.#argument(args[0], type, "for concat");
        return b.node("ToList", [b.node("Concat", [list, other], null)], type);
      }
      case "join": {
        this.#arity(key, args, 0, 1, node);
        const separator =
          args.length === 0
            ? b.constant(",", STRING, node)
            : this.#argument(args[0], STRING, "for join");
        let strings = list;
        if (item.kind !== "string" || item.nullable) {
          const select = this.#lambda([item], ([x]) => b.toNode(b.string(x, node), STRING, node));
          strings = b.node("ToList", [b.node("Select", [list, select], null)], listOf(STRING));
        }
        return b.node("StringJoin", [separator, strings], STRING);
      }
      case "toSorted": {
        this.#arity(key, args, 0, 1, node);
        const order =
          args.length === 0 ? this.#naturalOrder(item, node) : this.#comparatorKey(args[0], item);
        return b.node("ToList", [b.node("OrderBy", [list, order], null)], type);
      }
      case "toReversed":
        this.#arity(key, args, 0, 0, node);
        return b.node("ToList", [b.node("Reverse", [list], null)], type);
      case "push": {
        if (args.length === 0) {
          throw b.error(node, "push takes a value");
        }
        const appends = args.map((x) =>
          b.node("Append", [list, this.#argument(x, item, "for push")], null),
        );
        return appends.length === 1 ? appends[0] : b.node("Block", [appends], null);
      }
      case "pop":
        this.#arity(key, args, 0, 0, node);
        return b.once(list, (xs) => {
          const last = b.node("Variable", ["last", new TypeArgument(item)], item);
          const index = b.node(
            "Subtract",
            [b.node("Count", [xs], SINT32), b.constant(1, SINT32, node)],
            SINT32,
          );
          return b.node(
            "BlockWithVariables",
            [
              [last],
              [
                b.node("Assign", [last, b.node("Last", [xs], item)], null),
                b.node("RemoveAt", [xs, index], null),
                last,
              ],
            ],
            item,
          );
        });
      default: {
        const replacement = IN_PLACE[key];
        const hint = replacement === undefined ? "" : `. Use ${replacement}`;
        throw b.error(node, `Array method ${key} is not supported in a compiled function${hint}`);
      }
    }
  }

  #setMethod(
    set: Node,
    type: ListType,
    key: string,
    args: readonly (Expression | SpreadElement)[],
    node: CallExpression,
  ): Value {
    const b = this.#b;
    switch (key) {
      case "has":
        this.#arity(key, args, 1, 1, node);
        return b.node("Contains", [set, this.#argument(args[0], type.item, "for has")], BOOL);
      case "add":
        this.#arity(key, args, 1, 1, node);
        return b.node("Append", [set, this.#argument(args[0], type.item, "for add")], null);
      case "delete":
        this.#arity(key, args, 1, 1, node);
        return b.node("Remove", [set, this.#argument(args[0], type.item, "for delete")], null);
      case "clear":
        this.#arity(key, args, 0, 0, node);
        return b.node("Clear", [set], null);
      case "values":
      case "keys":
        this.#arity(key, args, 0, 0, node);
        return b.node("ToList", [set], { kind: "list", item: type.item, nullable: false });
      default:
        throw b.error(node, `Set method ${key} is not supported in a compiled function`);
    }
  }

  #mapMethod(
    map: Node,
    type: DictionaryType,
    key: string,
    args: readonly (Expression | SpreadElement)[],
    node: CallExpression,
  ): Value {
    const b = this.#b;
    const keyOf = (i: number) => this.#argument(args[i], type.key, `for ${key}`);
    switch (key) {
      case "has":
        this.#arity(key, args, 1, 1, node);
        return b.node("ContainsKey", [map, keyOf(0)], BOOL);
      case "get": {
        // A missing key gives null, as undefined in JavaScript
        this.#arity(key, args, 1, 1, node);
        const found = withNullable(type.value, true);
        return b.once(map, (m) =>
          b.once(keyOf(0), (k) =>
            b.node(
              "Conditional",
              [
                b.node("ContainsKey", [m, k], BOOL),
                b.cast(b.node("Get", [m, k], type.value), found),
                b.constant(null, found, node),
              ],
              found,
            ),
          ),
        );
      }
      case "set":
        this.#arity(key, args, 2, 2, node);
        return b.node("Set", [map, keyOf(0), this.#argument(args[1], type.value, "for set")], null);
      case "delete":
        this.#arity(key, args, 1, 1, node);
        return b.node("Remove", [map, keyOf(0)], null);
      case "clear":
        this.#arity(key, args, 0, 0, node);
        return b.node("Clear", [map], null);
      case "keys":
        this.#arity(key, args, 0, 0, node);
        return b.node("DictionaryKeys", [map], { kind: "list", item: type.key, nullable: false });
      case "values":
        this.#arity(key, args, 0, 0, node);
        return b.node("DictionaryValues", [map], {
          kind: "list",
          item: type.value,
          nullable: false,
        });
      case "entries":
        this.#arity(key, args, 0, 0, node);
        return this.#entries(map, type, node);
      default:
        throw b.error(node, `Map method ${key} is not supported in a compiled function`);
    }
  }

  /** The entries of a dictionary, as a list of tuples of a key and a value. */
  #entries(map: Node, type: DictionaryType, node: SyntaxNode): Node {
    const b = this.#b;
    const entry: Type = { kind: "tuple", items: [type.key, type.value], nullable: false };
    const pair = this.#lambda([type.key, type.value], ([k, v]) =>
      b.node("CreateTuple", [[k, v]], entry),
    );
    return b.once(map, (m) => {
      const keys = b.node("DictionaryKeys", [m], { kind: "list", item: type.key, nullable: false });
      const values = b.node("DictionaryValues", [m], {
        kind: "list",
        item: type.value,
        nullable: false,
      });
      void node;
      return b.node("ToList", [b.node("Zip", [keys, values, pair], null)], {
        kind: "list",
        item: entry,
        nullable: false,
      });
    });
  }

  /** The position of the first value of a list that passes a test, or -1. */
  #position(list: Node, item: Type, test: (x: Node) => Node, node: SyntaxNode): Node {
    const b = this.#b;
    const index = b.node("Variable", ["index", new TypeArgument(SINT32)], SINT32);
    const found = b.node("Variable", ["found", new TypeArgument(SINT32)], SINT32);
    const x = b.node("Variable", ["x", new TypeArgument(item)], item);
    const body = b.node(
      "Block",
      [
        [
          b.node(
            "IfThen",
            [
              test(x),
              b.node(
                "Block",
                [[b.node("Assign", [found, index], null), b.node("Break", [], null)]],
                null,
              ),
            ],
            null,
          ),
          b.node(
            "Assign",
            [index, b.node("Add", [index, b.constant(1, SINT32, node)], SINT32)],
            null,
          ),
        ],
      ],
      null,
    );
    return b.node(
      "BlockWithVariables",
      [
        [index, found, x],
        [
          b.node("Assign", [index, b.constant(0, SINT32, node)], null),
          b.node("Assign", [found, b.constant(-1, SINT32, node)], null),
          b.node("ForEach", [x, list, body], null),
          found,
        ],
      ],
      SINT32,
    );
  }

  #invoke(lambda: Node, args: readonly Node[]): Node {
    const parameters = lambda.args[0] as Node[];
    const values = new Map<string, Node>(parameters.map((p, i) => [p.args[0] as string, args[i]]));
    return this.#b.node("Invoke", [lambda, values], lambda.type);
  }

  /** The start and count of a slice, clamped to the collection as JavaScript clamps them. */
  #range(
    length: Node,
    args: readonly (Expression | SpreadElement)[],
    node: SyntaxNode,
  ): [Node, Node] {
    const b = this.#b;
    const zero = b.constant(0, DOUBLE, node);
    const index = (x: Expression | SpreadElement | undefined, fallback: Node): Node => {
      if (x === undefined) {
        return fallback;
      }
      const expression = this.#expression(x);
      const value = b.double(this.#x.expression(expression), expression);
      return b.once(value, (v) =>
        b.stdlib("Clamp", [
          b.node(
            "Conditional",
            [b.node("LessThan", [v, zero], BOOL), b.node("Add", [length, v], DOUBLE), v],
            DOUBLE,
          ),
          zero,
          length,
        ]),
      );
    };
    const start = index(args[0], zero);
    const end = index(args[1], length);
    const count = b.stdlib("Max", [b.node("Subtract", [end, start], DOUBLE), zero]);
    return [b.cast(start, SINT32), b.cast(count, SINT32)];
  }

  /** The key of a sort with no comparator, which JavaScript gives strings only. */
  #naturalOrder(item: Type, node: SyntaxNode): Node {
    const b = this.#b;
    if (item.kind !== "string") {
      throw b.error(
        node,
        "toSorted with no comparator sorts as strings. Give a comparator, such as (a, b) => a - b",
      );
    }
    return this.#lambda([item], ([x]) => x);
  }

  /** The key of a comparator that subtracts one value's key from the other's, as
   * `(a, b) => a.mass - b.mass`. Reversed operands sort in descending order. */
  #comparatorKey(x: Expression | SpreadElement, item: Type): Node {
    const b = this.#b;
    const fail = () =>
      b.error(
        x,
        "A comparator subtracts a key of one value from the same key of the other, as (a, b) => a.mass - b.mass",
      );
    if (x.type !== "ArrowFunctionExpression" || x.params.length !== 2) {
      throw fail();
    }
    const [a, c] = x.params;
    let body = x.body;
    if (body.type === "BlockStatement") {
      const [statement] = body.body;
      if (body.body.length !== 1 || statement.type !== "ReturnStatement" || !statement.argument) {
        throw fail();
      }
      body = statement.argument;
    }
    if (
      a.type !== "Identifier" ||
      c.type !== "Identifier" ||
      body.type !== "BinaryExpression" ||
      body.operator !== "-"
    ) {
      throw fail();
    }
    const left = body.left as Expression;
    const right = body.right;
    const ascending = sameShape(left, right, a.name, c.name);
    if (!ascending && !sameShape(left, right, c.name, a.name)) {
      throw fail();
    }
    const param = ascending ? a : c;
    const key: ArrowFunctionExpression = { ...x, params: [param], body: left };
    const lambda = this.#s.lambda(key, [item]);
    // JavaScript throws a TypeError for a comparator that returns a bigint
    if (!isNumber(lambda.type)) {
      throw b.error(x, "A comparator subtracts numbers");
    }
    if (ascending) {
      return lambda;
    }
    // Descending order of a reversed key keeps equal values in their order, as JavaScript does.
    // The bitwise complement reverses an integer with no overflow, and negation a float.
    const kind = lambda.type?.kind;
    const reverse = kind === "double" || kind === "float" ? "Negate" : "Not";
    return this.#lambda([item], ([v]) => b.node(reverse, [this.#invoke(lambda, [v])], lambda.type));
  }
}

/** Whether two expressions are the same with one name swapped for another. */
function sameShape(a: Expression, b: Expression, nameA: string, nameB: string): boolean {
  const strip = (node: unknown, from: string, to: string): unknown => {
    if (Array.isArray(node)) {
      return node.map((x) => strip(x, from, to));
    }
    if (typeof node !== "object" || node === null) {
      return node;
    }
    const result: Record<string, unknown> = {};
    for (const [key, value] of Object.entries(node)) {
      if (key === "start" || key === "end" || key === "loc" || key === "range") {
        continue;
      }
      result[key] = strip(value, from, to);
    }
    if (result.type === "Identifier" && result.name === from) {
      result.name = to;
    }
    return result;
  };
  return JSON.stringify(strip(a, nameA, "$")) === JSON.stringify(strip(b, nameB, "$"));
}

export { Variable };
