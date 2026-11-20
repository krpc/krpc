import { connect } from "@krpc-org/krpc";

const conn = await connect();
// [docs]
const vessel = await conn.spaceCenter.getActiveVessel();
const parts = await vessel.getParts();
const chute = (await parts.withName("parachuteSingle"))[0];
const parachute = await chute.getParachute();
await conn.spaceCenter.quickload();
// reads the parachute of the loaded game
console.log((await parachute.getState()).value);
// [/docs]
await conn.close();
