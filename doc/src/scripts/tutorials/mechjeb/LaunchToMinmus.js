import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Launch to Minmus" });
const sc = conn.spaceCenter;
const vessel = await sc.getActiveVessel();
const control = await vessel.getControl();
const mj = conn.mechJeb;

/**
 * Wait until the condition is true.
 * @template {object} C
 * @param {(
 *   captures: import("@krpc-org/krpc").OnServer<C>,
 * ) => boolean} condition
 * @param {C} captures
 */
async function waitFor(condition, captures) {
  const event = await conn.addEvent(condition, captures);
  await event.wait();
}

/**
 * Print each step of the autopilot. Returns the status stream.
 * @param {{ getStatus(): Promise<string> }} autopilot
 */
async function printSteps(autopilot) {
  let last = "";
  const status = await conn.addStream(() => autopilot.getStatus());
  status.addCallback((value) => {
    // The step, without the figures that change on each update
    const line = value.split("\n")[0];
    const step = line.split(/[0-9∞]/)[0].replace(/[ :=]+$/, "");
    if (step && step !== last) {
      last = step;
      console.log(step);
    }
  });
  return status;
}

async function printOrbit() {
  const orbit = await vessel.getOrbit();
  const body = await orbit.getBody();
  const apoapsis = (await orbit.getApoapsisAltitude()) / 1000;
  const periapsis = (await orbit.getPeriapsisAltitude()) / 1000;
  console.log(
    `Orbit of ${await body.getName()}, ` +
      `${apoapsis.toFixed(0)} km by ${periapsis.toFixed(0)} km`,
  );
}

// The target orbit, set as in the Ascent Guidance window
const ascent = await mj.getAscentGuidance();
await ascent.setAscentType(mj.AscentType.classic);
await ascent.setDesiredOrbitAltitude(100000);
await ascent.setDesiredInclination(0);

// The Ascent Settings window
const settings = await ascent.getSettings();
await settings.setAutostage(true);
await settings.setLimitAngleOfAttack(true);
await settings.setMaxAngleOfAttack(5);
await settings.setAutowarp(true);

// Print each step of the autopilot
let status = await printSteps(ascent);

await ascent.setEnabled(true);
await control.activateNextStage();

// Wait for the autopilot to disengage, once the orbit is circular
await waitFor(({ ascent }) => !ascent.enabled, { ascent });
await status.remove();
await printOrbit();

const planner = await mj.getManeuverPlanner();
await planner.setAutowarp(true);

// Stage once a stage burns out, during the burns too
const staging = await (await mj.getUtilities()).getStaging();
await staging.setEnabled(true);

/** Execute the vessel's maneuver nodes, and wait for the burns. */
async function executeNodes() {
  await planner.executeAllNodes();
  await waitFor(({ planner }) => !planner.executingNode, { planner });
}

// Target Minmus, and wait for MechJeb to see the target
const minmus = (await sc.getBodies()).get("Minmus");
await sc.setTargetBody(minmus);
const targetController = await mj.getTargetController();
await waitFor(
  ({ targetController }) => targetController.normalTargetExists,
  { targetController },
);

// Match planes with Minmus, at the nearest relative node
const plane = await planner.getOperationPlane();
await (await plane.getTimeSelector()).setTimeReference(
  mj.TimeReference.relNearestAd,
);
await plane.makeNodes();
await executeNodes();

// The Hohmann transfer, to an encounter with no capture burn
const transfer = await planner.getOperationTransfer();
await (await transfer.getTimeSelector()).setTimeReference(
  mj.TimeReference.computed,
);
await transfer.setCapture(false);
await transfer.makeNodes();
await executeNodes();

// Fine tune the encounter, for a periapsis 20 km above Minmus
const correction = await planner.getOperationCourseCorrection();
await correction.setFinalPeriapsisAltitude(20000);
await correction.makeNodes();
await executeNodes();

// Warp into Minmus' sphere of influence
const orbit = await vessel.getOrbit();
const soiChange = (await sc.getUT()) + (await orbit.getTimeToSOIChange());
await sc.warpTo(soiChange + 60);

// Circularize at the periapsis
const circularize = await planner.getOperationCircularize();
await (await circularize.getTimeSelector()).setTimeReference(
  mj.TimeReference.periapsis,
);
await circularize.makeNodes();
await executeNodes();

await sc.clearTarget();
await printOrbit();

// Stage until the next stage would drop the landing legs. This drops
// the stages below the lander, and activates the lander's engine.
await staging.setEnabled(false);
const parts = await vessel.getParts();
const legs = await (await parts.getLegs())[0].getPart();
const legsStage = await legs.getDecoupleStage();
while ((await control.getCurrentStage()) > legsStage + 1) {
  await control.activateNextStage();
}

// Limit the lander's thrust, so its lowest throttle leaves it falling
for (const engine of await parts.getEngines()) {
  await engine.setThrustLimit(0.3);
}

// Land on Minmus' Great Flats, and wait for MechJeb to see the target
const landing = await mj.getLandingGuidance();
await landing.setTarget(minmus, 0, -6);
await waitFor(
  ({ targetController }) => targetController.positionTargetExists,
  { targetController },
);

// The settings of the Landing Guidance window
await landing.setTouchdownSpeed(1);
await landing.setDeployGears(true);
await landing.setAutowarp(true);

// Print each step of the autopilot
status = await printSteps(landing);

await landing.landAtTarget();

// Wait for the autopilot to disengage, once the vessel has landed
await waitFor(({ landing }) => !landing.enabled, { landing });
await status.remove();

await sc.clearTarget();
const flight = await vessel.flight();
const latitude = await flight.getLatitude();
const longitude = await flight.getLongitude();
console.log(
  `Landed at latitude ${latitude.toFixed(2)}, ` +
    `longitude ${longitude.toFixed(2)}`,
);
await conn.close();
