import { connect } from "@krpc-org/krpc";

const conn = await connect();
const Expression = conn.krpc.Expression;
const vessel = await conn.spaceCenter.getActiveVessel();
const comms = await vessel.getComms();
const [link] = await comms.getControlPath();
const siteInfo = conn.getCall(() => link.getStart());
// [docs]
// The Name field of a structure a call returns
const expr = await Expression.getField(await Expression.call(siteInfo), "Name");
// [/docs]
console.log(await conn.run(expr));
