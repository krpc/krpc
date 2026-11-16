import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Surface prograde" });
const vessel = await conn.spaceCenter.getActiveVessel();
const ap = await vessel.getAutoPilot();

await ap.setReferenceFrame(await vessel.getSurfaceVelocityReferenceFrame());
await ap.setTargetDirection([0, 1, 0]);
await ap.setEngaged(true);
await ap.wait();
await ap.setEngaged(false);
await conn.close();
