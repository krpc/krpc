import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const flightInfo = await vessel.flight();

// Each read gets the current altitude from the game
console.log(await flightInfo.getMeanAltitude());
await new Promise((resolve) => setTimeout(resolve, 1000));
console.log(await flightInfo.getMeanAltitude());

// Two results for the same vessel are the same object
console.log(vessel === (await conn.spaceCenter.getActiveVessel()));
