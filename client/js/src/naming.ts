// The digits a word takes
const DIGITS = "(?:[0-9]+(?![0-9])(?![A-Z]+(?![a-z]))[a-z]*)?";
// The words of a name, by the rules on the client design page
const WORD = new RegExp(
  `(?:UInt|ULong|SInt|FoV)(?![a-z])${DIGITS}` +
    `|[A-Z]{2,}s(?![a-z])${DIGITS}` +
    `|[A-Z]+(?![a-z])${DIGITS}` +
    `|[A-Z]?[a-z]+${DIGITS}` +
    "|[0-9]+(?:[A-Z]+(?![a-z])|[a-z]+)?" +
    "|.",
  "gu",
);

/** The name of a member in lower camel case, from the name a server gives it: the first word in
 * lower case and the others as written, so `HTTPServer` is `httpServer`. */
export function camelCase(name: string): string {
  return name
    .split("_")
    .map((part) => {
      const [first = "", ...rest] = part.match(WORD) ?? [];
      return first.toLowerCase() + rest.join("");
    })
    .join("_");
}

// The members of Object.prototype, which a member of a service, an object, an enumeration or a
// structure value of the same name would replace
const OBJECT_MEMBERS: ReadonlySet<string> = new Set([
  "constructor",
  "hasOwnProperty",
  "isPrototypeOf",
  "propertyIsEnumerable",
  "toLocaleString",
  "toString",
  "valueOf",
]);

// The members of a class, which a static method of the same name would replace
const CLASS_MEMBERS: ReadonlySet<string> = new Set([
  ...OBJECT_MEMBERS,
  "length",
  "name",
  "prototype",
]);

/** The name of a member in lower camel case, with a trailing underscore where it would replace
 * a member of the object that holds it, so `ToString` is `toString_`. */
export function memberName(name: string, isStatic = false): string {
  const key = camelCase(name);
  return (isStatic ? CLASS_MEMBERS : OBJECT_MEMBERS).has(key) ? `${key}_` : key;
}

/** The name of a property accessor, such as `getRCSThrust` for the getter of `RCSThrust`. */
export function accessorName(prefix: "get" | "set", name: string): string {
  return prefix + name;
}
