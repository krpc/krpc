import type { ServerErrorDetails } from "./errors.js";
import type { Enumeration, StructDeclaration } from "./types.js";

/** An enumeration, structure or exception a generated module declares. */
export type Declared =
  | {
      readonly kind: "enumeration";
      readonly enumeration: Enumeration;
      /** The object of the enumeration's values the module exports. */
      readonly values: object;
    }
  | { readonly kind: "struct"; readonly declaration: StructDeclaration }
  | {
      readonly kind: "exception";
      readonly cls: new (error: ServerErrorDetails) => Error;
      /** The JavaScript error the class extends, or undefined for a ServerError. */
      readonly native: ErrorConstructor | undefined;
    };

// The declared types, by service and name. A later registration replaces an earlier one.
const registry = new Map<string, Declared>();

export function register(service: string, name: string, declared: Declared): void {
  registry.set(`${service}.${name}`, declared);
}

/** The type a generated module declares for a service's enumeration, structure or exception,
 * or undefined. */
export function registered(service: string, name: string): Declared | undefined {
  return registry.get(`${service}.${name}`);
}
