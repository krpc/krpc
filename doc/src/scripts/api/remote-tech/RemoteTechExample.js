import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "RemoteTech Example" });
const vessel = await conn.spaceCenter.getActiveVessel();

// Set a dish target
const part = (await (await vessel.getParts()).withTitle("Reflectron KR-7"))[0];
const antenna = await conn.remoteTech.antenna(part);
await antenna.setTargetBody((await conn.spaceCenter.getBodies()).get("Jool"));

// Get info about the vessels communications
const comms = await conn.remoteTech.comms(vessel);
console.log(`Signal delay = ${(await comms.getSignalDelay()).toFixed(4)} seconds`);
