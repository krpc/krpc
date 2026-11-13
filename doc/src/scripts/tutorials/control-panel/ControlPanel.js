import { connect, SpaceCenter } from "@krpc-org/krpc";

/** @type {[SpaceCenter.SASMode, string][]} */
const SAS_MODES = [
  [SpaceCenter.SASMode.stabilityAssist, "Stability assist"],
  [SpaceCenter.SASMode.maneuver, "Maneuver"],
  [SpaceCenter.SASMode.prograde, "Prograde"],
  [SpaceCenter.SASMode.retrograde, "Retrograde"],
  [SpaceCenter.SASMode.normal, "Normal"],
  [SpaceCenter.SASMode.antiNormal, "Anti-normal"],
  [SpaceCenter.SASMode.radial, "Radial"],
  [SpaceCenter.SASMode.antiRadial, "Anti-radial"],
  [SpaceCenter.SASMode.target, "Target"],
  [SpaceCenter.SASMode.antiTarget, "Anti-target"],
];

/** @type {import("@krpc-org/krpc").Connection} */
let conn = null;
/** @type {SpaceCenter.Control} */
let control = null;
/** The streams of the vessel shown
 * @type {{ remove(): Promise<void> }[]} */
let streams = [];

/** @param {string} id */
const element = (id) => document.getElementById(id);
const throttleSlider = /** @type {HTMLInputElement} */ (element("throttle-slider"));
/** @type {Map<SpaceCenter.SASMode, HTMLButtonElement>} */
const modeButtons = new Map();

/** @param {string} message */
function log(message) {
  const line = document.createElement("div");
  line.textContent = `${new Date().toLocaleTimeString()} ${message}`;
  element("log").prepend(line);
}

/** @param {number} meters */
function distance(meters) {
  return Math.abs(meters) < 10000 ? `${meters.toFixed(0)} m` : `${(meters / 1000).toFixed(1)} km`;
}

/** Runs an action on the vessel's controls, and logs an error it throws
 * @param {(control: SpaceCenter.Control) => Promise<unknown>} action */
async function act(action) {
  if (!control) return;
  try {
    await action(control);
  } catch (error) {
    log(String(error));
  }
}

/** Calls update with each new value of a stream, at most 10 times a second
 * @template T
 * @param {Promise<import("@krpc-org/krpc").Stream<T>>} added
 * @param {(value: T) => void} update */
async function watch(added, update) {
  const stream = await added;
  streams.push(stream);
  await stream.setRate(10);
  stream.addCallback(update);
}

/** Shows the data of a vessel, and points the controls at it
 * @param {SpaceCenter.Vessel} vessel */
async function showVessel(vessel) {
  // Remove the streams of the previous vessel
  await Promise.allSettled(streams.map((stream) => stream.remove()));
  streams = [];
  control = null;
  element("vessel").textContent = vessel ? await vessel.getName() : "No active vessel";
  if (!vessel) return;

  const flight = await vessel.flight();
  const newControl = await vessel.getControl();

  await Promise.all([
    watch(conn.addStream(() => flight.getMeanAltitude()), (altitude) => {
      element("altitude").textContent = distance(altitude);
    }),
    watch(conn.addStream(() => flight.getTrueAirSpeed()), (speed) => {
      element("airspeed").textContent = `${speed.toFixed(1)} m/s`;
    }),
    // The thrust to weight ratio, from the gravity at the vessel's distance from the body
    watch(
      conn.addStream(
        ({ vessel }) =>
          vessel.thrust /
          ((vessel.mass * vessel.orbit.body.gravitationalParameter) / vessel.orbit.radius ** 2),
        { vessel },
      ),
      (twr) => {
        element("twr").textContent = twr.toFixed(2);
      },
    ),
    watch(conn.addStream(() => newControl.getThrottle()), (throttle) => {
      element("throttle").textContent = `${(throttle * 100).toFixed(0)} %`;
      // Leave the slider alone while the user drags it
      if (!throttleSlider.matches(":active")) {
        throttleSlider.value = String(throttle * 100);
      }
    }),
    watch(conn.addStream(() => newControl.getCurrentStage()), (stage) => {
      element("stage-number").textContent = String(stage);
    }),
    watch(conn.addStream(() => newControl.getSAS()), (sas) => {
      element("sas").classList.toggle("on", sas);
    }),
    watch(conn.addStream(() => newControl.getRCS()), (rcs) => {
      element("rcs").classList.toggle("on", rcs);
    }),
    watch(conn.addStream(() => newControl.getSASMode()), (mode) => {
      for (const [buttonMode, button] of modeButtons) {
        button.classList.toggle("on", buttonMode === mode);
      }
    }),
  ]);
  control = newControl;
}

async function connectToServer() {
  const address = /** @type {HTMLInputElement} */ (element("address")).value;
  const port = Number(/** @type {HTMLInputElement} */ (element("port")).value);
  try {
    conn = await connect({ name: "Control Panel", address, port });
  } catch (error) {
    log(`Connection failed: ${error}`);
    return;
  }
  log(`Connected to ${address}:${port}`);
  element("connect").textContent = "Disconnect";

  // Follow the active vessel, which changes when the player switches vessel
  const activeVessel = await conn.addStream(() => conn.spaceCenter.getActiveVessel());
  await activeVessel.setRate(1);
  /** @type {SpaceCenter.Vessel} */
  let shown;
  activeVessel.addCallback(async (vessel) => {
    if (vessel === shown) return;
    shown = vessel;
    await showVessel(vessel);
  });

  // The stream ends when the connection closes
  activeVessel.addEndCallback((error) => {
    conn = null;
    control = null;
    streams = [];
    element("connect").textContent = "Connect";
    log(`Disconnected: ${error.message}`);
  });
}

element("connect").addEventListener("click", () => {
  if (conn) conn.close();
  else connectToServer();
});

element("stage").addEventListener("click", () => act((control) => control.activateNextStage()));

element("sas").addEventListener("click", () =>
  act(async (control) => control.setSAS(!(await control.getSAS()))),
);

element("rcs").addEventListener("click", () =>
  act(async (control) => control.setRCS(!(await control.getRCS()))),
);

throttleSlider.addEventListener("input", () =>
  act((control) => control.setThrottle(Number(throttleSlider.value) / 100)),
);

for (const [mode, label] of SAS_MODES) {
  const button = document.createElement("button");
  button.textContent = label;
  button.addEventListener("click", () =>
    act(async (control) => {
      // Turn SAS on, so the vessel holds the direction
      await control.setSAS(true);
      await control.setSASMode(mode);
    }),
  );
  element("sas-modes").append(button);
  modeButtons.set(mode, button);
}
