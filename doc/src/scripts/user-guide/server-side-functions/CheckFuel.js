import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const checkFuel = await conn.compile(
  ({ vessel }) => {
    if (vessel.resources.amount("LiquidFuel") < 100) {
      throw new Error("not enough fuel");
    }
  },
  { vessel },
);
// [/docs]
await conn.run(checkFuel);
