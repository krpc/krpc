import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();

const parts = await vessel.getParts();
for (const parachute of await parts.getParachutes()) {
  await parachute.deploy();
}
await conn.close();
