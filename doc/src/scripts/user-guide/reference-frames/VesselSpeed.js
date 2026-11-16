import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Vessel speed" });
const vessel = await conn.spaceCenter.getActiveVessel();
const body = await (await vessel.getOrbit()).getBody();
const obtFrame = await body.getNonRotatingReferenceFrame();
const srfFrame = await body.getReferenceFrame();
const obtFlight = await vessel.flight(obtFrame);
const srfFlight = await vessel.flight(srfFrame);

while (true) {
  const obtSpeed = await obtFlight.getSpeed();
  const srfSpeed = await srfFlight.getSpeed();
  console.log(
    `Orbital speed = ${obtSpeed.toFixed(1)} m/s, ` +
      `Surface speed = ${srfSpeed.toFixed(1)} m/s`,
  );
  await new Promise((resolve) => setTimeout(resolve, 1000));
}
