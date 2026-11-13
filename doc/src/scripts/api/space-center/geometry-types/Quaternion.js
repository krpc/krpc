import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const q = await (await vessel.flight()).getRotation();
console.log(q[0], q[1], q[2], q[3]);
