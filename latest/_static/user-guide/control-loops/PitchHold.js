// [docs]
import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "pitch hold" });
const vessel = await conn.spaceCenter.getActiveVessel();
const control = await vessel.getControl();
const targetPitch = 45;

try {
  for (let i = 0; i < 500; i++) {
    await conn.krpc.nextTick();
    const flight = await vessel.flight();
    const error = targetPitch - (await flight.getPitch());
    await control.setPitch(Math.max(-1, Math.min(1, 0.05 * error)));
  }
} finally {
  await conn.krpc.releaseTick();
  await control.setPitch(0);
  await conn.close();
}
// [/docs]
