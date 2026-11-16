import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const body = await (await vessel.getOrbit()).getBody();
const position = await vessel.position(await body.getReferenceFrame());
console.log(`(${position.map((x) => x.toFixed(1)).join(", ")})`);
await conn.close();
