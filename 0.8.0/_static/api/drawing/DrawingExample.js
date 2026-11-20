import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Drawing Example" });
const vessel = await conn.spaceCenter.getActiveVessel();
await conn.drawing.addDirectionFromCom(
  [0, 1, 0],
  await vessel.getReferenceFrame(),
);
await new Promise((resolve) => setTimeout(resolve, 10000));
