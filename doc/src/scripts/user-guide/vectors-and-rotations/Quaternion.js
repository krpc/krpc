import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const q = await (await vessel.flight()).getRotation();
console.log(`(${q.map((x) => x.toFixed(2)).join(", ")})`);
await conn.close();
