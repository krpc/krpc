/** The objects of one connection, by id. A connection holds one object per id, so two results
 * for one server object are the same object, and compare with `===`. */
export class ObjectCache {
  readonly #objects = new Map<bigint, WeakRef<RemoteObject>>();
  readonly #registry = new FinalizationRegistry<bigint>((id) => {
    if (this.#objects.get(id)?.deref() === undefined) {
      this.#objects.delete(id);
    }
  });

  get(id: bigint): RemoteObject | undefined {
    return this.#objects.get(id)?.deref();
  }

  add(id: bigint, obj: RemoteObject): void {
    this.#objects.set(id, new WeakRef(obj));
    this.#registry.register(obj, id);
  }
}

/** The key of the property that tells the objects of two classes apart in their types. The
 * property exists in types only. */
declare const classBrand: unique symbol;

/** The base class of the classes of a server's services. An instance stands for an object on
 * the server, by its id. The type parameter names the class, such as `"SpaceCenter.Vessel"`. */
export class RemoteObject<C extends string = string> {
  declare readonly [classBrand]?: C;
  // Assigned only for a new object, as the constructor returns the cached one for a known id
  readonly #id!: bigint;
  readonly #service!: string;
  readonly #className!: string;

  /** The object of the class with the given id, from the connection's cache when it holds
   * one. */
  constructor(cache: ObjectCache, service: string, className: string, id: bigint) {
    const existing = cache.get(id);
    if (
      existing !== undefined &&
      existing.#service === service &&
      existing.#className === className
    ) {
      // biome-ignore lint/correctness/noConstructorReturn: objects are interned by id
      return existing as RemoteObject<C>;
    }
    this.#id = id;
    this.#service = service;
    this.#className = className;
    cache.add(id, this);
  }

  /** The server's id for an object. */
  static idOf(obj: RemoteObject): bigint {
    return obj.#id;
  }

  /** The service and name of an object's class. */
  static classOf(obj: RemoteObject): { readonly service: string; readonly name: string } {
    return { service: obj.#service, name: obj.#className };
  }

  /** Whether two objects stand for one server object: objects of one class with one id, from
   * any connection. */
  static sameObject(a: RemoteObject, b: RemoteObject): boolean {
    return a.#id === b.#id && a.#service === b.#service && a.#className === b.#className;
  }

  /** Whether an object is an instance of the class of the given service and name, from any
   * connection. */
  static isInstance(obj: unknown, service: string, className: string): obj is RemoteObject {
    return obj instanceof RemoteObject && obj.#service === service && obj.#className === className;
  }

  toString(): string {
    return `${this.#service}.${this.#className}<${this.#id}>`;
  }
}

/** The key of the property that tells the services apart in their types. The property exists in
 * types only. */
declare const serviceBrand: unique symbol;

/** The base class of a server's services. A connection has one instance of it per service. The
 * type parameter names the service, such as `"SpaceCenter"`. */
export class RemoteService<S extends string = string> {
  declare readonly [serviceBrand]?: S;
}

/** The server's id for an object. Throws a TypeError for a value that is not an object of a
 * service's class, such as null. */
export function getObjectId(obj: RemoteObject): bigint {
  if (!(obj instanceof RemoteObject)) {
    throw new TypeError(`${String(obj)} is not an object of a service's class`);
  }
  return RemoteObject.idOf(obj);
}
