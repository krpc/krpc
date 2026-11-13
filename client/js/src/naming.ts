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

/** The name of a property accessor, such as `getRCSThrust` for the getter of `RCSThrust`. */
export function accessorName(prefix: "get" | "set", name: string): string {
  return prefix + name;
}
