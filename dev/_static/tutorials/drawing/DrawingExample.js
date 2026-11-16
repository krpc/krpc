import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Drawing Example" });
const spaceCenter = conn.spaceCenter;
const drawing = conn.drawing;
const { ColorRGB } = conn.stdLib;
const vessel = await spaceCenter.getActiveVessel();
const kerbin = (await spaceCenter.getBodies()).get("Kerbin");
const kerbinFrame = await kerbin.getReferenceFrame();

// Draw a circular orbit 250 km above Kerbin, as a dashed green path with a
// label
const orbit = await spaceCenter.Orbit.createFromOrbitalElements(
  kerbin,
  (await kerbin.getEquatorialRadius()) + 250000,
  0,
  0,
  0,
  0,
  0,
  await spaceCenter.getUT(),
);
const path = await drawing.addOrbit(orbit);
await path.setColor(new ColorRGB({ r: 0, g: 1, b: 0 }));
await path.setDashed(true);
await path.setLabel("Target orbit");

// Mark the north pole in the map view, with the game's flag icon
const site = await kerbin.surfacePosition(90, 0, kerbinFrame);
await drawing.addMapMarker(
  site,
  kerbinFrame,
  drawing.MapMarkerIcon.flag,
  "North pole",
);

// Draw a yellow arrow from the vessel, pointing prograde
const arrow = await drawing.addDirectionFromCom(
  [0, 1, 0],
  await vessel.getOrbitalReferenceFrame(),
);
await arrow.setColor(new ColorRGB({ r: 1, g: 1, b: 0 }));
await arrow.setEndArrow(true);

// Label the arrow with the orbital speed of the vessel
const vesselOrbit = await vessel.getOrbit();
while (true) {
  const speed = await vesselOrbit.getSpeed();
  await arrow.setLabel(`${Math.trunc(speed)} m/s`);
  await new Promise((resolve) => setTimeout(resolve, 1000));
}
