import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const targetPitch = 45;

// [docs]
const step = await conn.compile(
  ({ vessel, targetPitch }) => {
    const error = targetPitch - vessel.flight().pitch;
    vessel.control.pitch = Math.max(-1, Math.min(1, 0.05 * error));
  },
  { vessel, targetPitch },
);

while (true) {
  await conn.run(step);
}
// [/docs]
