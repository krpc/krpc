import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Orbital directions" });
const vessel = await conn.spaceCenter.getActiveVessel();
const ap = await vessel.getAutoPilot();
await ap.setReferenceFrame(await vessel.getOrbitalReferenceFrame());
await ap.setEngaged(true);

// Point the vessel in the prograde direction
await ap.setTargetDirection([0, 1, 0]);
await ap.wait();

// Point the vessel in the orbit normal direction
await ap.setTargetDirection([0, 0, 1]);
await ap.wait();

// Point the vessel in the orbit radial direction
await ap.setTargetDirection([-1, 0, 0]);
await ap.wait();

await ap.setEngaged(false);
await conn.close();
