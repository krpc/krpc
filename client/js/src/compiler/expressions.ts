import type {
  BinaryExpression,
  CallExpression,
  ChainExpression,
  ConditionalExpression,
  Expression,
  LogicalExpression,
  MemberExpression,
  NewExpression,
  ObjectExpression,
  PrivateIdentifier,
  SpreadElement,
  Super,
  Node as SyntaxNode,
  TemplateLiteral,
  UnaryExpression,
} from "acorn";
import { Connection } from "../connection.js";
import { RemoteObject } from "../objects.js";
import { namedOf } from "../registry.js";
import type { Type } from "../types.js";
import { RemoteFlags, RemoteStruct } from "../types.js";
import { TypeValue, types } from "../typevalues.js";
import { type Argument, type Builder, describe, isNullish } from "./builder.js";
import { builtinFunction, builtinMethod, concat } from "./builtins.js";
import { CollectionCompiler } from "./collections.js";
import { ClientValue, Collection, Node, type Operand, TypeArgument, type Value } from "./ir.js";
import type { Members } from "./members.js";
import { Caught, NO_CAPTURES, type Scope, Variable } from "./scope.js";
import { LocalFunction, type StatementCompiler } from "./statements.js";
import {
  BOOL,
  DOUBLE,
  isBigint,
  isNumber,
  resolveType,
  SINT32,
  STRING,
  typeName,
  UINT32,
  withNullable,
} from "./typing.js";

/** The constructs a compiled function cannot hold, by node type. */
const UNSUPPORTED: Readonly<Record<string, string>> = {
  ThisExpression: "this",
  AwaitExpression: "await",
  YieldExpression: "yield",
  ClassExpression: "A class",
  TaggedTemplateExpression: "A tagged template",
  SequenceExpression: "The comma operator",
  MetaProperty: "new.target and import.meta",
  ImportExpression: "import()",
  ArrowFunctionExpression: "A function here",
  FunctionExpression: "A function here",
  ObjectExpression: "An object literal",
  AssignmentExpression: "An assignment as a value",
  UpdateExpression: "++ and -- as a value",
};

const ARITHMETIC: Readonly<Record<string, string>> = {
  "+": "Add",
  "-": "Subtract",
  "*": "Multiply",
  "/": "Divide",
  "%": "Modulo",
  "**": "Power",
};

const BITWISE: Readonly<Record<string, string>> = {
  "&": "And",
  "|": "Or",
  "^": "ExclusiveOr",
};

const RELATIONAL: Readonly<Record<string, string>> = {
  "<": "LessThan",
  "<=": "LessThanOrEqual",
  ">": "GreaterThan",
  ">=": "GreaterThanOrEqual",
};

// The operators folded on the client, for operands computed on the client
const OPERATORS: Readonly<Record<string, (a: any, b: any) => unknown>> = {
  "+": (a, b) => a + b,
  "-": (a, b) => a - b,
  "*": (a, b) => a * b,
  "/": (a, b) => a / b,
  "%": (a, b) => a % b,
  "**": (a, b) => a ** b,
  "&": (a, b) => a & b,
  "|": (a, b) => a | b,
  "^": (a, b) => a ^ b,
  "<<": (a, b) => a << b,
  ">>": (a, b) => a >> b,
  ">>>": (a, b) => a >>> b,
  // biome-ignore lint/suspicious/noDoubleEquals: the operator folds as JavaScript computes it
  "==": (a, b) => a == b,
  // biome-ignore lint/suspicious/noDoubleEquals: the operator folds as JavaScript computes it
  "!=": (a, b) => a != b,
  "===": (a, b) => a === b,
  "!==": (a, b) => a !== b,
  "<": (a, b) => a < b,
  "<=": (a, b) => a <= b,
  ">": (a, b) => a > b,
  ">=": (a, b) => a >= b,
};

const UNARY_OPERATORS: Readonly<Record<string, (a: any) => unknown>> = {
  "-": (a) => -a,
  "+": (a) => +a,
  "~": (a) => ~a,
};

/** The string methods a function can call, with the number of arguments each takes. */
const STRING_METHODS: Readonly<Record<string, readonly [number, number]>> = {
  at: [1, 1],
  slice: [0, 2],
  substring: [1, 2],
  toUpperCase: [0, 0],
  toLowerCase: [0, 0],
  trim: [0, 0],
  trimStart: [0, 0],
  trimEnd: [0, 0],
  includes: [1, 1],
  startsWith: [1, 1],
  endsWith: [1, 1],
  indexOf: [1, 1],
  replaceAll: [2, 2],
  split: [1, 1],
};

/** A guard of an optional chain: a variable holding a value that can be null, which the chain
 * reads only when it is not. */
interface Guard {
  readonly variable: Node;
  readonly value: Node;
}

/** The end of an optional chain over a value computed on the client as null. */
class ShortCircuit {}

/** Compiles the expressions of a function into nodes of the typed tree. */
export class ExpressionCompiler {
  readonly #b: Builder;
  readonly #scope: Scope;
  #guards: Guard[] | null = null;
  statements!: StatementCompiler;
  collections!: CollectionCompiler;

  constructor(builder: Builder, scope: Scope) {
    this.#b = builder;
    this.#scope = scope;
  }

  expression(node: Expression | SpreadElement | Super | PrivateIdentifier): Value {
    const b = this.#b;
    switch (node.type) {
      case "Identifier":
        return this.#identifier(node.name, node);
      case "Literal":
        if ("regex" in node && node.regex !== undefined) {
          throw b.error(node, "A regular expression is not supported in a compiled function");
        }
        if (node.bigint !== undefined) {
          return new ClientValue(BigInt(node.bigint));
        }
        return new ClientValue(node.value);
      case "TemplateLiteral":
        return this.#template(node);
      case "MemberExpression":
        return this.#member(node);
      case "ChainExpression":
        return this.#chain(node);
      case "CallExpression":
        return this.#call(node);
      case "UnaryExpression":
        return this.#unary(node);
      case "BinaryExpression":
        return this.#binary(node);
      case "LogicalExpression":
        return this.#logical(node);
      case "ConditionalExpression":
        return this.#conditional(node);
      case "ParenthesizedExpression":
        return this.expression(node.expression);
      case "ArrayExpression":
      case "NewExpression":
        return b.resolved(this.operand(node), node);
      case "SpreadElement":
        throw b.error(node, "Spread is not supported in a compiled function");
      case "Super":
        throw b.error(node, "super is not supported in a compiled function");
      case "PrivateIdentifier":
        throw b.error(node, "A private name is not supported in a compiled function");
      default: {
        const construct = UNSUPPORTED[node.type] ?? node.type;
        throw b.error(node, `${construct} is not supported in a compiled function`);
      }
    }
  }

