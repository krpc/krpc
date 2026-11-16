import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Landing Site" });
const vessel = await conn.spaceCenter.getActiveVessel();
const body = await (await vessel.getOrbit()).getBody();

// Define the landing site as the top of the VAB
const landingLatitude = -(0 + 5 / 60 + 48.38 / 60 / 60);
const landingLongitude = -(74 + 37 / 60 + 12.2 / 60 / 60);
const landingAltitude = 111;

// Determine landing site reference frame
// (orientation: x=zenith, y=north, z=east)
const landingReferenceFrame = await body.surfaceReferenceFrame(
  landingLatitude,
  landingLongitude,
  landingAltitude,
);

// Draw axes
await conn.drawing.addAxes(landingReferenceFrame);

while (true) {
  await new Promise((resolve) => setTimeout(resolve, 1000));
}
