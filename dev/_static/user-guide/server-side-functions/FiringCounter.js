import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const state = { updates: 0 };
const stream = await conn.addStream(
  ({ vessel, state }) => {
    if (vessel.thrust > 0) {
      state.updates += 1;
    }
    return state.updates;
  },
  { vessel, state },
);
// [/docs]
console.log(await stream.get());
