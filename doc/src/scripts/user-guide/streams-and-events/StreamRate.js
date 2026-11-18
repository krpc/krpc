import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const flight = await vessel.flight();

// [docs]
const altitude = await conn.addStream(() => flight.getMeanAltitude());
await altitude.setRate(10);
while (true) {
  console.log(await altitude.get());
  await new Promise((resolve) => setTimeout(resolve, 100));
}
// [/docs]
