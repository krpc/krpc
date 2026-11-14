import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const flightInfo = await vessel.flight();
{
  await using altitude = await conn.addStream(() => flightInfo.getMeanAltitude());
  console.log(await altitude.get());
}
// The stream was removed at the end of the block
