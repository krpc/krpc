import { create } from "@bufbuild/protobuf";
import type { Node as SyntaxNode } from "acorn";
import { CompilationError } from "../errors.js";
import { camelCase } from "../naming.js";
import { RemoteObject } from "../objects.js";
import { namedOf } from "../registry.js";
import {
  ArgumentSchema,
  type Argument as CallArgument,
  ProcedureCallSchema,
} from "../schema/krpc_pb.js";
import { RemoteEnum, type Type, type Types, UnknownTypeError } from "../types.js";
import { ClientValue, Node, TypeArgument, type Value } from "./ir.js";
import type { Member, MemberIndex } from "./members.js";
import { at } from "./parse.js";
import {
  BOOL,
  classType,
  DOUBLE,
  isBigint,
  isNumber,
  SINT32,
  STRING,
  sameType,
  scalar,
  typeName,
  withNullable,
} from "./typing.js";

/** What the compiler reads from a connection. */
export interface Environment {
  readonly types: Types;
  readonly members: MemberIndex;
  /** Warns of the use of a deprecated member. */
  readonly warn: (message: string) => void;
}

/** A value passed to a procedure, with the syntax it was compiled from. */
export interface Argument {
  readonly value: Value;
  readonly at: SyntaxNode;
}

// The range of each integer type, as its smallest value and one past its largest
const INTEGER_RANGES: Readonly<Record<string, readonly [bigint, bigint]>> = {
  sint32: [-(2n ** 31n), 2n ** 31n],
  uint32: [0n, 2n ** 32n],
  sint64: [-(2n ** 63n), 2n ** 63n],
  uint64: [0n, 2n ** 64n],
};

const CONSTANTS: Readonly<Record<string, string>> = {
  double: "ConstantDouble",
  float: "ConstantFloat",
  sint32: "ConstantInt",
  uint32: "ConstantUInt",
  sint64: "ConstantLong",
  uint64: "ConstantULong",
};

/** Builds the typed tree of a function. Each method raises a CompilationError at the syntax it
 * is given, for a construct the server cannot compute. */
export class Builder {
  readonly env: Environment;
  readonly #offset: number;
  /** The calls of procedures the function makes, other than to StdLib. */
  remoteCalls = 0;

  constructor(env: Environment, offset: number) {
    this.env = env;
    this.#offset = offset;
  }

