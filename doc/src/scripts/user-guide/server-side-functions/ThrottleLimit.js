import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const control = await vessel.getControl();
const flight = await vessel.flight(await (await (await vessel.getOrbit()).getBody()).getReferenceFrame());
// [docs]
const limitThrottle = await conn.compile(
  ({ control, flight }) => {
    // Throttle back as the vertical speed nears 100 m/s
    control.throttle = Math.max(0, Math.min(1, (100 - flight.verticalSpeed) / 20));
  },
  { control, flight },
);

const stream = await conn.addStream(limitThrottle);
await new Promise((resolve) => setTimeout(resolve, 60000));
await stream.remove();
// [/docs]
