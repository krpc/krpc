import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const parts = await vessel.getParts();
const root = await parts.getRoot();
const temperature = await conn.addStream(() => root.getTemperature());

temperature.addCallback((value) => console.log("Temperature", value));
temperature.addEndCallback((error) => console.log("The stream ended:", error.message));