  error(node: SyntaxNode, message: string): CompilationError {
    return at(node, this.#offset, message);
  }

  node(method: string, args: readonly (Node | TypeArgument | unknown)[], type: Type | null): Node {
    return new Node(method, args as Node[], type);
  }

  /** A constant of a value computed on the client, of the given type or the type the value
   * gives it. */
  constant(value: unknown, target: Type | undefined, node: SyntaxNode, where = ""): Node {
    const suffix = where === "" ? "" : ` ${where}`;
    if (value === null || value === undefined) {
      if (target === undefined) {
        throw this.error(
          node,
          `null has no type here${suffix}. Give it one with a cast, such as types.nullable(types.double).cast(null)`,
        );
      }
      return this.node("ConstantNull", [new TypeArgument(target)], withNullable(target, true));
    }
    const mismatch = (description: string) =>
      this.error(node, `Cannot use ${description} as a ${typeName(target ?? null)}${suffix}`);
    switch (typeof value) {
      case "number": {
        const kind = target?.kind ?? "double";
        if (kind === "double" || kind === "float") {
          return this.node(CONSTANTS[kind], [value], scalar(kind));
        }
        if (!(kind in INTEGER_RANGES)) {
          throw mismatch("a number");
        }
        if (!Number.isFinite(value)) {
          throw this.error(node, `${value} is not a ${typeName(target ?? null)}${suffix}`);
        }
        return this.#integer(BigInt(Math.trunc(value)), kind, node, suffix);
      }
      case "bigint": {
        let kind = target?.kind ?? "sint64";
        if (target === undefined && value >= INTEGER_RANGES.sint64[1]) {
          kind = "uint64";
        }
        if (kind !== "sint64" && kind !== "uint64") {
          throw mismatch("a bigint");
        }
        return this.#integer(value, kind, node, suffix);
      }
      case "boolean":
        if (target !== undefined && target.kind !== "bool") {
          throw mismatch("a boolean");
        }
        return this.node("ConstantBool", [value], BOOL);
      case "string":
        if (target !== undefined && target.kind !== "string") {
          throw mismatch("a string");
        }
        return this.node("ConstantString", [value], STRING);
      default:
        break;
    }
    if (value instanceof Uint8Array) {
      if (target !== undefined && target.kind !== "bytes") {
        throw mismatch("a Uint8Array");
      }
      return this.node("ConstantBytes", [value], scalar("bytes"));
    }
    if (value instanceof RemoteEnum) {
      const { service, name } = value.enumeration;
      const enumeration = this.env.types.enumeration(service, name);
      if (enumeration === undefined) {
        throw this.error(node, `The server has no enumeration ${service}.${name}`);
      }
      const type: Type = { kind: "enumeration", enumeration, nullable: false };
      if (target !== undefined && !sameType(type, target, true)) {
        throw mismatch(`a ${typeName(type)}`);
      }
      return this.node("ConstantEnum", [service, name, value.value], type);
    }
    if (value instanceof RemoteObject) {
      const { service, name } = RemoteObject.classOf(value);
      let type: Type;
      try {
        type = classType(this.env.types, service, name);
      } catch (error) {
        if (error instanceof UnknownTypeError) {
          throw this.error(node, `The server has no class ${service}.${name}`);
        }
        throw error;
      }
      if (service === "KRPC") {
        throw this.error(node, `A ${service}.${name} cannot be a value in a function`);
      }
      return this.node("ConstantObject", [RemoteObject.idOf(value)], type);
    }
    throw this.error(node, `${describe(value)} cannot be a value in a function${suffix}`);
  }

  #integer(value: bigint, kind: string, node: SyntaxNode, suffix: string): Node {
    const [low, high] = INTEGER_RANGES[kind];
    if (value < low || value >= high) {
      throw this.error(
        node,
        `${value} is out of range for a ${typeName(scalar(kind as "sint32"))}${suffix}`,
      );
    }
    const argument = kind === "sint32" || kind === "uint32" ? Number(value) : value;
    return this.node(CONSTANTS[kind], [argument], scalar(kind as "sint32"));
  }

  /** A node of a value, converted to the given type. A constant takes the type, a number is
   * converted to the integer or floating point type, and a value of another type is a
   * CompilationError. With no type, a constant takes the type its value gives it. */
  toNode(value: Value, target: Type | undefined, node: SyntaxNode, where = ""): Node {
    if (value instanceof ClientValue) {
      return this.constant(value.value, target, node, where);
    }
    if (value.type === null) {
      throw this.error(node, `The expression has no value`);
    }
    if (target === undefined || sameType(value.type, target, true)) {
      return value;
    }
    const source = value.type;
    if (
      (isNumber(source) && (isNumber(target) || isBigint(target))) ||
      (isBigint(source) && isBigint(target))
    ) {
      return this.cast(value, withNullable(target, source.nullable && target.nullable));
    }
    // The server checks that an object is of the class, or a class derived from it
    if (source.kind === "class" && target.kind === "class") {
      return value;
    }
    const suffix = where === "" ? "" : ` ${where}`;
    throw this.error(node, `Cannot use a ${typeName(source)} as a ${typeName(target)}${suffix}`);
  }

  cast(value: Node, type: Type): Node {
    return this.node("Cast", [value, new TypeArgument(type)], type);
  }

  /** A node that evaluates a value once, and gives it to the body as a node that can be read
   * any number of times. */
  once(value: Node, body: (plain: Node) => Node): Node {
    if (value.isPlain) {
      return body(value);
    }
    const variable = this.node(
      "Variable",
      ["value", new TypeArgument(value.type as Type)],
      value.type,
    );
    const result = body(variable);
    return this.node(
      "BlockWithVariables",
      [[variable], [this.node("Assign", [variable, value], null), result]],
      result.type,
    );
  }

