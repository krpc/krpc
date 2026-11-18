import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
await (await vessel.getAutoPilot()).setShowInfoUI(true);
// [/docs]
await conn.close();
