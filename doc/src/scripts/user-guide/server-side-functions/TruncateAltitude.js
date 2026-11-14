import { connect } from "@krpc-org/krpc";

const conn = await connect();
const { Expression, Type } = conn.krpc;
const vessel = await conn.spaceCenter.getActiveVessel();
const flight = await vessel.flight();
const meanAltitude = conn.getCall(() => flight.getMeanAltitude());
// [docs]
// Truncate the altitude to a whole number of meters
const expr = await Expression.cast(await Expression.call(meanAltitude), await Type.int());
// [/docs]
console.log(await conn.run(expr));