  /** The type of a value, or undefined for a value computed on the client. */
  typeOf(value: Value): Type | null | undefined {
    return value instanceof Node ? value.type : undefined;
  }

  /** A number as a double. A nullable number is a CompilationError, as null is not a number. */
  double(value: Value, node: SyntaxNode): Node {
    if (value instanceof ClientValue) {
      if (typeof value.value !== "number") {
        throw this.error(node, `${describe(value.value)} is not a number`);
      }
      return this.constant(value.value, DOUBLE, node);
    }
    this.notNull(value, node);
    if (!isNumber(value.type)) {
      throw this.error(node, `A ${typeName(value.type)} is not a number`);
    }
    return value.type?.kind === "double" ? value : this.cast(value, DOUBLE);
  }

  /** A CompilationError for a node of a value that can be null. */
  notNull(value: Node, node: SyntaxNode): void {
    if (value.type?.nullable) {
      throw this.error(
        node,
        `The expression can be null. Give a value for null with ??, such as (x ?? 0)`,
      );
    }
  }

  /** A number converted to a 32-bit integer, as the bitwise operators convert it. */
  int32(value: Value, node: SyntaxNode): Node {
    if (value instanceof ClientValue) {
      if (typeof value.value !== "number") {
        throw this.error(node, `${describe(value.value)} is not a number`);
      }
      return this.constant(value.value | 0, SINT32, node);
    }
    this.notNull(value, node);
    const type = value.type as Type;
    if (!isNumber(type)) {
      throw this.error(node, `A ${typeName(type)} is not a number`);
    }
    if (type.kind === "sint32") {
      return value;
    }
    if (type.kind === "uint32") {
      return this.cast(value, SINT32);
    }
    // Through a 64-bit integer, which wraps to the low 32 bits as JavaScript does
    return this.cast(this.cast(value, scalar("sint64")), SINT32);
  }

  /** Whether a value is true, as JavaScript tests it. */
  truth(value: Value, node: SyntaxNode): Value {
    if (value instanceof ClientValue) {
      return new ClientValue(Boolean(value.value));
    }
    const type = value.type;
    if (type === null) {
      throw this.error(node, `The expression has no value`);
    }
    if (type.kind === "bool" && !type.nullable) {
      return value;
    }
    return this.once(value, (plain) => {
      const notNull = type.nullable
        ? this.node("Not", [this.node("IsNull", [plain], BOOL)], BOOL)
        : null;
      const inner = type.nullable ? this.cast(plain, withNullable(type, false)) : plain;
      const truth = this.#truthOf(inner, withNullable(type, false), node);
      if (notNull === null) {
        return truth ?? this.constant(true, BOOL, node);
      }
      return truth === null ? notNull : this.node("ConditionalAnd", [notNull, truth], BOOL);
    });
  }

