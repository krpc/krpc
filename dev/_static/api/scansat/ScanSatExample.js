import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "SCANsat Example" });
const vessel = await conn.spaceCenter.getActiveVessel();
const scansat = conn.scanSat;

for (const scanner of await scansat.scanners(vessel)) {
  await scanner.setActive(true);
}

const body = await (await vessel.getOrbit()).getBody();
const coverage = await scansat.coverage(body, scansat.ScanType.altimetry);
console.log(`Altimetry coverage of ${await body.getName()}: ${(coverage * 100).toFixed(1)}%`);

const flight = await vessel.flight();
const elevation = await scansat.elevation(
  body,
  await flight.getLatitude(),
  await flight.getLongitude(),
);
if (elevation === null) {
  console.log("The terrain below the vessel is not scanned");
} else {
  console.log(`The terrain below the vessel is at ${elevation.toFixed(0)} m`);
}
