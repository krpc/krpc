import { connect } from "@krpc-org/krpc";

const turnStartAltitude = 250;
const turnEndAltitude = 45000;
const targetAltitude = 150000;

const conn = await connect({ name: "Launch into orbit" });
const spaceCenter = conn.spaceCenter;
const vessel = await spaceCenter.getActiveVessel();
const control = await vessel.getControl();
const autoPilot = await vessel.getAutoPilot();
const orbit = await vessel.getOrbit();
const flight = await vessel.flight();

// Set up streams for telemetry
const ut = await conn.addStream(() => spaceCenter.getUT());
const altitude = await conn.addStream(() => flight.getMeanAltitude());
const apoapsis = await conn.addStream(() => orbit.getApoapsisAltitude());
const stage2Resources = await (await vessel.decoupleStageAt(2)).resources({
  cumulative: false,
});
const srbFuel = await conn.addStream(() =>
  stage2Resources.amount("SolidFuel"),
);

// Pre-launch setup
await control.setSAS(false);
await control.setRCS(false);
await control.setThrottle(1);

// Countdown...
const sleep = (/** @type {number} */ ms) =>
  new Promise((resolve) => setTimeout(resolve, ms));
console.log("3...");
await sleep(1000);
console.log("2...");
await sleep(1000);
console.log("1...");
await sleep(1000);
console.log("Launch!");

// Activate the first stage
await control.activateNextStage();
await autoPilot.setEngaged(true);
await autoPilot.targetPitchAndHeading(90, 90);

// Main ascent loop
let srbsSeparated = false;
let turnAngle = 0;
while (true) {
  // Wait for the next altitude update
  await altitude.wait();
  const alt = await altitude.get();

  // Gravity turn
  if (alt > turnStartAltitude && alt < turnEndAltitude) {
    const frac =
      (alt - turnStartAltitude) / (turnEndAltitude - turnStartAltitude);
    const newTurnAngle = frac * 90;
    if (Math.abs(newTurnAngle - turnAngle) > 0.5) {
      turnAngle = newTurnAngle;
      await autoPilot.targetPitchAndHeading(90 - turnAngle, 90);
    }
  }

  // Separate SRBs when finished
  if (!srbsSeparated) {
    if ((await srbFuel.get()) < 0.1) {
      await control.activateNextStage();
      srbsSeparated = true;
      console.log("SRBs separated");
    }
  }

  // Decrease throttle when approaching target apoapsis
  if ((await apoapsis.get()) > targetAltitude * 0.9) {
    console.log("Approaching target apoapsis");
    break;
  }
}

// Disable engines when target apoapsis is reached
await control.setThrottle(0.25);
const apoapsisEvent = await conn.addEvent(
  ({ orbit, targetAltitude }) => orbit.apoapsisAltitude >= targetAltitude,
  { orbit, targetAltitude },
);
await apoapsisEvent.wait();
console.log("Target apoapsis reached");
await control.setThrottle(0);

// Wait until out of atmosphere
console.log("Coasting out of atmosphere");
const atmosphereEvent = await conn.addEvent(
  ({ flight }) => flight.meanAltitude >= 70500,
  { flight },
);
await atmosphereEvent.wait();

// Plan circularization burn (using vis-viva equation)
console.log("Planning circularization burn");
const mu = await (await orbit.getBody()).getGravitationalParameter();
const r = await orbit.getApoapsis();
const a1 = await orbit.getSemiMajorAxis();
const a2 = r;
const v1 = Math.sqrt(mu * (2 / r - 1 / a1));
const v2 = Math.sqrt(mu * (2 / r - 1 / a2));
const deltaV = v2 - v1;
const node = await control.addNode(
  (await ut.get()) + (await orbit.getTimeToApoapsis()),
  { prograde: deltaV },
);

// Calculate burn time (using rocket equation)
const F = await vessel.getAvailableThrust();
const Isp = (await vessel.getSpecificImpulse()) * 9.82;
const m0 = await vessel.getMass();
const m1 = m0 / Math.exp(deltaV / Isp);
const flowRate = F / Isp;
const burnTime = (m0 - m1) / flowRate;

// Orientate ship
console.log("Orientating ship for circularization burn");
await autoPilot.setReferenceFrame(await node.getReferenceFrame());
await autoPilot.setTargetDirection([0, 1, 0]);
await autoPilot.wait();

// Wait until burn
console.log("Waiting until circularization burn");
const burnUT =
  (await ut.get()) + (await orbit.getTimeToApoapsis()) - burnTime / 2;
const leadTime = 5;
await spaceCenter.warpTo(burnUT - leadTime);

// Execute burn
console.log("Ready to execute burn");
const burnEvent = await conn.addEvent(
  ({ orbit, burnTime }) => orbit.timeToApoapsis <= burnTime / 2,
  { orbit, burnTime },
);
await burnEvent.wait();
console.log("Executing burn");
await control.setThrottle(1);
await sleep((burnTime - 0.1) * 1000);
console.log("Fine tuning");
await control.setThrottle(0.05);
const executedEvent = await conn.addEvent(
  ({ node }) => node.remainingBurnVector(node.referenceFrame)[1] <= 0,
  { node },
);
await executedEvent.wait();
await control.setThrottle(0);
await node.remove();

console.log("Launch complete");
await conn.close();
