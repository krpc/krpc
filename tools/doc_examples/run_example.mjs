// Runs a JavaScript documentation example against the game in the in-game tests. The arguments
// are the examples' folder, relative to the repository root, then the example's path in it.

import { register } from "node:module";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const root = fileURLToPath(new URL("../../", import.meta.url));
const scripts = path.join(root, process.argv[2]);
const example = path.join(scripts, process.argv[3]);

register("./example_hooks.mjs", import.meta.url, {
  data: { scripts: pathToFileURL(scripts + path.sep).href },
});
await import(pathToFileURL(example).href);

// Most examples leave their connection open, which keeps Node.js running. An example run
// to completion ends here, and one that loops never reaches this point.
if (process.env.KRPC_EXAMPLE_MODE === "exit") {
  process.exit(0);
}
