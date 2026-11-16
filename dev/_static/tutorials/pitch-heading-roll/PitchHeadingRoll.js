import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Pitch/Heading/Roll" });
const vessel = await conn.spaceCenter.getActiveVessel();
const vesselFrame = await vessel.getReferenceFrame();
const surfaceFrame = await vessel.getSurfaceReferenceFrame();

/** @typedef {readonly [number, number, number]} Vector */

/** @param {Vector} u @param {Vector} v @returns {Vector} */
function crossProduct(u, v) {
  return [
    u[1] * v[2] - u[2] * v[1],
    u[2] * v[0] - u[0] * v[2],
    u[0] * v[1] - u[1] * v[0],
  ];
}

/** @param {Vector} u @param {Vector} v */
function dotProduct(u, v) {
  return u[0] * v[0] + u[1] * v[1] + u[2] * v[2];
}

/** @param {Vector} v */
function magnitude(v) {
  return Math.sqrt(dotProduct(v, v));
}

/**
 * Compute the angle between vector u and v
 * @param {Vector} u
 * @param {Vector} v
 */
function angleBetweenVectors(u, v) {
  const dp = dotProduct(u, v);
  if (dp === 0) {
    return 0;
  }
  const um = magnitude(u);
  const vm = magnitude(v);
  return Math.acos(dp / (um * vm)) * (180 / Math.PI);
}

while (true) {
  const vesselDirection = await vessel.direction(surfaceFrame);

  // Get the direction of the vessel in the horizon plane
  /** @type {Vector} */
  const horizonDirection = [0, vesselDirection[1], vesselDirection[2]];

  // Compute the pitch - the angle between the vessels direction and
  // the direction in the horizon plane
  let pitch = angleBetweenVectors(vesselDirection, horizonDirection);
  if (vesselDirection[0] < 0) {
    pitch = -pitch;
  }

  // Compute the heading - the angle between north and
  // the direction in the horizon plane
  /** @type {Vector} */
  const north = [0, 1, 0];
  let heading = angleBetweenVectors(north, horizonDirection);
  if (horizonDirection[2] < 0) {
    heading = 360 - heading;
  }

  // Compute the roll
  // Compute the plane running through the vessels direction
  // and the upwards direction
  /** @type {Vector} */
  const up = [1, 0, 0];
  const planeNormal = crossProduct(vesselDirection, up);
  // Compute the upwards direction of the vessel
  const vesselUp = await conn.spaceCenter.transformDirection(
    [0, 0, -1],
    vesselFrame,
    surfaceFrame,
  );
  // Compute the angle between the upwards direction of
  // the vessel and the plane normal
  let roll = angleBetweenVectors(vesselUp, planeNormal);
  // Adjust so that the angle is between -180 and 180 and
  // rolling right is +ve and left is -ve
  if (vesselUp[0] > 0) {
    roll *= -1;
  } else if (roll < 0) {
    roll += 180;
  } else {
    roll -= 180;
  }

  const format = (/** @type {number} */ x) => x.toFixed(1).padStart(5);
  console.log(
    `pitch = ${format(pitch)}, heading = ${format(heading)}, ` +
      `roll = ${format(roll)}`,
  );

  await new Promise((resolve) => setTimeout(resolve, 1000));
}
