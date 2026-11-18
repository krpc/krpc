import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();

/** @param {number} pitch */
function controlLaw(pitch) {
  return 0.05 * (45 - pitch);
}

// [docs]
while (true) {
  await conn.krpc.holdTick();
  try {
    const pitch = await (await vessel.flight()).getPitch();
    await (await vessel.getControl()).setPitch(controlLaw(pitch));
  } finally {
    await conn.krpc.releaseTick();
  }
}
// [/docs]
