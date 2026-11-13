import type { Identifier, Pattern } from "acorn";
import type { TypeLike } from "../typevalues.js";
import type { Builder } from "./builder.js";
import { ClientValue, type Node, TypeArgument, type Value } from "./ir.js";
import type { ParsedFunction } from "./parse.js";
import { resolveType } from "./typing.js";

/** The builtin globals a function reads by name. A program that shadows one of them gets the
 * builtin in the function. */
const GLOBALS: ReadonlyMap<string, unknown> = new Map<string, unknown>([
  ["Math", Math],
  ["console", console],
  ["Number", Number],
  ["String", String],
  ["Boolean", Boolean],
  ["BigInt", BigInt],
  ["Array", Array],
  ["Set", Set],
  ["Map", Map],
  ["Error", Error],
  ["TypeError", TypeError],
  ["RangeError", RangeError],
  ["undefined", undefined],
  ["NaN", Number.NaN],
  ["Infinity", Number.POSITIVE_INFINITY],
]);

/** The binding of a first parameter that receives no captures, which the function cannot
 * read. */
export const NO_CAPTURES = new ClientValue(Symbol("no captures"));

/** The names a function reads: its captures, its parameters and the builtin globals. */
export class Scope {
  readonly #bindings = new Map<string, Value>();
  /** The parameters after the captures, in order. */
  readonly parameters: readonly Node[];

  constructor(
    builder: Builder,
    parsed: ParsedFunction,
    captures: object,
    parameterTypes: readonly TypeLike[],
  ) {
    const params = parsed.node.params;
    const [first, ...rest] = params;
    if (first === undefined) {
      if (Object.keys(captures).length > 0) {
        throw builder.error(
          parsed.node,
          "The function takes no captures. Receive them with an object pattern as its first parameter, such as ({ vessel }) => ...",
        );
      }
    } else if (first.type === "Identifier" && Object.keys(captures).length === 0) {
      // A function with no captures can name its first parameter, such as `_`, and not read it
      this.#bindings.set(first.name, NO_CAPTURES);
    } else {
      this.#bindCaptures(builder, first, captures);
    }
    if (rest.length !== parameterTypes.length) {
      throw builder.error(
        parsed.node,
        `The function has ${rest.length} parameters after its captures, and was given ${parameterTypes.length} parameter types`,
      );
    }
    this.parameters = rest.map((param, i) => {
      if (param.type !== "Identifier") {
        throw builder.error(
          param,
          "A parameter of a compiled function is a name, with no default, pattern or rest",
        );
      }
      let type: ReturnType<typeof resolveType>;
      try {
        type = resolveType(builder.env.types, parameterTypes[i]);
      } catch (error) {
        throw builder.error(
          param,
          `The type of parameter ${param.name}: ${(error as Error).message}`,
        );
      }
      const node = builder.node("Parameter", [param.name, new TypeArgument(type)], type);
      this.#bindings.set(param.name, node);
      return node;
    });
  }

  #bindCaptures(builder: Builder, pattern: Pattern, captures: object): void {
    if (pattern.type !== "ObjectPattern") {
      throw builder.error(
        pattern,
        "The first parameter receives the captured values, and is an object pattern, such as ({ vessel }) => ..., or a name it does not read, such as _, for no captures",
      );
    }
    for (const property of pattern.properties) {
      if (property.type === "RestElement") {
        throw builder.error(property, "The captures cannot have a rest element");
      }
      if (property.computed) {
        throw builder.error(property, "A capture is named by its key, which cannot be computed");
      }
      const key =
        property.key.type === "Identifier"
          ? property.key.name
          : property.key.type === "Literal"
            ? String(property.key.value)
            : null;
      if (key === null) {
        throw builder.error(
          property.key,
          "A capture is named by its key, which cannot be computed",
        );
      }
      if (property.value.type !== "Identifier") {
        throw builder.error(
          property.value,
          `Capture ${key} binds a name, with no default or pattern`,
        );
      }
      if (!Object.hasOwn(captures, key)) {
        throw builder.error(property, `The captured values have no ${key}`);
      }
      this.#bindings.set(
        (property.value as Identifier).name,
        new ClientValue(Reflect.get(captures, key)),
      );
    }
  }

  /** The frames of the blocks being compiled, innermost last. */
  #frames: Frame[] = [];

  /** Whether a name is a capture, rather than a name a block declares. */
  isCapture(name: string): boolean {
    return !this.#frames.some((x) => x.has(name)) && this.#bindings.has(name);
  }

  /** The binding of a name, or undefined for a name the function cannot read. */
  lookup(name: string): Binding | undefined {
    for (let i = this.#frames.length - 1; i >= 0; i--) {
      const binding = this.#frames[i].get(name);
      if (binding !== undefined) {
        return binding;
      }
    }
    const binding = this.#bindings.get(name);
    if (binding !== undefined) {
      return binding;
    }
    return GLOBALS.has(name) ? new ClientValue(GLOBALS.get(name)) : undefined;
  }

  /** Starts the frame of a block. */
  push(): Frame {
    const frame: Frame = new Map();
    this.#frames.push(frame);
    return frame;
  }

  pop(): void {
    this.#frames.pop();
  }

  /** Declares a name in the innermost frame. */
  declare(name: string, binding: Binding): void {
    this.#frames[this.#frames.length - 1].set(name, binding);
  }

  /** The frames in scope, for a local function to read later. */
  get frames(): readonly Frame[] {
    return [...this.#frames];
  }

  /** Runs a function with the given frames in scope, such as those of a local function's
   * definition. */
  within<T>(frames: readonly Frame[], fn: () => T): T {
    const outer = this.#frames;
    this.#frames = [...frames];
    try {
      return fn();
    } finally {
      this.#frames = outer;
    }
  }
}

/** A local variable, a parameter of a local function, or a state variable. */
export class Variable {
  readonly name: string;
  /** The key of its type in the hints, from the syntax that declares it. */
  readonly key: string;
  readonly constant: boolean;
  /** The variable's node, or null for a variable whose value has no type yet. */
  readonly node: Node | null;
  /** The function the variable belongs to. */
  readonly owner: object;
  /** For a variable with no type yet, the kind of value it holds. */
  readonly untyped: "array" | "set" | "map" | "null" | "unset" | undefined;
  /** Whether every path assigns it, as a state variable is. */
  readonly alwaysAssigned: boolean;

  constructor(
    name: string,
    key: string,
    constant: boolean,
    node: Node | null,
    owner: object,
    untyped?: "array" | "set" | "map" | "null" | "unset",
    alwaysAssigned = false,
  ) {
    this.name = name;
    this.key = key;
    this.constant = constant;
    this.node = node;
    this.owner = owner;
    this.untyped = untyped;
    this.alwaysAssigned = alwaysAssigned;
  }
}

/** The name of a catch clause, which holds the message of the caught exception. */
export class Caught {
  readonly message: Node;

  constructor(message: Node) {
    this.message = message;
  }
}

/** What a name stands for: a value, a variable, the error of a catch clause, or a local
 * function, which the statement compiler defines. */
export type Binding = Value | Variable | Caught | { readonly localFunction: true };

export type Frame = Map<string, Binding>;
