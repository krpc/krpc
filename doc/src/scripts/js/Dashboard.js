import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Dashboard" });
const vessel = await conn.spaceCenter.getActiveVessel();
const flightInfo = await vessel.flight();

// Shows the altitude in the page's <span id="altitude"> twice a second
const element = document.getElementById("altitude");
setInterval(async () => {
  const altitude = await flightInfo.getMeanAltitude();
  if (element !== null) {
    element.textContent = `${altitude.toFixed(0)} m`;
  }
}, 500);
