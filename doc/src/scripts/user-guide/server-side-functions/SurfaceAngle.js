import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const orbit = await vessel.getOrbit();
const frame = await (await orbit.getBody()).getReferenceFrame();
const stream = await conn.addStream(
  ({ stdLib, vessel, frame }) =>
    stdLib.radiansToDegrees(stdLib.vectorAngle(vessel.direction(frame), vessel.velocity(frame))),
  { stdLib: conn.stdLib, vessel, frame },
);
// [/docs]
console.log(await stream.get());