  /** Whether a value that is not null is true, or null for a value that always is. */
  #truthOf(value: Node, type: Type, node: SyntaxNode): Node | null {
    switch (type.kind) {
      case "bool":
        return value;
      case "double":
      case "float": {
        // False for zero and NaN alike
        const magnitude = this.stdlib("Abs", [this.toNode(value, DOUBLE, node)]);
        return this.node("GreaterThan", [magnitude, this.constant(0, DOUBLE, node)], BOOL);
      }
      case "sint32":
      case "uint32":
      case "sint64":
      case "uint64":
        return this.node("NotEqual", [value, this.constant(0, type, node)], BOOL);
      case "string":
        return this.node(
          "GreaterThan",
          [this.node("StringLength", [value], SINT32), this.constant(0, SINT32, node)],
          BOOL,
        );
      default:
        return null;
    }
  }

  /** Two values as nodes of one type, as the branches of a conditional or the operands of `&&`,
   * `||` and `??` are. Numbers of differing types are doubles, and null takes the type of the
   * other value. */
  join(a: Value, b: Value, nodes: readonly [SyntaxNode, SyntaxNode], what: string): [Node, Node] {
    const [x, y] = nodes;
    const ta = this.typeOf(a);
    const tb = this.typeOf(b);
    if (ta === null || tb === null) {
      throw this.error(ta === null ? x : y, `The expression has no value`);
    }
    if (ta === undefined && tb === undefined) {
      const na = isNullish(a) ? null : this.toNode(a, undefined, x);
      const nb = isNullish(b) ? null : this.toNode(b, undefined, y);
      if (na === null && nb === null) {
        throw this.error(
          x,
          `null has no type here. Give it one with a cast, such as types.nullable(types.double).cast(null)`,
        );
      }
      return this.join(na ?? a, nb ?? b, nodes, what);
    }
    if (ta === undefined || tb === undefined) {
      const [constant, node, other] = ta === undefined ? [a, x, b as Node] : [b, y, a as Node];
      const value = (constant as ClientValue).value;
      const type = other.type as Type;
      // A number that is not exact at an integer type makes both doubles
      if (
        typeof value === "number" &&
        isNumber(type) &&
        type.kind !== "double" &&
        !(Number.isInteger(value) && inRange(value, type.kind))
      ) {
        const widened = this.cast(other, withNullable(DOUBLE, type.nullable));
        const number = this.constant(value, DOUBLE, node);
        return ta === undefined ? [number, widened] : [widened, number];
      }
      const converted = this.#joinConstant(constant as ClientValue, type, node, what);
      return ta === undefined ? [converted, other] : [other, converted];
    }
    if (sameType(ta, tb, true)) {
      return [a as Node, b as Node];
    }
    if (isNumber(ta) && isNumber(tb)) {
      const toDouble = (n: Node, t: Type) =>
        t.kind === "double" ? n : this.cast(n, withNullable(DOUBLE, t.nullable));
      return [toDouble(a as Node, ta), toDouble(b as Node, tb)];
    }
    throw this.error(
      x,
      `The ${what} are a ${typeName(ta)} and a ${typeName(tb)}, which have no common type`,
    );
  }

  #joinConstant(value: ClientValue, other: Type, node: SyntaxNode, what: string): Node {
    if (value.value === null || value.value === undefined) {
      return this.constant(null, withNullable(other, false), node);
    }
    try {
      return this.constant(value.value, withNullable(other, false), node);
    } catch (error) {
      if (error instanceof CompilationError) {
        throw this.error(
          node,
          `The ${what} are ${describe(value.value)} and a ${typeName(other)}, which have no common type`,
        );
      }
      throw error;
    }
  }

  /** The string of a value, as `String` gives it. Only strings, numbers, bigints and booleans
   * have one on the server. A number converts as the server formats it. */
  string(value: Value, node: SyntaxNode): Value {
    if (value instanceof ClientValue) {
      const v = value.value;
      if (
        v === null ||
        v === undefined ||
        ["string", "number", "bigint", "boolean"].includes(typeof v)
      ) {
        return new ClientValue(String(v));
      }
      throw this.error(node, `${describe(v)} has no string in a function`);
    }
    const type = value.type;
    if (type === null) {
      throw this.error(node, `The expression has no value`);
    }
    if (type.kind === "string" && !type.nullable) {
      return value;
    }
    if (!(isNumber(type) || isBigint(type) || type.kind === "string" || type.kind === "bool")) {
      throw this.error(
        node,
        `A ${typeName(type)} has no string in a function. Only strings, numbers, bigints and booleans do`,
      );
    }
    return this.once(value, (plain) => {
      const inner = type.nullable ? this.cast(plain, withNullable(type, false)) : plain;
      let text: Node;
      if (type.kind === "string") {
        text = inner;
      } else if (type.kind === "bool") {
        text = this.node(
          "Conditional",
          [inner, this.constant("true", STRING, node), this.constant("false", STRING, node)],
          STRING,
        );
      } else {
        text = this.node("ConvertToString", [inner], STRING);
      }
      if (!type.nullable) {
        return text;
      }
      return this.node(
        "Conditional",
        [this.node("IsNull", [plain], BOOL), this.constant("null", STRING, node), text],
        STRING,
      );
    });
  }

  /** A call of a procedure. The arguments are in the order of its parameters, after the
   * object for a member of a class, followed by those passed by name. */
  call(
    member: Member,
    instance: Node | null,
    args: readonly Argument[],
    named: ReadonlyMap<string, Argument>,
    node: SyntaxNode,
  ): Node {
    const name = displayName(member);
    if (member.deprecation !== undefined) {
      this.env.warn(member.deprecation);
    }
    const parameters = member.parameters;
    const first = instance === null ? 0 : 1;
    if (first + args.length > parameters.length) {
      throw this.error(
        node,
        `${name} takes ${parameters.length - first} arguments, and was passed ${args.length}`,
      );
    }
    const given = new Map<number, Argument>();
    args.forEach((x, i) => {
      given.set(first + i, x);
    });
    for (const [key, x] of named) {
      const position = parameters.findIndex((p, i) => i >= first && p.name === key);
      if (position === -1) {
        throw this.error(x.at, `${name} has no parameter ${key}`);
      }
      if (given.has(position)) {
        throw this.error(x.at, `${name} was passed argument ${key} twice`);
      }
      given.set(position, x);
    }
    const expressions = new Map<number, Node>();
    const nulls: CallArgument[] = [];
    if (instance !== null) {
      expressions.set(0, instance);
    }
    parameters.forEach((parameter, position) => {
      if (position < first) {
        return;
      }
      const x = given.get(position);
      if (x === undefined) {
        if (!parameter.optional) {
          throw this.error(node, `${name} is missing argument ${parameter.name}`);
        }
        return;
      }
      if (isNullish(x.value)) {
        if (!parameter.type.nullable) {
          throw this.error(x.at, `Argument ${parameter.name} of ${name} cannot be null`);
        }
        nulls.push(create(ArgumentSchema, { position, isNull: true }));
        return;
      }
      expressions.set(
        position,
        this.toNode(x.value, parameter.type, x.at, `for argument ${parameter.name} of ${name}`),
      );
    });
    if (member.service !== "StdLib") {
      this.remoteCalls++;
    }
    const call = create(ProcedureCallSchema, {
      service: member.service,
      procedure: member.procedure.name,
      arguments: nulls,
    });
    return this.node("CallWithArguments", [call, expressions], member.result);
  }

  /** A call of a StdLib procedure, with arguments that are already of its parameter types. */
  stdlib(name: string, args: readonly Node[]): Node {
    const member = this.env.members.ofService("StdLib")?.methods.get(camelCase(name));
    if (member === undefined) {
      throw new CompilationError(`The server has no procedure StdLib.${name}`);
    }
    const expressions = new Map<number, Node>(args.map((x, i) => [i, x]));
    const call = create(ProcedureCallSchema, {
      service: "StdLib",
      procedure: member.procedure.name,
    });
    return this.node("CallWithArguments", [call, expressions], member.result);
  }

  /** Whether a value is a class value a service defines, and which. */
  namedOf(value: Value) {
    return value instanceof ClientValue ? namedOf(value.value) : undefined;
  }
}

/** Whether a value is a constant null or undefined. */
export function isNullish(value: Value): boolean {
  return value instanceof ClientValue && (value.value === null || value.value === undefined);
}

function inRange(value: number, kind: string): boolean {
  const range = INTEGER_RANGES[kind];
  return range !== undefined && BigInt(value) >= range[0] && BigInt(value) < range[1];
}

/** A value, in a message. */
export function describe(value: unknown): string {
  if (value === null || value === undefined) {
    return String(value);
  }
  switch (typeof value) {
    case "string":
      return JSON.stringify(value);
    case "number":
    case "boolean":
      return String(value);
    case "bigint":
      return `${value}n`;
    case "function":
      return value.name === "" ? "A function" : `The function ${value.name}`;
    default:
      return `A ${value.constructor?.name ?? "object"}`;
  }
}

/** The name of a procedure in messages, as the client's API names it. */
export function displayName(member: Member): string {
  return member.name;
}
