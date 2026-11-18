// Runs a JavaScript documentation example against the game in the in-game tests.

import { register } from "node:module";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const scripts = fileURLToPath(new URL("../src/scripts/", import.meta.url));
const example = path.join(scripts, process.argv[2]);

register("./example_hooks.mjs", import.meta.url);
await import(pathToFileURL(example).href);

// Most examples leave their connection open, which keeps Node.js running. An example run
// to completion ends here, and one that loops never reaches this point.
if (process.env.KRPC_EXAMPLE_MODE === "exit") {
  process.exit(0);
}
