import type {
  ArrowFunctionExpression,
  AssignmentExpression,
  BlockStatement,
  CatchClause,
  DoWhileStatement,
  Expression,
  ForOfStatement,
  ForStatement,
  FunctionDeclaration,
  FunctionExpression,
  IfStatement,
  MemberExpression,
  Pattern,
  ReturnStatement,
  Statement,
  SwitchStatement,
  Node as SyntaxNode,
  ThrowStatement,
  TryStatement,
  UpdateExpression,
  VariableDeclaration,
  WhileStatement,
} from "acorn";
import { RemoteObject } from "../objects.js";
import { namedOf } from "../registry.js";
import type { Type } from "../types.js";
import { type Builder, describe, isNullish, UNTYPED_HINT } from "./builder.js";
import type { CollectionCompiler } from "./collections.js";
import type { ExpressionCompiler } from "./expressions.js";
import { ClientValue, Collection, Node, type Operand, TypeArgument, type Value } from "./ir.js";
import { Caught, type Frame, type Scope, Variable } from "./scope.js";
import {
  BOOL,
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

/** The syntax of a function: the one being compiled, a local function or a callback. */
export type FunctionSyntax = ArrowFunctionExpression | FunctionExpression | FunctionDeclaration;

/** The variables every path to a point assigns, or null where no path reaches. */
type Flow = Set<Variable> | null;

/** A loop or switch that a break, and for a loop a continue, can jump out of. */
interface Jump {
  readonly kind: "loop" | "switch";
  readonly breaks: Flow[];
  readonly continues: Flow[];
  readonly finallyDepth: number;
}

/** The function whose statements are being compiled. */
class FunctionContext {
  /** The key of the type of its value in the hints. */
  readonly key: string;
  /** The type of the values it returns, null for none, and undefined before a return. */
  result: Type | null | undefined = undefined;
  /** Whether a return gives null before any typed value. */
  nullReturn = false;
  readonly jumps: Jump[] = [];
  finallyDepth = 0;
  /** The variables of enclosing functions it reads. */
  readonly reads = new Set<Variable>();

  constructor(key: string) {
    this.key = key;
  }
}

/** A function a block defines, built at its first call with the types of its arguments. */
export class LocalFunction {
  readonly localFunction = true;
  readonly name: string;
  readonly syntax: FunctionSyntax;
  readonly frames: readonly Frame[];
  built: { lambda: Node; result: Type | null; reads: ReadonlySet<Variable> } | null = null;
  building = false;

  constructor(name: string, syntax: FunctionSyntax, frames: readonly Frame[]) {
    this.name = name;
    this.syntax = syntax;
    this.frames = frames;
  }

  get key(): string {
    return `function:${this.syntax.start}`;
  }
}

/** The JavaScript errors a handler or a throw names, with the exceptions of the server. */
const JS_ERRORS = [Error, TypeError, RangeError] as const;

/** The exceptions a throw of each JavaScript error raises. */
const THROWN: ReadonlyMap<unknown, readonly [string, string]> = new Map([
  [Error, ["KRPC", "Error"]],
  [TypeError, ["KRPC", "ArgumentException"]],
  [RangeError, ["KRPC", "ArgumentOutOfRangeException"]],
] as const);

const COMPOUND: Readonly<Record<string, string>> = {
  "+=": "+",
  "-=": "-",
  "*=": "*",
  "/=": "/",
  "%=": "%",
  "**=": "**",
  "<<=": "<<",
  ">>=": ">>",
  ">>>=": ">>>",
  "&=": "&",
  "|=": "|",
  "^=": "^",
};

/** Compiles the statements of functions, and the functions a function defines. */
export class StatementCompiler {
  readonly #b: Builder;
  readonly #scope: Scope;
  readonly #x: ExpressionCompiler;
  #collections!: CollectionCompiler;
  #flow: Flow = new Set();
  readonly #contexts: FunctionContext[] = [];
  // The variables each block being compiled declares, innermost last
  readonly #declared: Node[][] = [];
  // The captured objects whose properties the function assigns, by capture name
  readonly #stateNames: ReadonlyMap<string, ReadonlySet<string>>;
  readonly #states = new Map<object, Map<string, Variable>>();

  constructor(builder: Builder, scope: Scope, expressions: ExpressionCompiler, body: SyntaxNode) {
    this.#b = builder;
    this.#scope = scope;
    this.#x = expressions;
    this.#stateNames = assignedProperties(body);
  }

  /** Whether the function keeps state variables, whose value changes between evaluations. */
  get hasState(): boolean {
    return this.#states.size > 0;
  }

  set collections(collections: CollectionCompiler) {
    this.#collections = collections;
  }

  // Functions

  /** The body of the function being compiled, with its parameters. A body of statements is a
   * Lambda, so its returns end the function. */
  topLevel(
    syntax: FunctionSyntax,
    parameters: readonly Node[],
  ): { root: Node; result: Type | null } {
    const context = new FunctionContext(`function:${syntax.start}`);
    this.#contexts.push(context);
    this.#scope.push();
    try {
      if (syntax.body.type !== "BlockStatement") {
        const node = this.#value(syntax.body);
        const root = parameters.length > 0 ? this.#lambda(parameters, node, node.type) : node;
        return { root, result: node.type };
      }
      const [body, result] = this.#functionBody(syntax.body, context, []);
      return { root: this.#lambda(parameters, body, result), result };
    } finally {
      this.#scope.pop();
      this.#contexts.pop();
    }
  }

  #lambda(parameters: readonly Node[], body: Node, result: Type | null): Node {
    return this.#b.node("Lambda", [parameters, body], result);
  }

  /** The value of a function body that is an expression, which can be a call with no value. */
  #value(expression: Expression): Node {
    const value = this.#x.operand(expression);
    if (value instanceof ClientValue && value.value instanceof LocalFunction) {
      throw this.#b.error(
        expression,
        `${value.value.name} is a local function, and can only be called`,
      );
    }
    if (value instanceof Node && value.type === null) {
      return value;
    }
    return this.#b.toNode(value, undefined, expression);
  }

  /** The statements of a function body, ending in a return of no value when it returns none.
   * A function that returns a value returns one on every path. */
  #functionBody(
    block: BlockStatement,
    context: FunctionContext,
    prologue: readonly Node[],
  ): [Node, Type | null] {
    const b = this.#b;
    const declared: Node[] = [];
    this.#declared.push(declared);
    const statements = [...prologue];
    try {
      this.#hoist(block.body);
      for (const statement of block.body) {
        if (this.#flow === null) {
          break;
        }
        statements.push(...this.statement(statement));
      }
    } finally {
      this.#declared.pop();
    }
    const hinted = b.hints.get(context.key);
    let result = context.result === undefined ? null : context.result;
    if (context.nullReturn && result === null) {
      if (hinted === undefined) {
        throw b.error(block, `The function returns only null, which has no type. ${UNTYPED_HINT}`);
      }
      result = hinted;
    }
    if (result !== null) {
      if (this.#flow !== null) {
        throw b.error(block, "The function returns a value, and the end of its body returns none");
      }
    } else {
      statements.push(b.node("ReturnNothing", [], null));
    }
    return [this.#block(declared, statements), result];
  }

  /** A Lambda of a function, such as a callback of a collection method, with the given types of
   * its parameters. */
  lambda(syntax: FunctionSyntax, types: readonly Type[], frames?: readonly Frame[]): Node {
    const b = this.#b;
    if (syntax.async || syntax.generator) {
      throw b.error(syntax, "An async function or a generator cannot be compiled");
    }
    if (syntax.params.length > types.length) {
      throw b.error(
        syntax,
        `The function takes ${syntax.params.length} parameters, and is passed ${types.length}`,
      );
    }
    const build = () => {
      const context = new FunctionContext(`function:${syntax.start}`);
      const outerFlow = this.#flow;
      this.#flow = outerFlow === null ? new Set() : new Set(outerFlow);
      this.#contexts.push(context);
      this.#scope.push();
      const declared: Node[] = [];
      this.#declared.push(declared);
      try {
        const prologue: Node[] = [];
        const parameters = syntax.params.map((param, i) => {
          const name = param.type === "Identifier" ? param.name : `parameter${i}`;
          const parameter = b.node("Parameter", [name, new TypeArgument(types[i])], types[i]);
          if (param.type === "Identifier") {
            this.#scope.declare(name, this.#variable(name, param, false, parameter, context));
          } else {
            prologue.push(...this.#bind(param, parameter, true, false));
          }
          return parameter;
        });
        this.#declared.pop();
        if (syntax.body.type === "BlockStatement") {
          const [body, result] = this.#functionBody(syntax.body, context, prologue);
          const node = declared.length > 0 ? this.#block(declared, [body]) : body;
          return this.#lambda(parameters, node, result);
        }
        this.#declared.push(declared);
        const value = this.#value(syntax.body);
        this.#declared.pop();
        const body =
          prologue.length > 0 || declared.length > 0
            ? b.node("BlockWithVariables", [declared, [...prologue, value]], value.type)
            : value;
        return this.#lambda(parameters, body, value.type);
      } finally {
        if (this.#declared[this.#declared.length - 1] === declared) {
          this.#declared.pop();
        }
        this.#scope.pop();
        this.#contexts.pop();
        this.#flow = outerFlow;
        for (const read of context.reads) {
          this.#noteRead(read);
        }
        this.#lastReads = new Set(context.reads);
      }
    };
    return frames === undefined ? build() : this.#scope.within(frames, build);
  }

  /** A call of a local function. Each parameter takes the one type that fits the arguments of
   * every call, which a call notes, compiling the function again. */
  callLocal(
    fn: LocalFunction,
    args: readonly Operand[],
    at: readonly SyntaxNode[],
    node: SyntaxNode,
  ): Node {
    const b = this.#b;
    const params = fn.syntax.params;
    if (args.length !== params.length) {
      throw b.error(
        node,
        `${fn.name} takes ${params.length} arguments, and is passed ${args.length}`,
      );
    }
    const types = args.map((arg, i) => this.#parameterType(fn, i, arg, at[i]));
    const lambda = this.#localLambda(fn, types, node);
    const values = new Map<string, Node>();
    params.forEach((param, i) => {
      values.set(
        param.type === "Identifier" ? param.name : `parameter${i}`,
        b.toNode(args[i], types[i], at[i], `for parameter ${i + 1} of ${fn.name}`),
      );
    });
    return b.node("Invoke", [lambda, values], lambda.type);
  }

  /** A local function passed by name to a collection method, which calls it with values of the
   * given types. */
  localCallback(fn: LocalFunction, types: readonly Type[], node: SyntaxNode): Node {
    const noted = types.map((type, i) => this.#parameterType(fn, i, nodeOf(this.#b, type), node));
    return this.#localLambda(fn, noted, node);
  }

  #parameterType(fn: LocalFunction, i: number, arg: Operand, at: SyntaxNode): Type {
    const b = this.#b;
    const key = `${fn.key}:${i}`;
    const hinted = b.hints.get(key);
    const type = isNullish(arg) ? null : b.valueType(arg, at);
    if (type === null) {
      if (hinted === undefined) {
        throw b.error(at, `The argument of ${fn.name} has no type. ${UNTYPED_HINT}`);
      }
      if (!hinted.nullable && isNullish(arg)) {
        return b.hint(key, withNullable(hinted, true));
      }
      return hinted;
    }
    if (hinted === undefined) {
      return b.hint(key, type);
    }
    if (sameType(hinted, type, true)) {
      if (type.nullable && !hinted.nullable) {
        return b.hint(key, type);
      }
      return hinted;
    }
    throw b.error(
      at,
      `${fn.name} is called with a ${typeName(type)} here, and a ${typeName(hinted)} in another call, which have no common type`,
    );
  }

  #localLambda(fn: LocalFunction, types: readonly Type[], node: SyntaxNode): Node {
    const b = this.#b;
    if (fn.building) {
      throw b.error(node, `${fn.name} calls itself, which a compiled function cannot do`);
    }
    if (fn.built === null) {
      fn.building = true;
      try {
        const lambda = this.lambda(fn.syntax, types, fn.frames);
        fn.built = { lambda, result: lambda.type, reads: this.#lastReads };
      } finally {
        fn.building = false;
      }
    }
    for (const read of fn.built.reads) {
      // A function that calls this one reads the variable too
      this.#noteRead(read);
      if (!this.#isAssigned(read)) {
        throw b.error(
          node,
          `${fn.name} reads the local variable ${read.name}, which is not assigned on every path to this call`,
        );
      }
    }
    return fn.built.lambda;
  }

  /** The error for the first read of a variable with no type, which compiles as null. */
  untypedRead: Error | null = null;

  // The outer variables the last function built reads
  #lastReads: ReadonlySet<Variable> = new Set();

  // Variables

  /** The node of a variable read, checking that every path to the read assigns it. */
  readVariable(variable: Variable, node: SyntaxNode): Value {
    const b = this.#b;
    if (variable.node === null) {
      // A variable holding null reads as null until a later use types it, which compiles the
      // function again. With no such use, the read is an error
      if (variable.untyped !== "null" && variable.untyped !== "unset") {
        throw b.error(node, `${variable.name} has no type yet. ${UNTYPED_HINT}`);
      }
      this.untypedRead ??= b.error(node, `${variable.name} has no type yet. ${UNTYPED_HINT}`);
      return new ClientValue(null);
    }
    if (!this.#isAssigned(variable)) {
      throw b.error(node, `${variable.name} is read before it is assigned on every path`);
    }
    this.#noteRead(variable);
    return variable.node;
  }

  #isAssigned(variable: Variable): boolean {
    return variable.alwaysAssigned || this.#flow === null || this.#flow.has(variable);
  }

  /** Notes a read of a variable of an enclosing function, which a local function checks at each
   * call. */
  #noteRead(variable: Variable): void {
    const context = this.#contexts[this.#contexts.length - 1];
    if (context !== undefined && variable.owner !== context && !variable.alwaysAssigned) {
      context.reads.add(variable);
    }
  }

  #variable(
    name: string,
    syntax: SyntaxNode,
    constant: boolean,
    node: Node | null,
    owner: object,
    untyped?: Variable["untyped"],
  ): Variable {
    const variable = new Variable(name, `variable:${syntax.start}`, constant, node, owner, untyped);
    if (node !== null && node.method === "Parameter") {
      this.#flow?.add(variable);
    }
    return variable;
  }

  /** Declares a variable, typed by its hint or by its first value. */
  #declare(
    name: string,
    syntax: SyntaxNode,
    constant: boolean,
    value: Operand | null,
  ): [Variable, Node[]] {
    const b = this.#b;
    const key = `variable:${syntax.start}`;
    const type = b.hints.get(key) ?? (value === null ? null : b.valueType(value, syntax));
    const owner = this.#contexts[this.#contexts.length - 1];
    if (type === null) {
      const untyped =
        value === null
          ? "unset"
          : value instanceof Collection
            ? value.form
            : isNullish(value)
              ? "null"
              : undefined;
      if (untyped === undefined) {
        throw b.error(syntax, `${name} has no type. ${UNTYPED_HINT}`);
      }
      const variable = new Variable(name, key, constant, null, owner, untyped);
      this.#scope.declare(name, variable);
      return [variable, []];
    }
    const node = b.node("Variable", [name, new TypeArgument(type)], type);
    this.#declared[this.#declared.length - 1].push(node);
    const variable = new Variable(name, key, constant, node, owner);
    this.#scope.declare(name, variable);
    if (value === null) {
      return [variable, []];
    }
    return [variable, [this.#assignVariable(variable, value, syntax)]];
  }

  /** An assignment of a value to a variable. A value that can be null widens a variable that
   * cannot hold null, and the function is compiled again. */
  #assignVariable(variable: Variable, value: Operand, node: SyntaxNode): Node {
    const b = this.#b;
    if (variable.node === null) {
      const type = isNullish(value) ? null : b.valueType(value, node);
      if (type === null) {
        this.#flow?.add(variable);
        return this.#noop();
      }
      return b.hint(variable.key, withNullable(type, type.nullable || variable.untyped === "null"));
    }
    const type = variable.node.type as Type;
    if (!type.nullable && (isNullish(value) || (value instanceof Node && value.type?.nullable))) {
      b.hint(variable.key, withNullable(type, true), () =>
        b.error(node, `${variable.name} cannot be null, as its type is ${typeName(type)}`),
      );
    }
    const converted = b.toNode(value, type, node, `for ${variable.name}`);
    this.#flow?.add(variable);
    return b.node("Assign", [variable.node, converted], null);
  }

  /** Gives a type to a variable with none from a value added to it, and compiles the function
   * again. Returns for a variable that has a type. */
  typeFromUse(
    variable: Variable,
    method: string,
    args: readonly Operand[],
    node: SyntaxNode,
  ): void {
    const b = this.#b;
    if (variable.node !== null) {
      return;
    }
    const type = (i: number) => {
      const t = args[i] === undefined || isNullish(args[i]) ? null : b.valueType(args[i], node);
      if (t === null) {
        throw b.error(node, `${variable.name} has no type yet. ${UNTYPED_HINT}`);
      }
      return t;
    };
    if (variable.untyped === "array" && (method === "push" || method === "index")) {
      b.hint(variable.key, {
        kind: "list",
        item: type(method === "index" ? 1 : 0),
        nullable: false,
      });
    }
    if (variable.untyped === "set" && method === "add") {
      b.hint(variable.key, { kind: "set", item: type(0), nullable: false });
    }
    if (variable.untyped === "map" && method === "set") {
      b.hint(variable.key, { kind: "dictionary", key: type(0), value: type(1), nullable: false });
    }
    throw b.error(node, `${variable.name} has no type yet. ${UNTYPED_HINT}`);
  }

  /** The state variable of a property of a captured plain object that the function assigns, or
   * undefined for a property it only reads. */
  stateVariable(
    capture: string,
    object: object,
    property: string,
    node: SyntaxNode,
  ): Variable | undefined {
    if (!this.#stateNames.get(capture)?.has(property) || !this.#scope.isCapture(capture)) {
      return undefined;
    }
    let properties = this.#states.get(object);
    if (properties === undefined) {
      properties = new Map();
      this.#states.set(object, properties);
    }
    let variable = properties.get(property);
    if (variable === undefined) {
      const b = this.#b;
      const key = `state:${capture}.${property}`;
      const initial = new ClientValue(Reflect.get(object, property));
      const type = b.hints.get(key) ?? b.valueType(initial, node);
      let state: Node | null = null;
      if (type !== null) {
        state = b.node(
          "StateVariable",
          [`${capture}.${property}`, b.toNode(initial, type, node)],
          type,
        );
      }
      const value = initial.value;
      const untyped = Array.isArray(value)
        ? "array"
        : value instanceof Set
          ? "set"
          : value instanceof Map
            ? "map"
            : "null";
      variable = new Variable(property, key, false, state, this, untyped, true);
      properties.set(property, variable);
    }
    return variable;
  }

  // Blocks

  #block(declared: readonly Node[], statements: readonly Node[]): Node {
    const b = this.#b;
    const body = statements.length === 0 ? [this.#noop()] : statements;
    if (declared.length > 0) {
      return b.node("BlockWithVariables", [declared, body], null);
    }
    return body.length === 1 ? body[0] : b.node("Block", [body], null);
  }

  #noop(): Node {
    return this.#b.node("ConstantBool", [true], BOOL);
  }

  /** A block of statements, with a frame of its own. */
  #statements(statements: readonly Statement[]): Node {
    this.#scope.push();
    const declared: Node[] = [];
    this.#declared.push(declared);
    try {
      this.#hoist(statements);
      const nodes: Node[] = [];
      for (const statement of statements) {
        if (this.#flow === null) {
          break;
        }
        nodes.push(...this.statement(statement));
      }
      return this.#block(declared, nodes);
    } finally {
      this.#declared.pop();
      this.#scope.pop();
    }
  }

  /** Declares the local functions of a block, which can be called before their definitions. */
  #hoist(statements: readonly Statement[]): void {
    for (const statement of statements) {
      if (statement.type === "FunctionDeclaration") {
        const name = statement.id.name;
        this.#scope.declare(name, new LocalFunction(name, statement, this.#scope.frames));
      }
    }
  }

  statement(statement: Statement): Node[] {
    const b = this.#b;
    switch (statement.type) {
      case "ExpressionStatement":
        return this.#expressionStatement(statement.expression);
      case "VariableDeclaration":
        return this.#declaration(statement);
      case "FunctionDeclaration":
        if (statement.async || statement.generator) {
          throw b.error(statement, "An async function or a generator cannot be compiled");
        }
        return [];
      case "EmptyStatement":
        return [];
      case "BlockStatement":
        return [this.#statements(statement.body)];
      case "ReturnStatement":
        return [this.#return(statement)];
      case "IfStatement":
        return this.#if(statement);
      case "WhileStatement":
        return [this.#while(statement)];
      case "DoWhileStatement":
        return [this.#doWhile(statement)];
      case "ForStatement":
        return [this.#for(statement)];
      case "ForOfStatement":
        return [this.#forOf(statement)];
      case "BreakStatement":
      case "ContinueStatement":
        if (statement.label) {
          throw b.error(statement, "A label is not supported in a compiled function");
        }
        return [this.#jump(statement.type === "BreakStatement" ? "break" : "continue", statement)];
      case "SwitchStatement":
        return [this.#switch(statement)];
      case "ThrowStatement":
        return [this.#throw(statement)];
      case "TryStatement":
        return [this.#try(statement)];
      case "ForInStatement":
        throw b.error(statement, "for...in is not supported in a compiled function. Use for...of");
      case "LabeledStatement":
        throw b.error(statement, "A label is not supported in a compiled function");
      default:
        throw b.error(statement, `A ${statement.type} is not supported in a compiled function`);
    }
  }

  #expressionStatement(expression: Expression): Node[] {
    if (expression.type === "AssignmentExpression") {
      return this.assignment(expression);
    }
    if (expression.type === "UpdateExpression") {
      return this.update(expression);
    }
    if (expression.type === "SequenceExpression") {
      return expression.expressions.flatMap((x) => this.#expressionStatement(x));
    }
    const value = this.#x.operand(expression);
    return value instanceof Node ? [value] : [];
  }

  #declaration(statement: VariableDeclaration): Node[] {
    const b = this.#b;
    if (statement.kind === "var") {
      throw b.error(statement, "var is not supported in a compiled function. Use let or const");
    }
    if (statement.kind !== "let" && statement.kind !== "const") {
      throw b.error(statement, `${statement.kind} is not supported in a compiled function`);
    }
    const constant = statement.kind === "const";
    const nodes: Node[] = [];
    for (const declarator of statement.declarations) {
      const { id, init } = declarator;
      if (
        id.type === "Identifier" &&
        init &&
        (init.type === "ArrowFunctionExpression" || init.type === "FunctionExpression")
      ) {
        if (!constant) {
          throw b.error(declarator, "A local function is declared with const");
        }
        this.#scope.declare(id.name, new LocalFunction(id.name, init, this.#scope.frames));
        continue;
      }
      if (id.type === "Identifier") {
        const value = init ? this.#x.operand(init) : null;
        const [variable, assigned] = this.#declare(id.name, id, constant, value);
        if (init && variable.node === null) {
          this.#flow?.add(variable);
        }
        nodes.push(...assigned);
        continue;
      }
      if (!init) {
        throw b.error(declarator, "A destructuring declaration needs a value");
      }
      nodes.push(
        ...this.#bind(id, b.toNode(this.#x.operand(init), undefined, init), true, constant),
      );
    }
    return nodes;
  }

  /** Binds the names of a pattern to the parts of a value: the values of a tuple or a list by
   * position, and the properties of an object or the fields of a structure by name. */
  #bind(pattern: Pattern, value: Node, declare: boolean, constant: boolean): Node[] {
    const b = this.#b;
    if (pattern.type === "Identifier") {
      if (declare) {
        return this.#declare(pattern.name, pattern, constant, value)[1];
      }
      return [this.#assignTo(pattern, value)];
    }
    if (pattern.type === "MemberExpression" && !declare) {
      return [this.#assignTo(pattern, value)];
    }
    if (pattern.type !== "ArrayPattern" && pattern.type !== "ObjectPattern") {
      throw b.error(pattern, `A ${pattern.type} is not supported in a compiled function`);
    }
    // The value is read once, from a variable of its own
    const nodes: Node[] = [];
    let source = value;
    if (!value.isPlain) {
      source = b.node("Variable", ["value", new TypeArgument(value.type as Type)], value.type);
      this.#declared[this.#declared.length - 1].push(source);
      nodes.push(b.node("Assign", [source, value], null));
    }
    if (pattern.type === "ArrayPattern") {
      pattern.elements.forEach((element, i) => {
        if (element === null) {
          return;
        }
        if (element.type === "RestElement" || element.type === "AssignmentPattern") {
          throw b.error(
            element,
            "A rest element or default is not supported in a compiled function",
          );
        }
        nodes.push(
          ...this.#bind(element, this.#collections.element(source, i, element), declare, constant),
        );
      });
      return nodes;
    }
    for (const property of pattern.properties) {
      if (
        property.type === "RestElement" ||
        property.computed ||
        property.key.type !== "Identifier"
      ) {
        throw b.error(property, "A destructured property is named by its key");
      }
      if (property.value.type === "AssignmentPattern") {
        throw b.error(property, "A default is not supported in a compiled function");
      }
      const part = this.#x.memberOf(source, property.key.name, property);
      nodes.push(
        ...this.#bind(property.value, b.toNode(part, undefined, property), declare, constant),
      );
    }
    return nodes;
  }

  // Assignments

  /** An assignment statement: to a variable, a state variable, a property, an element of a
   * collection, or destructured into several. */
  assignment(expression: AssignmentExpression): Node[] {
    const b = this.#b;
    const { left, right, operator } = expression;
    if (operator === "=") {
      if (left.type === "ArrayPattern" || left.type === "ObjectPattern") {
        // An array literal is a tuple of the variables' types, and another value keeps its own
        const types = right.type === "ArrayExpression" ? this.#patternTypes(left) : undefined;
        const value = b.toNode(this.#x.operand(right), types, right);
        return this.#bind(left, value, false, false);
      }
      return [this.#assign(left as Expression, this.#x.operand(right), expression)];
    }
    const binary = COMPOUND[operator];
    if (binary === undefined) {
      throw b.error(expression, `${operator} is not supported in a compiled function`);
    }
    return [
      this.#compound(
        left as Expression,
        binary,
        (current) => {
          const value = this.#x.operand(right);
          return this.#x.binaryValues(binary, current, b.resolved(value, right), expression);
        },
        expression,
      ),
    ];
  }

  /** The type of a tuple a destructuring assignment takes, from the variables it assigns. */
  #patternTypes(pattern: Pattern): Type | undefined {
    if (pattern.type !== "ArrayPattern") {
      return undefined;
    }
    const items: Type[] = [];
    for (const element of pattern.elements) {
      if (element?.type !== "Identifier") {
        return undefined;
      }
      const binding = this.#scope.lookup(element.name);
      if (!(binding instanceof Variable) || binding.node?.type == null) {
        return undefined;
      }
      items.push(binding.node.type);
    }
    return { kind: "tuple", items, nullable: false };
  }

  /** `x++`, `--x` and the like, as a statement. */
  update(expression: UpdateExpression): Node[] {
    const one = new ClientValue(1);
    const binary = expression.operator === "++" ? "+" : "-";
    return [
      this.#compound(
        expression.argument,
        binary,
        (current) => {
          const value = isBigint(current instanceof Node ? current.type : null)
            ? new ClientValue(1n)
            : one;
          return this.#x.binaryValues(binary, current, value, expression);
        },
        expression,
      ),
    ];
  }

  /** An assignment of a value computed from the target's current value, with the target's
   * object and index read once. */
  #compound(
    target: Expression,
    operator: string,
    compute: (current: Value) => Value,
    node: SyntaxNode,
  ): Node {
    const b = this.#b;
    if (target.type === "Identifier") {
      return this.#assign(target, compute(this.#x.expression(target)), node);
    }
    if (target.type !== "MemberExpression") {
      throw b.error(target, `${operator}= takes a variable, a property or an element`);
    }
    const state = this.#stateOf(target);
    if (state !== undefined) {
      return this.#assignVariable(state, compute(this.readVariable(state, target)), node);
    }
    const object = b.toNode(this.#x.expression(target.object), undefined, target.object);
    return b.once(object, (o) => {
      if (target.computed) {
        const index = b.toNode(
          this.#x.expression(target.property as Expression),
          undefined,
          target.property,
        );
        return b.once(index, (i) =>
          this.#collections.setElement(
            o,
            i,
            compute(this.#collections.index(o, i, target)),
            target,
          ),
        );
      }
      const key = (target.property as { name: string }).name;
      return this.#x.setProperty(o, key, compute(this.#x.memberOf(o, key, target)), target);
    });
  }

  #assign(target: Expression, value: Operand, node: SyntaxNode): Node {
    const b = this.#b;
    if (target.type === "Identifier") {
      const binding = this.#scope.lookup(target.name);
      if (binding instanceof Variable) {
        if (binding.constant) {
          throw b.error(target, `${target.name} is a constant`);
        }
        return this.#assignVariable(binding, value, node);
      }
      if (binding instanceof Node && binding.method === "Parameter") {
        return b.node("Assign", [binding, b.toNode(value, binding.type as Type, node)], null);
      }
      throw b.error(
        target,
        `${target.name} is not a variable. A capture cannot be assigned, and a property of a captured object can`,
      );
    }
    return this.#assignTo(target, b.toNode(value, this.#targetType(target), node));
  }

  /** The type of the property or element an assignment sets, so a value is converted to it. */
  #targetType(target: Expression): Type | undefined {
    if (target.type !== "MemberExpression") {
      return undefined;
    }
    const state = this.#stateOf(target);
    if (state !== undefined) {
      return state.node?.type ?? undefined;
    }
    return undefined;
  }

  #assignTo(target: Pattern | Expression, value: Node): Node {
    const b = this.#b;
    if (target.type === "Identifier") {
      return this.#assign(target, value, target);
    }
    if (target.type !== "MemberExpression") {
      throw b.error(
        target,
        `An assignment to a ${target.type} is not supported in a compiled function`,
      );
    }
    const state = this.#stateOf(target);
    if (state !== undefined) {
      if (state.node === null) {
        b.hint(state.key, b.valueType(value, target) as Type);
      }
      return this.#assignVariable(state, value, target);
    }
    if (target.object.type === "Identifier") {
      const binding = this.#scope.lookup(target.object.name);
      if (binding instanceof Variable && binding.node === null && target.computed) {
        this.typeFromUse(binding, "index", [new ClientValue(0), value], target);
      }
    }
    const object = b.toNode(
      this.#x.expression(target.object as Expression),
      undefined,
      target.object,
    );
    if (target.computed) {
      const index = this.#x.expression(target.property as Expression);
      return this.#collections.setElement(
        object,
        b.toNode(index, undefined, target.property),
        value,
        target,
      );
    }
    const key = (target.property as { name: string }).name;
    return this.#x.setProperty(object, key, value, target);
  }

  /** The state variable a member expression names, such as `state.max` for a captured `state`
   * whose `max` the function assigns. */
  #stateOf(target: MemberExpression): Variable | undefined {
    if (
      target.object.type !== "Identifier" ||
      target.computed ||
      target.property.type !== "Identifier"
    ) {
      return undefined;
    }
    const binding = this.#scope.lookup(target.object.name);
    if (!(binding instanceof ClientValue) || !isPlainObject(binding.value)) {
      return undefined;
    }
    return this.stateVariable(
      target.object.name,
      binding.value as object,
      target.property.name,
      target,
    );
  }

  // Control flow

  #return(statement: ReturnStatement): Node {
    const b = this.#b;
    const context = this.#contexts[this.#contexts.length - 1];
    if (context.finallyDepth > 0) {
      throw b.error(statement, "A return cannot leave a finally clause");
    }
    if (!statement.argument) {
      this.#flow = null;
      if (context.result !== undefined && context.result !== null) {
        throw b.error(statement, "The function returns a value, and this return gives none");
      }
      context.result = null;
      return b.node("ReturnNothing", [], null);
    }
    if (context.result === null) {
      throw b.error(statement, "The function returns no value elsewhere, and a value here");
    }
    const value = this.#x.operand(statement.argument);
    this.#flow = null;
    const hinted = b.hints.get(context.key);
    if (isNullish(value)) {
      context.nullReturn = true;
      const type = hinted ?? context.result;
      if (type === undefined) {
        return b.node("ReturnNothing", [], null);
      }
      context.result = withNullable(type, true);
      if (hinted === undefined) {
        b.hint(context.key, context.result);
      }
      return b.node("Return", [b.constant(null, type, statement.argument)], null);
    }
    if (hinted !== undefined) {
      context.result = hinted;
      return b.node(
        "Return",
        [b.toNode(value, hinted, statement.argument, "for the returned value")],
        null,
      );
    }
    const node = b.toNode(
      value,
      context.result ?? undefined,
      statement.argument,
      "for the returned value",
    );
    const type = node.type as Type;
    const joined = context.result === undefined ? type : joinTypes(context.result, type);
    if (joined === null) {
      throw b.error(
        statement,
        `The function returns a ${typeName(context.result as Type)} and a ${typeName(type)}, which have no common type`,
      );
    }
    if (context.nullReturn && !joined.nullable) {
      b.hint(context.key, withNullable(joined, true));
    }
    context.result = joined;
    return b.node("Return", [node], null);
  }

  /** The condition of a statement, which can be a constant. */
  #condition(expression: Expression): Value {
    return this.#b.truth(this.#x.expression(expression), expression);
  }

  #if(statement: IfStatement): Node[] {
    const b = this.#b;
    const condition = this.#condition(statement.test);
    if (condition instanceof ClientValue) {
      const taken = condition.value ? statement.consequent : statement.alternate;
      return taken ? [this.#statements([taken])] : [];
    }
    const before = copy(this.#flow);
    const consequent = this.#statements([statement.consequent]);
    const afterConsequent = this.#flow;
    this.#flow = copy(before);
    if (!statement.alternate) {
      this.#flow = merge(afterConsequent, this.#flow);
      return [b.node("IfThen", [condition, consequent], null)];
    }
    const alternate = this.#statements([statement.alternate]);
    this.#flow = merge(afterConsequent, this.#flow);
    return [b.node("IfThenElse", [condition, consequent, alternate], null)];
  }

  /** The body of a loop or switch, and the jumps out of it. */
  #jumping<T>(kind: Jump["kind"], body: () => T): [T, Jump] {
    const context = this.#contexts[this.#contexts.length - 1];
    const jump: Jump = { kind, breaks: [], continues: [], finallyDepth: context.finallyDepth };
    context.jumps.push(jump);
    try {
      return [body(), jump];
    } finally {
      context.jumps.pop();
    }
  }

  #jump(kind: "break" | "continue", node: SyntaxNode): Node {
    const b = this.#b;
    const context = this.#contexts[this.#contexts.length - 1];
    const jumps = context.jumps.filter((x) => kind === "break" || x.kind === "loop");
    const target = jumps[jumps.length - 1];
    if (target === undefined) {
      throw b.error(node, `A ${kind} is outside a loop${kind === "break" ? " or switch" : ""}`);
    }
    if (target.finallyDepth < context.finallyDepth) {
      throw b.error(node, `A ${kind} cannot leave a finally clause`);
    }
    (kind === "break" ? target.breaks : target.continues).push(copy(this.#flow));
    this.#flow = null;
    return b.node(kind === "break" ? "Break" : "Continue", [], null);
  }

  #while(statement: WhileStatement): Node {
    const b = this.#b;
    const condition = this.#condition(statement.test);
    const before = copy(this.#flow);
    const [body, jump] = this.#jumping("loop", () => this.#statements([statement.body]));
    const forever = condition instanceof ClientValue && condition.value === true;
    this.#flow = merge(forever ? null : before, ...jump.breaks);
    if (condition instanceof ClientValue && !condition.value) {
      this.#flow = before;
      return this.#noop();
    }
    return b.node("While", [b.toNode(condition, BOOL, statement.test), body], null);
  }

  #doWhile(statement: DoWhileStatement): Node {
    const b = this.#b;
    const [body, jump] = this.#jumping("loop", () => this.#statements([statement.body]));
    this.#flow = merge(this.#flow, ...jump.continues);
    const condition = this.#condition(statement.test);
    const forever = condition instanceof ClientValue && condition.value === true;
    const afterBody = copy(this.#flow);
    this.#flow = merge(forever ? null : afterBody, ...jump.breaks);
    const stop = b.node(
      "IfThen",
      [b.node("Not", [b.toNode(condition, BOOL, statement.test)], BOOL), b.node("Break", [], null)],
      null,
    );
    if (jump.continues.length === 0) {
      return b.node(
        "While",
        [b.constant(true, BOOL, statement), b.node("Block", [[body, stop]], null)],
        null,
      );
    }
    // A continue goes to the start of the loop's body, so the test runs there, after the first pass
    return this.#firstPass(stop, body, statement);
  }

  /** A loop whose body first runs a step on every pass but the first: the test of a do...while,
   * or the update of a for. A continue then runs the step, as JavaScript does. */
  #firstPass(step: Node, body: Node, node: SyntaxNode, test?: Node): Node {
    const b = this.#b;
    const first = b.node("Variable", ["first", new TypeArgument(BOOL)], BOOL);
    const statements = [
      b.node(
        "IfThenElse",
        [first, b.node("Assign", [first, b.constant(false, BOOL, node)], null), step],
        null,
      ),
      ...(test === undefined ? [] : [test]),
      body,
    ];
    return b.node(
      "BlockWithVariables",
      [
        [first],
        [
          b.node("Assign", [first, b.constant(true, BOOL, node)], null),
          b.node(
            "While",
            [b.constant(true, BOOL, node), b.node("Block", [statements], null)],
            null,
          ),
        ],
      ],
      null,
    );
  }

  #for(statement: ForStatement): Node {
    const b = this.#b;
    this.#scope.push();
    const declared: Node[] = [];
    this.#declared.push(declared);
    try {
      const init: Node[] = [];
      if (statement.init?.type === "VariableDeclaration") {
        init.push(...this.#declaration(statement.init));
      } else if (statement.init) {
        init.push(...this.#expressionStatement(statement.init));
      }
      const condition = statement.test ? this.#condition(statement.test) : new ClientValue(true);
      const before = copy(this.#flow);
      const [body, jump] = this.#jumping("loop", () => this.#statements([statement.body]));
      this.#flow = merge(this.#flow, ...jump.continues);
      const update = statement.update ? this.#expressionStatement(statement.update) : [];
      const forever = condition instanceof ClientValue && condition.value === true;
      this.#flow = merge(forever ? null : before, ...jump.breaks);
      const test = b.toNode(condition, BOOL, statement.test ?? statement);
      let loop: Node;
      if (update.length === 0 || jump.continues.length === 0) {
        const steps = update.length === 0 ? body : b.node("Block", [[body, ...update]], null);
        loop = b.node("While", [test, steps], null);
      } else {
        const stop = b.node(
          "IfThen",
          [b.node("Not", [test], BOOL), b.node("Break", [], null)],
          null,
        );
        loop = this.#firstPass(this.#block([], update), body, statement, stop);
      }
      return this.#block(declared, [...init, loop]);
    } finally {
      this.#declared.pop();
      this.#scope.pop();
    }
  }

  #forOf(statement: ForOfStatement): Node {
    const b = this.#b;
    if (statement.await) {
      throw b.error(statement, "for await is not supported in a compiled function");
    }
    const collection = b.toNode(this.#x.operand(statement.right), undefined, statement.right);
    const [iterated, item] = this.#collections.iteration(collection, statement.right);
    this.#scope.push();
    const declared: Node[] = [];
    this.#declared.push(declared);
    try {
      const left = statement.left;
      const before = copy(this.#flow);
      let variable: Node;
      const prologue: Node[] = [];
      if (left.type === "VariableDeclaration") {
        const pattern = left.declarations[0].id;
        if (pattern.type === "Identifier") {
          variable = b.node("Variable", [pattern.name, new TypeArgument(item)], item);
          const binding = new Variable(
            pattern.name,
            `variable:${pattern.start}`,
            left.kind === "const",
            variable,
            this.#contexts[this.#contexts.length - 1],
          );
          this.#scope.declare(pattern.name, binding);
          this.#flow?.add(binding);
        } else {
          variable = b.node("Variable", ["item", new TypeArgument(item)], item);
          this.#declared.push([]);
          const inner = this.#declared[this.#declared.length - 1];
          prologue.push(...this.#bind(pattern, variable, true, left.kind === "const"));
          this.#declared.pop();
          declared.push(...inner);
        }
      } else {
        throw b.error(left, "The variable of for...of is declared with const or let");
      }
      const [body, jump] = this.#jumping("loop", () => this.#statements([statement.body]));
      this.#flow = merge(before, ...jump.breaks);
      const steps = prologue.length === 0 ? body : b.node("Block", [[...prologue, body]], null);
      return b.node(
        "BlockWithVariables",
        [[variable, ...declared], [b.node("ForEach", [variable, iterated, steps], null)]],
        null,
      );
    } finally {
      this.#declared.pop();
      this.#scope.pop();
    }
  }

  /** A switch, with each clause a case of the server's switch. A clause with no statements
   * falls through to the next. */
  #switch(statement: SwitchStatement): Node {
    const b = this.#b;
    // JavaScript scopes a declaration to the whole switch, and the server to its case
    for (const clause of statement.cases) {
      const declaration = clause.consequent.find(
        (x) => x.type === "VariableDeclaration" || x.type === "FunctionDeclaration",
      );
      if (declaration !== undefined) {
        throw b.error(
          declaration,
          "Wrap a declaration in a case in a block, as in case 1: { ... }",
        );
      }
    }
    let value = b.toNode(
      this.#x.expression(statement.discriminant),
      undefined,
      statement.discriminant,
    );
    // Numbers compare as doubles, as JavaScript compares them
    if (isNumber(value.type) && value.type?.kind !== "double") {
      value = b.cast(value, withNullable(DOUBLE, value.type?.nullable ?? false));
    }
    const type = withNullable(value.type as Type, false);
    const before = copy(this.#flow);
    const cases: Node[][] = [];
    const bodies: Node[] = [];
    const hasDefault = statement.cases.some((x) => !x.test);
    const [, jump] = this.#jumping("switch", () => {
      // Every test runs before any body, and a body is reached by its test or by falling through
      let fallthrough: Flow = null;
      for (const clause of statement.cases) {
        this.#flow = copy(before);
        cases.push(
          clause.test
            ? [
                b.toNode(
                  this.#x.expression(clause.test),
                  type,
                  clause.test,
                  "as a case of the switch",
                ),
              ]
            : [],
        );
        this.#flow = merge(copy(before), fallthrough);
        bodies.push(
          clause.consequent.length === 0 ? this.#noop() : this.#statements(clause.consequent),
        );
        fallthrough = this.#flow;
      }
    });
    this.#flow = merge(this.#flow, ...jump.breaks, hasDefault ? null : before);
    return b.node("Switch", [value, cases, bodies], null);
  }

  #throw(statement: ThrowStatement): Node {
    const b = this.#b;
    const argument = statement.argument;
    if (argument.type !== "NewExpression") {
      throw b.error(
        argument,
        "A compiled function throws a new error, such as throw new Error(message)",
      );
    }
    const callee = this.#x.expression(argument.callee as Expression);
    const [service, name] = this.#exceptionOf(callee, argument.callee);
    const [message] = argument.arguments;
    let text: Node = b.constant("", STRING, argument);
    if (message !== undefined) {
      if (message.type === "SpreadElement") {
        throw b.error(message, "Spread is not supported in a compiled function");
      }
      text = b.toNode(b.string(this.#x.expression(message), message), STRING, message);
    }
    this.#flow = null;
    return b.node("Throw", [service, name, text], null);
  }

  /** The server exception a throw of an error class raises. */
  #exceptionOf(callee: Value, node: SyntaxNode): readonly [string, string] {
    const b = this.#b;
    if (callee instanceof ClientValue) {
      const thrown = THROWN.get(callee.value);
      if (thrown !== undefined) {
        return thrown;
      }
      const named = namedOf(callee.value);
      if (named?.kind === "exception") {
        return [named.service, named.name];
      }
    }
    throw b.error(
      node,
      "A compiled function throws Error, TypeError, RangeError or a service's exception class",
    );
  }

  #try(statement: TryStatement): Node {
    const b = this.#b;
    const context = this.#contexts[this.#contexts.length - 1];
    const before = copy(this.#flow);
    let node = this.#statements(statement.block.body);
    let after = this.#flow;
    if (statement.handler) {
      this.#flow = copy(before);
      node = this.#catch(node, statement.handler);
      after = merge(after, this.#flow);
    }
    if (statement.finalizer) {
      this.#flow = copy(before);
      context.finallyDepth++;
      let finalizer: Node;
      try {
        finalizer = this.#statements(statement.finalizer.body);
      } finally {
        context.finallyDepth--;
      }
      const assigned = this.#flow;
      this.#flow = after === null || assigned === null ? null : new Set([...after, ...assigned]);
      node = b.node("TryFinally", [node, finalizer], null);
    } else {
      this.#flow = after;
    }
    return node;
  }

  /** A catch clause. A handler that starts with a chain of `if (e instanceof X)` runs the branch
   * for the class the client raises the exception as. Each branch catches its exceptions by
   * name, innermost first, and records which branch matched, so the branches run after the
   * catches, and an exception from one branch is not caught by the next. */
  #catch(body: Node, handler: CatchClause): Node {
    const b = this.#b;
    const param = handler.param;
    if (param && param.type !== "Identifier") {
      throw b.error(param, "The error of a catch clause is a name");
    }
    const message = b.node(
      "Variable",
      [param?.name ?? "message", new TypeArgument(STRING)],
      STRING,
    );
    const name = param?.name;
    const statements = handler.body.body;
    const [first, ...rest] = statements;
    const branches =
      name !== undefined && first?.type === "IfStatement" ? this.#branches(first, name) : null;
    return this.#withCaught(name, message, () => {
      if (branches === null) {
        const compiled = this.#statements(statements);
        return b.node(
          "BlockWithVariables",
          [[message], [b.node("TryCatchAll", [body, message, compiled], null)]],
          null,
        );
      }
      const matched = b.node("Variable", ["handler", new TypeArgument(SINT32)], SINT32);
      const before = copy(this.#flow);
      const compiled: Node[] = [];
      let after: Flow = null;
      let wrapped = body;
      branches.cases.forEach(([names, statement], i) => {
        this.#flow = copy(before);
        compiled.push(this.#statements([statement]));
        after = merge(after, this.#flow);
        const record = b.node("Assign", [matched, b.constant(i + 1, SINT32, statement)], null);
        for (const [service, exception] of names) {
          wrapped = b.node("TryCatch", [wrapped, service, exception, message, record], null);
        }
      });
      const other = branches.cases.length + 1;
      if (branches.otherwise !== "rethrow") {
        this.#flow = copy(before);
        compiled.push(
          branches.otherwise === null ? this.#noop() : this.#statements([branches.otherwise]),
        );
        after = merge(after, this.#flow);
        const record = b.node("Assign", [matched, b.constant(other, SINT32, handler)], null);
        wrapped = b.node("TryCatchAll", [wrapped, message, record], null);
      }
      let dispatch: Node = this.#noop();
      for (let i = compiled.length - 1; i >= 0; i--) {
        const test = b.node("Equal", [matched, b.constant(i + 1, SINT32, handler)], BOOL);
        dispatch = b.node("IfThenElse", [test, compiled[i], dispatch], null);
      }
      this.#flow = after;
      const nodes = [
        b.node("Assign", [matched, b.constant(0, SINT32, handler)], null),
        wrapped,
        dispatch,
      ];
      if (rest.length > 0) {
        const tail = this.#statements(rest);
        nodes.push(
          b.node(
            "IfThen",
            [b.node("NotEqual", [matched, b.constant(0, SINT32, handler)], BOOL), tail],
            null,
          ),
        );
      }
      return b.node("BlockWithVariables", [[message, matched], nodes], null);
    });
  }

  /** The branches of an `if (e instanceof X) ... else ...` chain, by the exceptions each
   * catches. The last else is null when there is none, and "rethrow" for `throw e`. */
  #branches(
    statement: IfStatement,
    name: string,
  ): {
    cases: [(readonly [string, string])[], Statement][];
    otherwise: Statement | null | "rethrow";
  } | null {
    const cases: [(readonly [string, string])[], Statement][] = [];
    let current: Statement | null | undefined = statement;
    while (current?.type === "IfStatement") {
      const names = this.#instanceOf(current.test, name);
      if (names === null) {
        return cases.length === 0 ? null : { cases, otherwise: current };
      }
      cases.push([names, current.consequent]);
      current = current.alternate;
    }
    if (current && isRethrow(current, name)) {
      return { cases, otherwise: "rethrow" };
    }
    return { cases, otherwise: current ?? null };
  }

  /** The exceptions a test `e instanceof X` (or several joined with ||) catches, or null for
   * another test. */
  #instanceOf(test: Expression, name: string): (readonly [string, string])[] | null {
    if (test.type === "LogicalExpression" && test.operator === "||") {
      const left = this.#instanceOf(test.left, name);
      const right = this.#instanceOf(test.right, name);
      return left === null || right === null ? null : [...left, ...right];
    }
    if (
      test.type !== "BinaryExpression" ||
      test.operator !== "instanceof" ||
      test.left.type !== "Identifier" ||
      test.left.name !== name
    ) {
      return null;
    }
    const b = this.#b;
    const cls = this.#x.expression(test.right);
    const value = cls instanceof ClientValue ? cls.value : undefined;
    const named = namedOf(value);
    if (named?.kind === "exception") {
      return [[named.service, named.name]];
    }
    if (value === Error) {
      throw b.error(test, "Every error is an Error. Handle it with no test");
    }
    if (JS_ERRORS.includes(value as never)) {
      const exceptions = b.env.members.exceptions.filter((x) => x.native === value);
      return exceptions.map((x) => [x.service, x.name] as const);
    }
    throw b.error(
      test.right,
      `${describe(value)} is not an error class a compiled function can catch`,
    );
  }

  #withCaught<T>(name: string | undefined, message: Node, fn: () => T): T {
    this.#scope.push();
    try {
      if (name !== undefined) {
        this.#scope.declare(name, new Caught(message));
      }
      return fn();
    } finally {
      this.#scope.pop();
    }
  }
}

/** Whether a statement is `throw e`, alone or in a block. */
function isRethrow(statement: Statement, name: string): boolean {
  const inner =
    statement.type === "BlockStatement" && statement.body.length === 1
      ? statement.body[0]
      : statement;
  return (
    inner.type === "ThrowStatement" &&
    inner.argument.type === "Identifier" &&
    inner.argument.name === name
  );
}

function copy(flow: Flow): Flow {
  return flow === null ? null : new Set(flow);
}

/** The variables every one of several paths assigns. */
function merge(...flows: Flow[]): Flow {
  const reached = flows.filter((x): x is Set<Variable> => x !== null);
  if (reached.length === 0) {
    return null;
  }
  const [first, ...others] = reached;
  return new Set([...first].filter((x) => others.every((y) => y.has(x))));
}

/** The type of two returned values: one type, doubles for numbers, or null for none. */
function joinTypes(a: Type, b: Type): Type | null {
  const nullable = a.nullable || b.nullable;
  if (sameType(a, b, true)) {
    return withNullable(a, nullable);
  }
  if (isNumber(a) && isNumber(b)) {
    return withNullable(DOUBLE, nullable);
  }
  if (isBigint(a) && isBigint(b)) {
    return scalar("sint64", nullable);
  }
  return null;
}

/** A node standing for a value of a type, for the types of a callback's arguments. */
function nodeOf(b: Builder, type: Type): Node {
  return b.node("Parameter", ["value", new TypeArgument(type)], type);
}

function isPlainObject(value: unknown): boolean {
  if (typeof value !== "object" || value === null || value instanceof RemoteObject) {
    return false;
  }
  const prototype = Object.getPrototypeOf(value);
  return prototype === Object.prototype || prototype === null;
}

/** The methods that change a collection. */
const MUTATORS: ReadonlySet<string> = new Set(["push", "pop", "add", "set", "delete", "clear"]);

/** The properties of each captured name a function assigns, as `state.max = x`, or changes, as
 * `state.values.push(x)`. */
function assignedProperties(body: SyntaxNode): Map<string, Set<string>> {
  const result = new Map<string, Set<string>>();
  const visit = (node: unknown): void => {
    if (typeof node !== "object" || node === null) {
      return;
    }
    if (Array.isArray(node)) {
      node.forEach(visit);
      return;
    }
    const syntax = node as SyntaxNode & Record<string, unknown>;
    let target: unknown;
    if (syntax.type === "AssignmentExpression") {
      target = syntax.left;
    } else if (syntax.type === "UpdateExpression") {
      target = syntax.argument;
    } else if (syntax.type === "CallExpression") {
      // A method that changes a collection, as in state.values.push(x)
      const callee = syntax.callee as MemberExpression;
      if (
        callee.type === "MemberExpression" &&
        callee.property.type === "Identifier" &&
        MUTATORS.has(callee.property.name)
      ) {
        target = callee.object;
      }
    }
    // An element of a collection, as in state.values[0] = x
    const element = target as MemberExpression | undefined;
    if (element?.type === "MemberExpression" && element.computed) {
      target = element.object;
    }
    const member = target as MemberExpression | undefined;
    if (
      member?.type === "MemberExpression" &&
      !member.computed &&
      member.object.type === "Identifier" &&
      member.property.type === "Identifier"
    ) {
      const properties = result.get(member.object.name) ?? new Set();
      properties.add(member.property.name);
      result.set(member.object.name, properties);
    }
    for (const [key, value] of Object.entries(syntax)) {
      if (key !== "loc" && typeof value === "object") {
        visit(value);
      }
    }
  };
  visit(body);
  return result;
}
