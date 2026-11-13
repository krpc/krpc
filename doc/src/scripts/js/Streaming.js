import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const flightInfo = await vessel.flight();
const altitude = await conn.addStream(() => flightInfo.getMeanAltitude());

// Prints the altitude each time it changes
console.log(await altitude.get());
while (true) {
  await altitude.wait();
  console.log(altitude.value);
}
