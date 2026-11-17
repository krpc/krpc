import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const v = await (await vessel.flight()).getPrograde();
console.log(`(${v.map((x) => x.toFixed(2)).join(", ")})`);
await conn.close();
