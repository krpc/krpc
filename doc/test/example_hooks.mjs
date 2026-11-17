// Module hooks for the examples: the package name resolves to the client built from this tree,
// and an example is an ES module, which no package.json beside it says.

const client = new URL("../../client/js/dist/index.js", import.meta.url);
const scripts = new URL("../src/scripts/", import.meta.url);

export async function resolve(specifier, context, next) {
  if (specifier === "@krpc-org/krpc") {
    return { url: client.href, shortCircuit: true };
  }
  return next(specifier, context);
}

export async function load(url, context, next) {
  if (url.startsWith(scripts.href) && url.endsWith(".js")) {
    return next(url, { ...context, format: "module" });
  }
  return next(url, context);
}
