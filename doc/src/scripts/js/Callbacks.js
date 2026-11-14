import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const control = await vessel.getControl();
const abort = await conn.addStream(() => control.getAbort());

abort.addCallback((value) => console.log("Abort 1 called with a value of", value));
abort.addCallback((value) => console.log("Abort 2 called with a value of", value));
