import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const v = await (await vessel.flight()).getPrograde();
console.log(v[0], v[1], v[2]);
