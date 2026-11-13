import { camelCase } from "./naming.js";
import type { Service } from "./schema/krpc_pb.js";

/** A member of a service that can be deprecated. */
export interface Deprecatable {
  deprecated: boolean;
  deprecatedReason: string;
}

/** Deprecates each member of a deprecated service, class, enumeration or structure. A member
 * with no reason of its own takes the reason of its container. */
export function inheritDeprecation(service: Service): void {
  const inherit = (member: Deprecatable, container: Deprecatable) => {
    if (container.deprecated) {
      member.deprecated = true;
      member.deprecatedReason ||= container.deprecatedReason;
    }
  };
  // A class's reason comes before its service's
  const classes = new Map(service.classes.map((x) => [x.name, x]));
  for (const procedure of service.procedures) {
    const name = procedure.name;
    if (name.includes("_") && !name.startsWith("get_") && !name.startsWith("set_")) {
      const cls = classes.get(name.slice(0, name.indexOf("_")));
      if (cls !== undefined) {
        inherit(procedure, cls);
      }
    }
  }
  for (const member of [
    ...service.classes,
    ...service.enumerations,
    ...service.structs,
    ...service.exceptions,
    ...service.procedures,
  ]) {
    inherit(member, service);
  }
  for (const enumeration of service.enumerations) {
    for (const value of enumeration.values) {
      inherit(value, enumeration);
    }
  }
  for (const struct of service.structs) {
    for (const field of struct.fields) {
      inherit(field, struct);
    }
  }
}

/** The warning for a use of a member, or undefined for a member that is not deprecated. The
 * reason may hold the see-cref markup of documentation. A reference becomes a dotted name, with
 * the prefix of the member's service dropped and a member's name in lower camel case. */
export function deprecationMessage(
  member: Deprecatable,
  service: string,
  qualifiedName: string,
): string | undefined {
  if (!member.deprecated) {
    return undefined;
  }
  const reason = member.deprecatedReason
    .replace(/<see cref=['"](\w):([^'"]*)['"]\s*\/>/g, (_, kind: string, name: string) => {
      if (name.startsWith(`${service}.`)) {
        name = name.slice(service.length + 1);
      }
      if (kind === "M") {
        const dot = name.lastIndexOf(".");
        name = name.slice(0, dot + 1) + camelCase(name.slice(dot + 1));
      }
      return name;
    })
    .replace(/<[^>]*>/g, "");
  return reason === ""
    ? `${qualifiedName} is deprecated`
    : `${qualifiedName} is deprecated: ${reason}`;
}

/** The deprecated members a connection has warned of, so that a loop does not repeat a
 * warning. */
export class Deprecations {
  readonly #warned = new Set<string>();

  /** Warns of the use of a deprecated member, once per member. Node.js gets a
   * DeprecationWarning, and other runtimes a warning on the console. */
  warn(message: string): void {
    if (this.#warned.has(message)) {
      return;
    }
    this.#warned.add(message);
    const process = (
      globalThis as { process?: { emitWarning?: (message: string, type: string) => void } }
    ).process;
    if (typeof process?.emitWarning === "function") {
      process.emitWarning(message, "DeprecationWarning");
    } else {
      console.warn(`DeprecationWarning: ${message}`);
    }
  }
}
