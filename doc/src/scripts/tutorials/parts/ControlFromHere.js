import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const parts = await vessel.getParts();
const [part] = await parts.withTitle("Clamp-O-Tron Docking Port");
await parts.setControlling(part);
await conn.close();
