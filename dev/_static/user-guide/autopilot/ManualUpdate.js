import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const ap = await vessel.getAutoPilot();
await ap.setEngaged(true);

/** @param {number} pitch */
function controlLaw(pitch) {
  return pitch + 0.05 * (45 - pitch);
}

/**
 * @param {number} error
 * @param {number} pitch
 */
function record(error, pitch) {
  console.log(error, pitch);
}

// [docs]
await ap.setUpdateMode(conn.spaceCenter.AutoPilotUpdateMode.manual);
while (true) {
  await conn.krpc.holdTick();
  try {
    const pitch = await (await vessel.flight()).getPitch();
    await ap.targetPitchAndHeading(controlLaw(pitch), 90);
    await ap.update();
    record(
      await ap.getError(),
      await (await vessel.getControl()).getPitch(),
    );
  } finally {
    await conn.krpc.releaseTick();
  }
}
// [/docs]
