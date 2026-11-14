import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const step = await conn.compile(({ vessel }) => vessel.control.toggleActionGroup(1), { vessel });
while (true) {
  await conn.run(step);
}
// [/docs]
