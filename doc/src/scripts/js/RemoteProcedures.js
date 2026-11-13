import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();

await vessel.setName("My Vessel");

const flightInfo = await vessel.flight();
console.log(await flightInfo.getMeanAltitude());
