import { deprecationMessage } from "../deprecation.js";
import { camelCase } from "../naming.js";
import type { Procedure, Service } from "../schema/krpc_pb.js";
import { type Type, type Types, UnknownTypeError } from "../types.js";

/** A parameter of a procedure, named in lower camel case. */
export interface Parameter {
  readonly name: string;
  readonly type: Type;
  readonly optional: boolean;
}

/** A procedure a function can call, with the types of its parameters and result. */
export interface Member {
  readonly service: string;
  readonly procedure: Procedure;
  readonly parameters: readonly Parameter[];
  readonly result: Type | null;
  /** The member's name in messages, such as `SpaceCenter.Vessel.flight`. */
  readonly name: string;
  /** The warning for a use of a deprecated member. */
  readonly deprecation: string | undefined;
}

/** The members of a class or a service, by name in lower camel case. */
export interface Members {
  readonly methods: Map<string, Member>;
  readonly getters: Map<string, Member>;
  readonly setters: Map<string, Member>;
  /** The static methods of a class. */
  readonly statics: Map<string, Member>;
}

/** The members of the classes and services of a connection. */
export class MemberIndex {
  readonly #classes = new Map<string, Members>();
  readonly #services = new Map<string, Members>();

  constructor(services: readonly Service[], types: Types) {
    for (const service of services) {
      this.#services.set(service.name, emptyMembers());
    }
    for (const service of services) {
      for (const procedure of service.procedures) {
        if (!procedure.internal) {
          this.#add(service.name, procedure, types);
        }
      }
    }
  }

  /** The members of a class, by service and name. */
  ofClass(service: string, name: string): Members | undefined {
    return this.#classes.get(`${service}.${name}`);
  }

  /** The members of a service of its own, by name. */
  ofService(service: string): Members | undefined {
    return this.#services.get(service);
  }

  #add(service: string, procedure: Procedure, types: Types): void {
    const name = procedure.name;
    let member: Member;
    try {
      member = {
        service,
        procedure,
        parameters: procedure.parameters.map((x) => {
          if (x.type === undefined) {
            throw new UnknownTypeError(`Parameter ${x.name} has no type`);
          }
          return { name: camelCase(x.name), type: types.get(x.type), optional: x.hasDefaultValue };
        }),
        result: procedure.returnType === undefined ? null : types.get(procedure.returnType),
        name: "",
        deprecation: undefined,
      };
    } catch (error) {
      // A member with a type the client cannot use cannot be called
      if (error instanceof UnknownTypeError) {
        return;
      }
      throw error;
    }
    const isClassMember =
      name.includes("_") && !name.startsWith("get_") && !name.startsWith("set_");
    if (!isClassMember) {
      const members = this.#services.get(service) as Members;
      const [map, key] = accessor(members, name);
      map.set(key, withDeprecation(member, `${service}.${key}`));
      return;
    }
    const className = name.slice(0, name.indexOf("_"));
    const memberName = name.slice(name.lastIndexOf("_") + 1);
    if (name.includes("_static_")) {
      const key = camelCase(memberName);
      const members = this.#members(service, className);
      members.statics.set(key, withDeprecation(member, `${service}.${className}.${key}`));
      return;
    }
    // A member of a class can belong to another service, and takes the object first
    const instance = member.parameters[0]?.type;
    if (instance?.kind !== "class") {
      return;
    }
    const members = this.#members(instance.service, instance.name);
    const [map, key] = accessor(members, name.slice(name.indexOf("_") + 1));
    map.set(key, withDeprecation(member, `${instance.service}.${instance.name}.${key}`));
  }

  #members(service: string, className: string): Members {
    const key = `${service}.${className}`;
    let members = this.#classes.get(key);
    if (members === undefined) {
      members = emptyMembers();
      this.#classes.set(key, members);
    }
    return members;
  }
}

/** The map a member goes in, and its key, from its procedure name with any class prefix
 * removed: `get_Name` is the getter `name`. */
function accessor(members: Members, name: string): [Map<string, Member>, string] {
  if (name.startsWith("get_")) {
    return [members.getters, camelCase(name.slice(4))];
  }
  if (name.startsWith("set_")) {
    return [members.setters, camelCase(name.slice(4))];
  }
  return [members.methods, camelCase(name)];
}

function withDeprecation(member: Member, qualified: string): Member {
  const deprecation = deprecationMessage(member.procedure, member.service, qualified);
  return { ...member, name: qualified, deprecation };
}

function emptyMembers(): Members {
  return { methods: new Map(), getters: new Map(), setters: new Map(), statics: new Map() };
}
