import { RemoteObject } from "./objects.js";
import { RemoteEnum, RemoteStruct, StructDeclaration } from "./types.js";

/** Whether two values are equal: numbers and strings by `===`, enumeration values of one name by
 * number, objects by class and id, bytes and arrays item by item, structures with the same fields
 * field by field, sets and maps by their members. */
export function equals(a: unknown, b: unknown): boolean {
  if (a === b) {
    return true;
  }
  if (a instanceof RemoteObject) {
    return b instanceof RemoteObject && RemoteObject.sameObject(a, b);
  }
  if (a instanceof RemoteEnum) {
    return (
      b instanceof RemoteEnum &&
      a.enumeration.service === b.enumeration.service &&
      a.enumeration.name === b.enumeration.name &&
      a.value === b.value
    );
  }
  if (typeof a === "number" && typeof b === "number") {
    return Number.isNaN(a) && Number.isNaN(b);
  }
  if (typeof a !== "object" || typeof b !== "object" || a === null || b === null) {
    return false;
  }
  if (a instanceof Uint8Array || Array.isArray(a)) {
    const other = b as ArrayLike<unknown>;
    if (a.constructor !== b.constructor || a.length !== other.length) {
      return false;
    }
    for (let i = 0; i < a.length; i++) {
      if (!equals(a[i], other[i])) {
        return false;
      }
    }
    return true;
  }
  if (a instanceof Set) {
    return (
      b instanceof Set && a.size === b.size && [...a].every((x) => [...b].some((y) => equals(x, y)))
    );
  }
  if (a instanceof Map) {
    if (!(b instanceof Map) || a.size !== b.size) {
      return false;
    }
    return [...a].every(([k, v]) => [...b].some(([l, w]) => equals(k, l) && equals(v, w)));
  }
  if (!(a instanceof RemoteStruct) || !(b instanceof RemoteStruct)) {
    return false;
  }
  const [x, y] = [RemoteStruct.structOf(a), RemoteStruct.structOf(b)];
  if (!StructDeclaration.same(x, y)) {
    return false;
  }
  return x.fields.every((field) =>
    equals(RemoteStruct.fieldOf(a, field.name), RemoteStruct.fieldOf(b, field.name)),
  );
}
