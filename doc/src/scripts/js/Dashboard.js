import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Dashboard" });
const vessel = await conn.spaceCenter.getActiveVessel();
const flightInfo = await vessel.flight();
const altitude = await conn.addStream(() => flightInfo.getMeanAltitude());
await altitude.setRate(10);

// Shows the altitude in the page's <span id="altitude"> each time it changes
const element = document.getElementById("altitude");
altitude.addCallback((value) => {
  element.textContent = `${value.toFixed(0)} m`;
});
