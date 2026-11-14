import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();

const root = await (await vessel.getParts()).getRoot();
const stack = [{ part: root, depth: 0 }];
while (stack.length > 0) {
  const { part, depth } = stack.pop();
  // A part that is not axially attached is radially attached
  const attachMode = (await part.getAxiallyAttached()) ? "axial" : "radial";
  console.log(" ".repeat(depth), await part.getTitle(), "-", attachMode);
  for (const child of await part.getChildren()) {
    stack.push({ part: child, depth: depth + 1 });
  }
}
