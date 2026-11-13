import type { RemoteObject, RemoteService } from "./objects.js";
import type { Expression } from "./services/KRPC.js";
import type { RemoteEnum, RemoteStruct } from "./types.js";
import type { TypeValue } from "./typevalues.js";

/** The server forms of the classes and services, by name, such as `"SpaceCenter.Vessel"`. Each
 * generated module adds its own, so the services of mods join in. */
// biome-ignore lint/suspicious/noEmptyInterface: generated modules add members by declaration merging
export interface ServerTypes {}

/** The client forms of the classes and services, by name, the reverse of ServerTypes. */
// biome-ignore lint/suspicious/noEmptyInterface: generated modules add members by declaration merging
export interface ClientTypes {}

/** The key of the property that names a server form in its type. The property exists in types
 * only. */
declare const serverBrand: unique symbol;

/** The base of the server form of a class or a service, as a compiled function reads it: a
 * property is a property, and a method returns its value. The type parameter names it, such as
 * `"SpaceCenter.Vessel"`. */
export interface ServerObject<C extends string = string> {
  readonly [serverBrand]?: C;
}

/** The form of a captured value in a compiled function: an object of a service's class, or a
 * service, as its server form, and collections and plain objects of them. */
export type OnServer<T> =
  T extends RemoteObject<infer C>
    ? Lookup<ServerTypes, C, T>
    : T extends RemoteService<infer S>
      ? Lookup<ServerTypes, S, T>
      : T extends ServerObject | TypeValue | RemoteEnum | RemoteStruct | Uint8Array | Primitive
        ? T
        : T extends readonly unknown[]
          ? { -readonly [K in keyof T]: OnServer<T[K]> }
          : // A Map tests first, as it also has the members of a ReadonlySet
            T extends ReadonlyMap<infer K, infer V>
            ? Map<OnServer<K>, OnServer<V>>
            : T extends ReadonlySet<infer E>
              ? Set<OnServer<E>>
              : T extends (...args: infer A) => Promise<infer R>
                ? (...args: A) => OnServer<R>
                : T extends (...args: any[]) => unknown
                  ? T
                  : T extends object
                    ? { [K in keyof T]: OnServer<T[K]> }
                    : T;

/** The forms of the parameters of a compiled function in its body. */
export type OnServerAll<P extends unknown[]> = { [K in keyof P]: OnServer<P[K]> };

/** The form a value of a compiled function has in the client: the reverse of OnServer. */
export type OnClient<T> =
  T extends ServerObject<infer C>
    ? Lookup<ClientTypes, C, T>
    : T extends Primitive
      ? T
      : T extends readonly unknown[]
        ? { -readonly [K in keyof T]: OnClient<T[K]> }
        : T extends ReadonlyMap<infer K, infer V>
          ? Map<OnClient<K>, OnClient<V>>
          : T extends ReadonlySet<infer E>
            ? Set<OnClient<E>>
            : T;

/** The forms the arguments of a compiled function take, wider than its values, as for a call: a
 * number or a bigint for a 64-bit integer, and read-only collections. */
export type ArgumentsOf<P extends unknown[]> = { [K in keyof P]: ArgumentOf<P[K]> };

export type ArgumentOf<T> = T extends bigint
  ? number | bigint
  : T extends readonly unknown[]
    ? { readonly [K in keyof T]: ArgumentOf<T[K]> }
    : T extends ReadonlyMap<infer K, infer V>
      ? ReadonlyMap<ArgumentOf<K>, ArgumentOf<V>>
      : T extends ReadonlySet<infer E>
        ? ReadonlySet<ArgumentOf<E>>
        : T;

type Primitive = string | number | bigint | boolean | symbol | null | undefined;

type Lookup<Table, Key, Otherwise> = Key extends keyof Table ? Table[Key] : Otherwise;

/** The key of the property that holds a compiled function's types. The property exists in types
 * only. */
declare const functionTypes: unique symbol;

/** A compiled server side function, a KRPC.Expression, with the types of its parameters and of
 * its value in the client. */
export type ServerFunction<P extends unknown[] = unknown[], R = unknown> = Expression & {
  readonly [functionTypes]?: [P, R];
};