  #identifier(name: string, node: SyntaxNode): Value {
    const b = this.#b;
    const binding = this.#scope.lookup(name);
    if (binding === NO_CAPTURES) {
      throw b.error(
        node,
        `${name} receives the captured values, which are read with an object pattern, such as ({ vessel }) => ...`,
      );
    }
    if (binding === undefined) {
      throw b.error(
        node,
        `${name} is not a capture, a parameter or a variable. Capture it, as in ({ ${name} }) => ... with { ${name} }`,
      );
    }
    if (binding instanceof Variable) {
      return this.statements.readVariable(binding, node);
    }
    if (binding instanceof Caught) {
      throw b.error(node, `${name} is the caught error, whose message is ${name}.message`);
    }
    if (binding instanceof LocalFunction) {
      return new ClientValue(binding);
    }
    return binding as Value;
  }

  /** The variable or state variable an expression names, when it has no type yet. */
  #untyped(node: Expression): Variable | undefined {
    let variable: Variable | undefined;
    if (node.type === "Identifier") {
      const binding = this.#scope.lookup(node.name);
      variable = binding instanceof Variable ? binding : undefined;
    } else if (
      node.type === "MemberExpression" &&
      !node.computed &&
      node.object.type === "Identifier" &&
      node.property.type === "Identifier"
    ) {
      const binding = this.#scope.lookup(node.object.name);
      if (
        binding instanceof ClientValue &&
        typeof binding.value === "object" &&
        binding.value !== null
      ) {
        variable = this.statements.stateVariable(
          node.object.name,
          binding.value,
          node.property.name,
          node,
        );
      }
    }
    return variable?.node === null ? variable : undefined;
  }

  /** The local function a name is bound to, or undefined. */
  lookupFunction(node: { name: string }): LocalFunction | undefined {
    const binding = this.#scope.lookup(node.name);
    return binding instanceof LocalFunction ? binding : undefined;
  }

  /** The value of an expression, with a collection left to take its type from where it is
   * used. */
  operand(node: Expression | SpreadElement): Operand {
    const b = this.#b;
    switch (node.type) {
      case "ArrayExpression":
        return new Collection(
          "array",
          node.elements.map((x) => {
            if (x === null) {
              throw b.error(node, "An array with holes is not supported in a compiled function");
            }
            return this.operand(x);
          }),
          [],
          node,
        );
      case "NewExpression":
        return this.#new(node);
      case "ParenthesizedExpression":
        return this.operand(node.expression);
      case "SpreadElement":
        throw b.error(node, "Spread is not supported in a compiled function");
      default:
        return this.expression(node);
    }
  }

  /** `new Set(...)`, `new Map(...)`, or a structure built from an object of its fields. */
  #new(node: NewExpression): Operand {
    const b = this.#b;
    const callee = this.expression(node.callee as Expression);
    const args = node.arguments;
    const value = callee instanceof ClientValue ? callee.value : undefined;
    if (value === Set || value === Map) {
      if (args.length > 1) {
        throw b.error(node, `new ${value === Set ? "Set" : "Map"} takes at most one argument`);
      }
      const [source] = args;
      if (source === undefined) {
        return new Collection(value === Set ? "set" : "map", [], [], node);
      }
      if (source.type === "SpreadElement") {
        throw b.error(source, "Spread is not supported in a compiled function");
      }
      if (value === Set) {
        if (source.type === "ArrayExpression") {
          const array = this.operand(source) as Collection;
          return new Collection("set", array.items, [], node);
        }
        const values = b.toNode(this.operand(source), undefined, source);
        const type = values.type as Type;
        if (type.kind !== "list" && type.kind !== "set") {
          throw b.error(source, `A Set is built from an array, and this is a ${typeName(type)}`);
        }
        return b.node("ToSet", [values], { kind: "set", item: type.item, nullable: false });
      }
      if (source.type !== "ArrayExpression") {
        throw b.error(
          source,
          "A Map is built from an array of [key, value] arrays in a compiled function",
        );
      }
      const keys: Operand[] = [];
      const items: Operand[] = [];
      for (const entry of source.elements) {
        if (entry?.type !== "ArrayExpression" || entry.elements.length !== 2) {
          throw b.error(entry ?? source, "An entry of a Map is a [key, value] array");
        }
        keys.push(this.operand(entry.elements[0] as Expression));
        items.push(this.operand(entry.elements[1] as Expression));
      }
      return new Collection("map", items, keys, node);
    }
    const named = namedOf(value);
    if (named?.kind === "struct") {
      return this.#newStruct(named.declaration, node);
    }
    if (
      value === Error ||
      value === TypeError ||
      value === RangeError ||
      named?.kind === "exception"
    ) {
      throw b.error(node, "An error is built in a throw statement, as throw new Error(message)");
    }
    throw b.error(node, `new ${describe(value)} is not supported in a compiled function`);
  }

  #newStruct(declaration: { service: string; name: string }, node: NewExpression): Node {
    const b = this.#b;
    const key = `${declaration.service}.${declaration.name}`;
    const struct = b.env.types.struct(key);
    const type: Type = { kind: "struct", struct, nullable: false };
    const [fields] = node.arguments;
    if (node.arguments.length !== 1 || fields?.type !== "ObjectExpression") {
      throw b.error(node, `A ${key} is built from an object of its fields`);
    }
    const values = this.#named(fields);
    for (const name of values.keys()) {
      if (!struct.fields.some((x) => x.name === name)) {
        throw b.error(fields, `A ${key} has no field ${name}`);
      }
    }
    const nodes = struct.fields.map((field) => {
      const value = values.get(field.name);
      if (value === undefined) {
        throw b.error(fields, `A ${key} needs the field ${field.name}`);
      }
      return b.toNode(value.value, field.type, value.at, `for field ${field.name}`);
    });
    return b.node("CreateStruct", [new TypeArgument(type), nodes], type);
  }

  /** A node of an expression's value, taking the type its value gives it. */
  node(node: Expression): Node {
    return this.#b.toNode(this.expression(node), undefined, node);
  }

  #template(node: TemplateLiteral): Value {
    const parts: Value[] = [];
    node.quasis.forEach((quasi, i) => {
      parts.push(new ClientValue(quasi.value.cooked ?? ""));
      const expression = node.expressions[i];
      if (expression !== undefined) {
        parts.push(this.#b.string(this.expression(expression), expression));
      }
    });
    return concat(this.#b, parts, node);
  }

  // Members

  /** The object of a member expression, guarded when the member is optional. */
  #object(node: MemberExpression): Value {
    const value = this.expression(node.object);
    return node.optional ? this.#guard(value, node.object) : value;
  }

  #guard(value: Value, node: SyntaxNode): Value {
    if (value instanceof ClientValue) {
      if (value.value === null || value.value === undefined) {
        throw new ShortCircuit();
      }
      return value;
    }
    if (!value.type?.nullable) {
      return value;
    }
    if (this.#guards === null) {
      throw this.#b.error(node, "?. is supported only in a chain");
    }
    const type = value.type;
    const variable = this.#b.node("Variable", ["value", new TypeArgument(type)], type);
    this.#guards.push({ variable, value });
    return this.#b.cast(variable, withNullable(type, false));
  }

  #chain(node: ChainExpression): Value {
    const b = this.#b;
    const outer = this.#guards;
    const guards: Guard[] = [];
    this.#guards = guards;
    let inner: Value;
    try {
      inner = this.expression(node.expression);
    } catch (error) {
      if (error instanceof ShortCircuit) {
        return new ClientValue(undefined);
      }
      throw error;
    } finally {
      this.#guards = outer;
    }
    if (guards.length === 0) {
      return inner;
    }
    let result = b.toNode(inner, undefined, node.expression);
    const type = result.type === null ? null : withNullable(result.type, true);
    for (const guard of guards.reverse()) {
      const isNull = b.node("IsNull", [guard.variable], BOOL);
      const read =
        type === null
          ? b.node("IfThen", [b.node("Not", [isNull], BOOL), result], null)
          : b.node("Conditional", [isNull, b.constant(null, type, node), result], type);
      result = b.node(
        "BlockWithVariables",
        [[guard.variable], [b.node("Assign", [guard.variable, guard.value], null), read]],
        type,
      );
    }
    return result;
  }

  /** The key of a member: its name, or for a computed member, the value of the key. */
  #key(node: MemberExpression): string | Value {
    const b = this.#b;
    if (!node.computed) {
      if (node.property.type !== "Identifier") {
        throw b.error(node.property, "A private member is not supported in a compiled function");
      }
      return node.property.name;
    }
    const key = this.expression(node.property);
    if (
      key instanceof ClientValue &&
      (typeof key.value === "string" || typeof key.value === "symbol")
    ) {
      return String(key.value);
    }
    return key;
  }

  #member(node: MemberExpression): Value {
    const b = this.#b;
    if (
      node.object.type === "Identifier" &&
      !node.computed &&
      node.property.type === "Identifier"
    ) {
      const binding = this.#scope.lookup(node.object.name);
      if (binding instanceof Caught) {
        if (node.property.name !== "message") {
          throw b.error(node, "A compiled function reads only the message of a caught error");
        }
        return binding.message;
      }
      if (
        binding instanceof ClientValue &&
        typeof binding.value === "object" &&
        binding.value !== null
      ) {
        const state = this.statements.stateVariable(
          node.object.name,
          binding.value,
          node.property.name,
          node,
        );
        if (state !== undefined) {
          return this.statements.readVariable(state, node);
        }
      }
    }
    let object = this.#object(node);
    const key = this.#key(node);
    if (object instanceof ClientValue && isClientCollection(object.value)) {
      object = b.toNode(object, undefined, node.object);
    }
    if (typeof key !== "string") {
      return this.#index(object, key, node);
    }
    if (object instanceof ClientValue) {
      const value = object.value;
      if (value instanceof RemoteObject) {
        return this.#property(b.constant(value, undefined, node.object), key, node);
      }
      if (typeof value === "string") {
        if (key === "length") {
          return new ClientValue(value.length);
        }
        throw b.error(node, `String member ${key} is not supported in a compiled function`);
      }
      const named = b.namedOf(object);
      if (named?.kind === "service") {
        const members = b.env.members.ofService(named.service);
        if (members?.getters.has(key) || members?.methods.has(key)) {
          return this.#property(null, key, node, members, named.service);
        }
        return new ClientValue(Reflect.get(value as object, key));
      }
      if (this.#readsOnClient(value)) {
        return new ClientValue(Reflect.get(value as object, key));
      }
      throw b.error(
        node,
        `Reading ${key} of ${describe(value)} is not supported in a compiled function`,
      );
    }
    return this.memberOf(object, key, node);
  }

  /** A property of a node's value: a property of an object, a field of a structure, or the
   * length or size of a string or collection. */
  memberOf(object: Node, key: string, node: SyntaxNode): Value {
    const b = this.#b;
    if (object.type === null) {
      throw b.error(node, "The expression has no value");
    }
    // A member of null raises an exception on the server, as a TypeError does in JavaScript
    const type = object.type;
    if (type.kind === "class") {
      return this.#property(object, key, node);
    }
    if (type.kind === "string") {
      if (key === "length") {
        return b.node("StringLength", [object], SINT32);
      }
      throw b.error(node, `String member ${key} is not supported in a compiled function`);
    }
    if (type.kind === "struct") {
      const field = type.struct.fields.find((x) => x.name === key);
      const name = b.env.members.fieldName(type.struct.service, type.struct.name, key);
      if (field === undefined || name === undefined) {
        throw b.error(node, `${type.struct.service}.${type.struct.name} has no field ${key}`);
      }
      return b.node("GetField", [object, name], field.type);
    }
    if (CollectionCompiler.handles(type)) {
      return this.collections.member(object, key, node);
    }
    throw b.error(
      node,
      `Reading ${key} of a ${typeName(type)} is not supported in a compiled function`,
    );
  }

  /** An assignment to a property of an object or a service, with its setter. */
  setProperty(object: Value, key: string, value: Operand, node: SyntaxNode): Node {
    const b = this.#b;
    let members: Members | undefined;
    let instance: Node | null = null;
    let owner: string;
    const named = b.namedOf(object);
    if (named?.kind === "service") {
      members = b.env.members.ofService(named.service);
      owner = named.service;
    } else {
      instance = object instanceof Node ? object : b.toNode(object, undefined, node);
      const type = instance.type;
      if (type?.kind !== "class") {
        throw b.error(
          node,
          `A property of a ${typeName(type)} cannot be assigned in a compiled function`,
        );
      }
      members = b.env.members.ofClass(type.service, type.name);
      owner = `${type.service}.${type.name}`;
    }
    const setter = members?.setters.get(key);
    if (setter === undefined) {
      throw b.error(node, `${owner}.${key} has no setter`);
    }
    return b.call(setter, instance, [{ value, at: node }], new Map(), node);
  }

  /** Whether the properties of a captured value are read on the client: a plain object, a
   * module, the connection, or an enumeration's or a builtin's values. */
  #readsOnClient(value: unknown): boolean {
    if (typeof value !== "object" || value === null) {
      return false;
    }
    if (value instanceof Connection || value === Math || value === Number) {
      return true;
    }
    if (Object.prototype.toString.call(value) === "[object Module]") {
      return true;
    }
    const prototype = Object.getPrototypeOf(value);
    return prototype === Object.prototype || prototype === null;
  }

  /** The value of a property of an object, or of a service when the object is null. */
  #property(
    object: Node | null,
    key: string,
    node: SyntaxNode,
    members?: Members,
    service?: string,
  ): Value {
    const b = this.#b;
    let owner: string;
    if (object === null) {
      owner = service as string;
    } else {
      const type = object.type as Extract<Node["type"] & object, { kind: "class" }>;
      owner = `${type.service}.${type.name}`;
      members = b.env.members.ofClass(type.service, type.name);
    }
    const getter = members?.getters.get(key);
    if (getter !== undefined) {
      return b.call(getter, object, [], new Map(), node);
    }
    if (members?.methods.has(key)) {
      throw b.error(node, `${owner}.${key} is a method. Call it, as in ${key}()`);
    }
    throw b.error(node, `${owner} has no property ${key}`);
  }

  /** An element of a string, `s[i]`. */
  #index(object: Value, key: Value, node: MemberExpression): Value {
    const b = this.#b;
    if (object instanceof ClientValue && key instanceof ClientValue) {
      if (typeof object.value === "string" && typeof key.value === "number") {
        return new ClientValue(object.value[key.value]);
      }
      throw b.error(
        node,
        `Indexing ${describe(object.value)} is not supported in a compiled function`,
      );
    }
    const string = b.toNode(object, undefined, node.object);
    if (CollectionCompiler.handles(string.type)) {
      return this.collections.index(string, key, node);
    }
    if (string.type?.kind !== "string") {
      throw b.error(
        node,
        `Indexing a ${typeName(string.type)} is not supported in a compiled function`,
      );
    }
    b.notNull(string, node.object);
    if (
      key instanceof ClientValue &&
      typeof key.value === "number" &&
      !Number.isInteger(key.value)
    ) {
      throw b.error(node.property, `${key.value} is not an index, so the character is undefined`);
    }
    return b.node(
      "StringGet",
      [string, b.toNode(key, SINT32, node.property, "as an index")],
      STRING,
    );
  }

  // Calls

  #call(node: CallExpression): Value {
    const b = this.#b;
    if (node.optional) {
      throw b.error(node, "?.() is not supported in a compiled function");
    }
    const callee = node.callee;
    if (callee.type === "MemberExpression") {
      const untyped = this.#untyped(callee.object as Expression);
      if (untyped !== undefined && !callee.computed && callee.property.type === "Identifier") {
        this.collections.typeFromMethod(untyped, callee.property.name, node);
      }
      const object = this.#object(callee);
      const key = this.#key(callee);
      if (typeof key !== "string") {
        throw b.error(callee.property, "A method is named by its key, which cannot be computed");
      }
      return this.#method(object, key, node, callee);
    }
    const fn = this.expression(callee);
    if (fn instanceof ClientValue && fn.value instanceof LocalFunction) {
      const args = this.#arguments(node.arguments);
      return this.statements.callLocal(
        fn.value,
        args.map((x) => x.value),
        args.map((x) => x.at),
        node,
      );
    }
    if (fn instanceof ClientValue) {
      const result = builtinFunction(b, fn.value, this.#arguments(node.arguments), node);
      if (result !== null) {
        return result;
      }
      throw b.error(
        callee,
        `${describe(fn.value)} cannot be called in a compiled function. A function calls the members of a service's objects, and the builtins`,
      );
    }
    throw b.error(callee, `A ${typeName(fn.type)} cannot be called`);
  }

  #arguments(args: readonly (Expression | SpreadElement)[]): Argument[] {
    return args.map((x) => {
      if (x.type === "SpreadElement") {
        throw this.#b.error(x, "Spread is not supported in a compiled function");
      }
      return { value: this.operand(x), at: x };
    });
  }

  /** The arguments of a member call: those by position, and an object literal last as the
   * arguments by name. */
  #memberArguments(
    args: readonly (Expression | SpreadElement)[],
  ): [Argument[], Map<string, Argument>] {
    const last = args[args.length - 1];
    if (last?.type !== "ObjectExpression") {
      return [this.#arguments(args), new Map()];
    }
    return [this.#arguments(args.slice(0, -1)), this.#named(last)];
  }

  #named(node: ObjectExpression): Map<string, Argument> {
    const b = this.#b;
    const named = new Map<string, Argument>();
    for (const property of node.properties) {
      if (property.type === "SpreadElement") {
        throw b.error(property, "Spread is not supported in a compiled function");
      }
      if (property.computed || property.kind !== "init" || property.method) {
        throw b.error(property, "An argument by name is a key and a value");
      }
      const key =
        property.key.type === "Identifier"
          ? property.key.name
          : String((property.key as { value: unknown }).value);
      named.set(key, { value: this.operand(property.value as Expression), at: property });
    }
    return named;
  }

  #method(object: Value, key: string, node: CallExpression, callee: MemberExpression): Value {
    const b = this.#b;
    if (object instanceof ClientValue && isClientCollection(object.value)) {
      object = b.toNode(object, undefined, callee.object);
    }
    if (object instanceof ClientValue) {
      const value = object.value;
      const builtin = builtinMethod(b, value, key, () => this.#arguments(node.arguments), node);
      if (builtin !== null) {
        return builtin;
      }
      if (value instanceof RemoteObject) {
        return this.#classMethod(b.constant(value, undefined, callee.object), key, node);
      }
      if (typeof value === "string") {
        return this.#stringMethod(object, key, node);
      }
      if (value === types || (value instanceof TypeValue && key === "cast")) {
        return this.#typeMethod(value, key, node);
      }
      if (value instanceof RemoteFlags) {
        return this.#flagsMethod(object, key, node);
      }
      const named = b.namedOf(object);
      if (named?.kind === "service") {
        const members = b.env.members.ofService(named.service);
        return this.#memberCall(members?.methods.get(key), members, null, named.service, key, node);
      }
      if (named?.kind === "class") {
        const members = b.env.members.ofClass(named.service, named.name);
        const member = members?.statics.get(key);
        if (member === undefined) {
          throw b.error(callee, `${named.service}.${named.name} has no static method ${key}`);
        }
        const [args, byName] = this.#memberArguments(node.arguments);
        return b.call(member, null, args, byName, node);
      }
      throw b.error(callee, `${describe(value)} has no method ${key} a compiled function can call`);
    }
    const type = object.type;
    if (type === null) {
      throw b.error(callee.object, "The expression has no value");
    }
    if (type.kind === "class") {
      return this.#classMethod(object, key, node);
    }
    if (type.kind === "string") {
      return this.#stringMethod(object, key, node);
    }
    if (type.kind === "enumeration" && type.enumeration.flags) {
      return this.#flagsMethod(object, key, node);
    }
    if (CollectionCompiler.handles(type)) {
      return this.collections.method(object, key, node);
    }
    throw b.error(callee, `A ${typeName(type)} has no method ${key} a compiled function can call`);
  }

  /** A method of the `types` values: a type built on the client, or a value given a type with
   * `cast`. */
  #typeMethod(value: object, key: string, node: CallExpression): Value {
    const b = this.#b;
    const [first] = node.arguments;
    if (value instanceof TypeValue && first !== undefined && first.type !== "SpreadElement") {
      // A cast types a variable that has no type yet, and the function is compiled again
      const untyped = this.#untyped(first);
      if (untyped !== undefined) {
        b.hint(untyped.key, resolveType(b.env.types, value));
      }
    }
    const args = this.#arguments(node.arguments);
    if (value instanceof TypeValue) {
      if (args.length !== 1) {
        throw b.error(node, "cast takes one argument");
      }
      let type: ReturnType<typeof resolveType>;
      try {
        type = resolveType(b.env.types, value);
      } catch (error) {
        throw b.error(node, (error as Error).message);
      }
      const [x] = args;
      if (isNullish(x.value) && !type.nullable) {
        throw b.error(x.at, `A ${typeName(type)} cannot be null. Cast to types.nullable(T)`);
      }
      return b.toNode(x.value, type, x.at, "for cast");
    }
    const fn = Reflect.get(value, key);
    if (typeof fn !== "function" || !args.every((x) => x.value instanceof ClientValue)) {
      throw b.error(node, `types.${key} takes types, read on the client`);
    }
    try {
      return new ClientValue(fn(...args.map((x) => (x.value as ClientValue).value)));
    } catch (error) {
      throw b.error(node, (error as Error).message);
    }
  }

  #classMethod(object: Node, key: string, node: CallExpression): Value {
    const type = object.type as Extract<NonNullable<Node["type"]>, { kind: "class" }>;
    const members = this.#b.env.members.ofClass(type.service, type.name);
    return this.#memberCall(
      members?.methods.get(key),
      members,
      object,
      `${type.service}.${type.name}`,
      key,
      node,
    );
  }

  #memberCall(
    member: ReturnType<Members["methods"]["get"]>,
    members: Members | undefined,
    object: Node | null,
    owner: string,
    key: string,
    node: CallExpression,
  ): Value {
    const b = this.#b;
    if (member === undefined) {
      if (members?.getters.has(key)) {
        throw b.error(node, `${owner}.${key} is a property. Read it with no call, as in .${key}`);
      }
      const accessor = /^(get|set)([A-Z].*)$/.exec(key);
      if (accessor !== null) {
        const property = accessor[2].charAt(0).toLowerCase() + accessor[2].slice(1);
        const has =
          accessor[1] === "get" ? members?.getters.has(property) : members?.setters.has(property);
        if (has) {
          throw b.error(
            node,
            accessor[1] === "get"
              ? `In a compiled function, read the property, as in .${property} in place of .${key}()`
              : `In a compiled function, assign the property, as in .${property} = x in place of .${key}(x)`,
          );
        }
      }
      throw b.error(node, `${owner} has no method ${key}`);
    }
    const [args, named] = this.#memberArguments(node.arguments);
    return b.call(member, object, args, named, node);
  }

  #stringMethod(object: Value, key: string, node: CallExpression): Value {
    const b = this.#b;
    const arity = STRING_METHODS[key];
    if (arity === undefined) {
      const hint = key === "replace" ? ". Use replaceAll" : "";
      throw b.error(node, `String method ${key} is not supported in a compiled function${hint}`);
    }
    const args = this.#arguments(node.arguments);
    if (args.length < arity[0] || args.length > arity[1]) {
      throw b.error(
        node,
        `String method ${key} takes ${arity[0] === arity[1] ? arity[0] : `${arity[0]} to ${arity[1]}`} arguments in a compiled function`,
      );
    }
    if (object instanceof ClientValue && args.every((x) => x.value instanceof ClientValue)) {
      const values = args.map((x) => (x.value as ClientValue).value);
      return new ClientValue((object.value as any)[key](...values));
    }
    const string = b.toNode(object, STRING, node);
    const text = (i: number) => b.toNode(args[i].value, STRING, args[i].at, `for ${key}`);
    switch (key) {
      case "toUpperCase":
        return b.node("StringToUpper", [string], STRING);
      case "toLowerCase":
        return b.node("StringToLower", [string], STRING);
      case "trim":
        return b.node("StringTrim", [string], STRING);
      case "trimStart":
        return b.node("StringTrimStart", [string], STRING);
      case "trimEnd":
        return b.node("StringTrimEnd", [string], STRING);
      case "includes":
        return b.node("StringContains", [string, text(0)], BOOL);
      case "startsWith":
        return b.node("StringStartsWith", [string, text(0)], BOOL);
      case "endsWith":
        return b.node("StringEndsWith", [string, text(0)], BOOL);
      case "indexOf":
        return b.node("StringIndexOf", [string, text(0)], SINT32);
      case "replaceAll":
        return b.node(
          "StringReplace",
          [string, this.#search(args[0]), this.#replacement(args[1])],
          STRING,
        );
      case "split":
        return b.node("StringSplit", [string, text(0)], {
          kind: "list",
          item: STRING,
          nullable: false,
        });
      case "at":
        return this.#at(string, args[0], node);
      default:
        return this.#slice(string, key, args, node);
    }
  }

  /** The search string of `replaceAll`, which the server cannot match when empty. */
  #search(arg: Argument): Node {
    const b = this.#b;
    if (arg.value instanceof ClientValue && arg.value.value === "") {
      throw b.error(
        arg.at,
        "replaceAll of an empty string is not supported in a compiled function",
      );
    }
    return b.toNode(arg.value, STRING, arg.at, "for replaceAll");
  }

  /** The replacement of `replaceAll`, a constant with no $, as the server applies no replacement
   * patterns. */
  #replacement(arg: Argument): Node {
    const b = this.#b;
    if (!(arg.value instanceof ClientValue && typeof arg.value.value === "string")) {
      throw b.error(
        arg.at,
        "The replacement of replaceAll must be a string constant in a compiled function",
      );
    }
    if (arg.value.value.includes("$")) {
      throw b.error(arg.at, "A replacement pattern with $ is not supported in a compiled function");
    }
    return b.toNode(arg.value, STRING, arg.at, "for replaceAll");
  }

  /** `s.at(i)`, where a negative index counts back from the end. */
  #at(string: Node, index: Argument, node: CallExpression): Value {
    const b = this.#b;
    // A constant index is truncated toward zero, with NaN as zero, as JavaScript reads it
    const constant =
      index.value instanceof ClientValue && typeof index.value.value === "number"
        ? Math.trunc(index.value.value) || 0
        : null;
    const i =
      constant === null
        ? b.toNode(index.value, SINT32, index.at, "as an index")
        : b.constant(constant, SINT32, index.at);
    if (constant !== null && constant >= 0) {
      return b.node("StringGet", [string, i], STRING);
    }
    return b.once(string, (s) =>
      b.once(i, (j) => {
        const fromEnd = b.node("Add", [b.node("StringLength", [s], SINT32), j], SINT32);
        const position =
          constant !== null
            ? fromEnd
            : b.node(
                "Conditional",
                [b.node("LessThan", [j, b.constant(0, SINT32, node)], BOOL), fromEnd, j],
                SINT32,
              );
        return b.node("StringGet", [s, position], STRING);
      }),
    );
  }

  /** An index as JavaScript reads it, truncated toward zero, with NaN as zero. */
  #integer(value: Node, node: SyntaxNode): Node {
    const b = this.#b;
    const zero = b.constant(0, DOUBLE, node);
    return b.once(value, (v) =>
      b.node(
        "Conditional",
        [
          b.stdlib("IsNan", [v]),
          zero,
          b.node(
            "Conditional",
            [b.node("LessThan", [v, zero], BOOL), b.stdlib("Ceiling", [v]), b.stdlib("Floor", [v])],
            DOUBLE,
          ),
        ],
        DOUBLE,
      ),
    );
  }

  /** `s.slice(start, end)` and `s.substring(start, end)`, with the indices clamped to the string
   * as JavaScript clamps them. A negative index counts back from the end for `slice`, and is zero
   * for `substring`, which also swaps indices given in reverse. */
  #slice(string: Node, key: string, args: readonly Argument[], node: CallExpression): Value {
    const b = this.#b;
    return b.once(string, (s) => {
      const length = b.cast(b.node("StringLength", [s], SINT32), DOUBLE);
      const index = (x: Argument | undefined, fallback: Node): Node => {
        if (x === undefined) {
          return fallback;
        }
        const value =
          x.value instanceof ClientValue && typeof x.value.value === "number"
            ? b.constant(Math.trunc(x.value.value) || 0, DOUBLE, x.at)
            : this.#integer(b.double(b.resolved(x.value, x.at), x.at), node);
        const zero = b.constant(0, DOUBLE, node);
        if (key === "substring") {
          return b.stdlib("Clamp", [value, zero, length]);
        }
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
      return b.once(index(args[0], b.constant(0, DOUBLE, node)), (start) =>
        b.once(index(args[1], length), (end) => {
          const toInt = (x: Node) => b.cast(x, SINT32);
          if (key === "substring") {
            const from = b.stdlib("Min", [start, end]);
            const count = b.stdlib("Abs", [b.node("Subtract", [end, start], DOUBLE)]);
            return b.node("StringSubstring", [s, toInt(from), toInt(count)], STRING);
          }
          const count = b.stdlib("Max", [
            b.node("Subtract", [end, start], DOUBLE),
            b.constant(0, DOUBLE, node),
          ]);
          return b.node("StringSubstring", [s, toInt(start), toInt(count)], STRING);
        }),
      );
    });
  }

  /** The methods of a value of a flags enumeration. */
  #flagsMethod(object: Value, key: string, node: CallExpression): Value {
    const b = this.#b;
    const args = this.#arguments(node.arguments);
    if (!["union", "intersection", "difference", "contains"].includes(key)) {
      throw b.error(
        node,
        `Method ${key} of a flags enumeration is not supported in a compiled function`,
      );
    }
    if (args.length !== 1) {
      throw b.error(node, `${key} takes one argument`);
    }
    const a = b.toNode(object, undefined, node);
    const type = a.type as NonNullable<Node["type"]>;
    const other = b.toNode(args[0].value, withNullable(type, false), args[0].at, `for ${key}`);
    b.notNull(other, args[0].at);
    switch (key) {
      case "union":
        return b.node("Or", [a, other], type);
      case "intersection":
        return b.node("And", [a, other], type);
      case "difference":
        return b.node("And", [a, b.node("Not", [other], type)], type);
      default:
        return b.once(other, (o) => b.node("Equal", [b.node("And", [a, o], type), o], BOOL));
    }
  }

  // Operators

  #unary(node: UnaryExpression): Value {
    const b = this.#b;
    const operator = node.operator;
    if (operator === "typeof" || operator === "void" || operator === "delete") {
      throw b.error(node, `${operator} is not supported in a compiled function`);
    }
    const value = this.expression(node.argument);
    if (operator === "!") {
      const truth = b.truth(value, node.argument);
      return truth instanceof ClientValue
        ? new ClientValue(!truth.value)
        : b.node("Not", [truth], BOOL);
    }
    if (value instanceof ClientValue) {
      return this.#foldUnary(operator, value, node);
    }
    b.notNull(value, node.argument);
    const type = value.type;
    if (operator === "~") {
      if (isBigint(type) || (type?.kind === "enumeration" && type.enumeration.flags)) {
        return b.node("Not", [value], type);
      }
      return b.node("Not", [b.int32(value, node.argument)], SINT32);
    }
    if (isBigint(type)) {
      if (operator === "+") {
        throw b.error(node, "Unary + of a bigint is a TypeError");
      }
      if (type?.kind === "uint64") {
        throw b.error(node, "Negating a uint64 is not supported in a compiled function");
      }
      return b.node("Negate", [value], type);
    }
    const number = b.double(value, node.argument);
    return operator === "+" ? number : b.node("Negate", [number], DOUBLE);
  }

  #foldUnary(operator: string, value: ClientValue, node: UnaryExpression): Value {
    const v = value.value as any;
    if (
      typeof v !== "number" &&
      typeof v !== "bigint" &&
      !(operator === "~" && v instanceof RemoteFlags)
    ) {
      throw this.#b.error(
        node,
        `${operator} of ${describe(v)} is not supported in a compiled function`,
      );
    }
    if (v instanceof RemoteFlags) {
      return new ClientValue(
        v.enumeration.fromNumber(
          ~v.value & Object.values(v.enumeration.values).reduce((m, x) => m | x.value, 0),
        ),
      );
    }
    try {
      return new ClientValue(UNARY_OPERATORS[operator](v));
    } catch (error) {
      throw this.#b.error(node, (error as Error).message);
    }
  }

  #binary(node: BinaryExpression): Value {
    if (node.operator === "in" || node.operator === "instanceof") {
      throw this.#b.error(node, `${node.operator} is not supported in a compiled function`);
    }
    const left = this.expression(node.left);
    const right = this.expression(node.right);
    return this.binaryValues(node.operator, left, right, node, [node.left, node.right]);
  }

  /** The value of a binary operator, such as `+`, on two values. */
  binaryValues(
    operator: string,
    left: Value,
    right: Value,
    node: SyntaxNode,
    at: readonly [SyntaxNode, SyntaxNode] = [node, node],
  ): Value {
    const b = this.#b;
    if (
      left instanceof ClientValue &&
      right instanceof ClientValue &&
      isPrimitive(left.value) &&
      isPrimitive(right.value)
    ) {
      try {
        return new ClientValue(OPERATORS[operator](left.value, right.value));
      } catch (error) {
        throw b.error(node, (error as Error).message);
      }
    }
    if (operator === "+" && (this.#isString(left) || this.#isString(right))) {
      return concat(b, [b.string(left, at[0]), b.string(right, at[1])], node);
    }
    const arithmetic = ARITHMETIC[operator];
    if (arithmetic !== undefined) {
      if (this.#isBigint(left) || this.#isBigint(right)) {
        const [x, y] = this.#bigints(left, right, at);
        return b.node(arithmetic, [x, y], x.type);
      }
      return b.node(arithmetic, [b.double(left, at[0]), b.double(right, at[1])], DOUBLE);
    }
    const bitwise = BITWISE[operator];
    if (bitwise !== undefined) {
      if (this.#isBigint(left) || this.#isBigint(right)) {
        const [x, y] = this.#bigints(left, right, at);
        return b.node(bitwise, [x, y], x.type);
      }
      const flags = this.#flags(left, right, at);
      if (flags !== null) {
        return b.node(bitwise, flags, flags[0].type);
      }
      return b.node(bitwise, [b.int32(left, at[0]), b.int32(right, at[1])], SINT32);
    }
    if (operator === "<<" || operator === ">>" || operator === ">>>") {
      if (this.#isBigint(left) || this.#isBigint(right)) {
        if (operator === ">>>") {
          throw b.error(node, ">>> of a bigint is a TypeError");
        }
        const [x, y] = this.#bigints(left, right, at);
        return this.#bigintShift(operator === "<<", x, y, right, node);
      }
      const value = b.int32(left, at[0]);
      const count = b.int32(right, at[1]);
      if (operator === ">>>") {
        return b.node("RightShift", [b.cast(value, UINT32), count], UINT32);
      }
      return b.node(operator === "<<" ? "LeftShift" : "RightShift", [value, count], SINT32);
    }
    if (operator === "===" || operator === "!==" || operator === "==" || operator === "!=") {
      const strict = operator === "===" || operator === "!==";
      const equal = this.#equal(left, right, strict, node, at);
      if (operator === "===" || operator === "==") {
        return equal;
      }
      return equal instanceof ClientValue
        ? new ClientValue(!equal.value)
        : b.node("Not", [equal], BOOL);
    }
    return this.#relational(RELATIONAL[operator], left, right, node, at);
  }

  /** A shift of a bigint, by a count the server masks to six bits and JavaScript does not. A
   * count of 64 or more shifts every bit out, and a negative count shifts the other way. */
  #bigintShift(leftward: boolean, x: Node, y: Node, count: Value, node: SyntaxNode): Node {
    const b = this.#b;
    const type = x.type as Type;
    const signed = type.kind === "sint64";
    const constant = (value: bigint) => b.constant(value, type, node);
    const shift = (left: boolean, value: Node, by: Node) =>
      b.node(left ? "LeftShift" : "RightShift", [value, by], type);
    // Every bit shifted out, leaving zero, or the sign of a negative value shifted right
    const out = (left: boolean, value: Node) =>
      left || !signed
        ? constant(0n)
        : b.node(
            "Conditional",
            [b.node("LessThan", [value, constant(0n)], BOOL), constant(-1n), constant(0n)],
            type,
          );
    if (count instanceof ClientValue) {
      const c = count.value as bigint;
      if (c >= 64n) {
        return out(leftward, x);
      }
      if (c <= -64n) {
        return out(!leftward, x);
      }
      return c < 0n ? shift(!leftward, x, constant(-c)) : shift(leftward, x, y);
    }
    return b.once(x, (v) =>
      b.once(y, (c) => {
        const forward = b.node(
          "Conditional",
          [
            b.node("GreaterThanOrEqual", [c, constant(64n)], BOOL),
            out(leftward, v),
            shift(leftward, v, c),
          ],
          type,
        );
        if (!signed) {
          return forward;
        }
        const back = b.node(
          "Conditional",
          [
            b.node("LessThanOrEqual", [c, constant(-64n)], BOOL),
            out(!leftward, v),
            shift(!leftward, v, b.node("Negate", [c], type)),
          ],
          type,
        );
        return b.node(
          "Conditional",
          [b.node("LessThan", [c, constant(0n)], BOOL), back, forward],
          type,
        );
      }),
    );
  }

  #isString(value: Value): boolean {
    return value instanceof ClientValue
      ? typeof value.value === "string"
      : value.type?.kind === "string";
  }

  #isBigint(value: Value): boolean {
    return value instanceof ClientValue ? typeof value.value === "bigint" : isBigint(value.type);
  }

  /** Two bigints as nodes of one integer type. */
  #bigints(left: Value, right: Value, at: readonly [SyntaxNode, SyntaxNode]): [Node, Node] {
    const b = this.#b;
    for (const [value, node] of [
      [left, at[0]],
      [right, at[1]],
    ] as const) {
      if (!this.#isBigint(value)) {
        throw b.error(
          node,
          "A bigint and a number cannot be mixed. Convert one with BigInt() or Number()",
        );
      }
      if (value instanceof Node) {
        b.notNull(value, node);
      }
    }
    const [x, y] = b.join(left, right, at, "operands");
    if (x.type?.kind !== y.type?.kind) {
      throw b.error(at[0], "An int64 and a uint64 cannot be mixed in a compiled function");
    }
    return [x, y];
  }

  /** Two values of one flags enumeration, or null for operands that are not. */
  #flags(left: Value, right: Value, at: readonly [SyntaxNode, SyntaxNode]): [Node, Node] | null {
    const isFlags = (value: Value) =>
      value instanceof ClientValue
        ? value.value instanceof RemoteFlags
        : value.type?.kind === "enumeration" && value.type.enumeration.flags;
    if (!isFlags(left) && !isFlags(right)) {
      return null;
    }
    const b = this.#b;
    const [x, y] = b.join(left, right, at, "operands");
    b.notNull(x, at[0]);
    b.notNull(y, at[1]);
    return [x, y];
  }

  #equal(
    left: Value,
    right: Value,
    strict: boolean,
    node: SyntaxNode,
    at: readonly [SyntaxNode, SyntaxNode],
  ): Value {
    const b = this.#b;
    if (isNullish(left) || isNullish(right)) {
      const [other, where] = isNullish(left) ? [right, at[1]] : [left, at[0]];
      if (other instanceof ClientValue) {
        const values = [(left as ClientValue).value, (right as ClientValue).value];
        return new ClientValue(OPERATORS[strict ? "===" : "=="](values[0], values[1]));
      }
      if (other.type === null) {
        throw b.error(where, "The expression has no value");
      }
      if (!other.type.nullable) {
        return new ClientValue(false);
      }
      // The server has one null, which a strict comparison cannot tell from undefined
      if (strict) {
        throw b.error(node, "Compare a server value with null using == null or != null");
      }
      return b.node("IsNull", [other], BOOL);
    }
    if (left instanceof ClientValue && right instanceof ClientValue) {
      return new ClientValue(left.value === right.value);
    }
    if (this.#isBigint(left) !== this.#isBigint(right)) {
      throw b.error(
        node,
        "A bigint and a number are never equal with ===. Convert one with BigInt() or Number()",
      );
    }
    const [x, y] = b.join(left, right, at, "operands of the comparison");
    const kind = x.type?.kind;
    if (
      kind === "struct" ||
      kind === "list" ||
      kind === "set" ||
      kind === "tuple" ||
      kind === "dictionary"
    ) {
      throw b.error(
        node,
        `A ${typeName(x.type)} compares by identity, which a compiled function cannot compare`,
      );
    }
    return b.node("Equal", [x, y], BOOL);
  }

  #relational(
    method: string,
    left: Value,
    right: Value,
    _node: SyntaxNode,
    at: readonly [SyntaxNode, SyntaxNode],
  ): Value {
    const b = this.#b;
    if (this.#isBigint(left) || this.#isBigint(right)) {
      const [x, y] = this.#bigints(left, right, at);
      return b.node(method, [x, y], BOOL);
    }
    for (const [value, syntax] of [
      [left, at[0]],
      [right, at[1]],
    ] as const) {
      const isANumber =
        value instanceof ClientValue ? typeof value.value === "number" : isNumber(value.type);
      if (!isANumber) {
        const what =
          value instanceof ClientValue ? describe(value.value) : `A ${typeName(value.type)}`;
        throw b.error(
          syntax,
          `${what} cannot be ordered in a compiled function. Only numbers and bigints can`,
        );
      }
      if (value instanceof Node) {
        b.notNull(value, syntax);
      }
    }
    const [x, y] = b.join(left, right, at, "operands of the comparison");
    return b.node(method, [x, y], BOOL);
  }

  #logical(node: LogicalExpression): Value {
    const b = this.#b;
    const left = this.expression(node.left);
    const operator = node.operator;
    if (left instanceof ClientValue) {
      const v = left.value;
      const takeLeft =
        operator === "&&" ? !v : operator === "||" ? Boolean(v) : v !== null && v !== undefined;
      return takeLeft ? left : this.expression(node.right);
    }
    if (left.type === null) {
      throw b.error(node.left, "The expression has no value");
    }
    const right = this.expression(node.right);
    const at: [SyntaxNode, SyntaxNode] = [node.left, node.right];
    if (operator === "??") {
      const type = left.type;
      if (!type.nullable) {
        return left;
      }
      return b.once(left, (plain) => {
        const [x, y] = b.join(
          b.cast(plain, withNullable(type, false)),
          right,
          at,
          "operands of ??",
        );
        const result = withNullable(
          x.type as NonNullable<Node["type"]>,
          x.type?.nullable || (y.type?.nullable ?? false),
        );
        return b.node("Conditional", [b.node("IsNull", [plain], BOOL), y, x], result);
      });
    }
    const isBool = (value: Value) =>
      value instanceof ClientValue
        ? typeof value.value === "boolean"
        : value.type?.kind === "bool" && !value.type.nullable;
    if (isBool(left) && isBool(right)) {
      const method = operator === "&&" ? "ConditionalAnd" : "ConditionalOr";
      return b.node(method, [left, b.toNode(right, BOOL, node.right)], BOOL);
    }
    const [x, y] = b.join(left, right, at, `operands of ${operator}`);
    const type = withNullable(
      x.type as NonNullable<Node["type"]>,
      (x.type?.nullable || y.type?.nullable) ?? false,
    );
    return b.once(x, (plain) => {
      const truth = b.toNode(b.truth(plain, node.left), BOOL, node.left);
      const [ifTrue, ifFalse] = operator === "&&" ? [y, plain] : [plain, y];
      return b.node("Conditional", [truth, ifTrue, ifFalse], type);
    });
  }

  #conditional(node: ConditionalExpression): Value {
    const b = this.#b;
    const test = b.truth(this.expression(node.test), node.test);
    if (test instanceof ClientValue) {
      return this.expression(test.value ? node.consequent : node.alternate);
    }
    const consequent = this.operand(node.consequent);
    const alternate = this.operand(node.alternate);
    const tc = b.typeOf(consequent);
    const ta = b.typeOf(alternate);
    if (tc === null && ta === null) {
      return b.node("Conditional", [test, consequent, alternate], null);
    }
    const [x, y] = b.join(
      consequent,
      alternate,
      [node.consequent, node.alternate],
      "branches of the conditional",
    );
    const type = withNullable(
      x.type as NonNullable<Node["type"]>,
      (x.type?.nullable || y.type?.nullable) ?? false,
    );
    return b.node("Conditional", [test, x, y], type);
  }
}

/** Whether a captured value is a collection or a structure, which is a constant node. */
function isClientCollection(value: unknown): boolean {
  return (
    Array.isArray(value) ||
    value instanceof Set ||
    value instanceof Map ||
    value instanceof RemoteStruct
  );
}

function isPrimitive(value: unknown): boolean {
  return value === null || (typeof value !== "object" && typeof value !== "function");
}
