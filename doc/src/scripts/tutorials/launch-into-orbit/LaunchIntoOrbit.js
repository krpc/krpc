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
const stage5Resources = await (await vessel.decoupleStageAt(5)).resources({
  cumulative: false,
});
const stage3Resources = await (await vessel.decoupleStageAt(3)).resources({
  cumulative: false,
});
const firstStageFuel = await conn.addStream(() =>
  stage5Resources.amount("SolidFuel"),
);
const secondStageFuel = await conn.addStream(() =>
  stage3Resources.amount("LiquidFuel"),
);

// Pre-launch setup
await control.setSAS(false);
await control.setRCS(false);
await control.setThrottle(1);

// Activate the first stage
await autoPilot.setEngaged(true);
await autoPilot.targetPitchAndHeading(90, 90);
await control.activateNextStage();

// Main ascent loop
let firstStageSeparated = false;
let secondStageSeparated = false;
while (true) {
  // Wait for the next altitude update
  await altitude.wait();
  const alt = await altitude.get();

  // Gravity turn
  if (alt > turnStartAltitude && alt < turnEndAltitude) {
    const frac =
      (alt - turnStartAltitude) / (turnEndAltitude - turnStartAltitude);
    const turnAngle = frac * 90;
    await autoPilot.targetPitchAndHeading(90 - turnAngle, 90);
  }

  if (!firstStageSeparated) {
    if ((await firstStageFuel.get()) < 0.1) {
      await control.activateNextStage();
      await control.activateNextStage();
      firstStageSeparated = true;
      console.log("First stage separated");
    }
  } else if (!secondStageSeparated) {
    if ((await secondStageFuel.get()) < 0.1) {
      await control.activateNextStage();
      await control.activateNextStage();
      secondStageSeparated = true;
      console.log("Second stage separated");
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
const body = await orbit.getBody();
const mu = await body.getGravitationalParameter();
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

// Orientate ship along the burn
console.log("Orientating ship for circularization burn");
const frame = await body.getNonRotatingReferenceFrame();
await autoPilot.setReferenceFrame(frame);
await autoPilot.setTargetDirection(await node.remainingBurnVector(frame));
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
const remainingBurn = await conn.addStream(() =>
  node.remainingBurnVector(frame),
);
const remainingDeltaV = await conn.addStream(() =>
  node.getRemainingDeltaV(),
);
const sleep = (/** @type {number} */ ms) =>
  new Promise((resolve) => setTimeout(resolve, ms));
let lowestDeltaV = await remainingDeltaV.get();
while ((await remainingDeltaV.get()) > 0.1) {
  // Steer along the remaining burn, which turns as the vessel moves
  // around its orbit
  await autoPilot.setTargetDirection(await remainingBurn.get());
  // Full throttle, then throttle down for the last few m/s
  await control.setThrottle((await remainingDeltaV.get()) > 5 ? 1 : 0.05);
  // Stop if the burn overshoots, as the remaining delta-v then grows
  if ((await remainingDeltaV.get()) > lowestDeltaV + 0.1) {
    break;
  }
  lowestDeltaV = Math.min(lowestDeltaV, await remainingDeltaV.get());
  await sleep(50);
}
await control.setThrottle(0);
await node.remove();

console.log("Launch complete");
await conn.close();
