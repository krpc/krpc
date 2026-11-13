import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const flight = await vessel.flight();
const { Expression, Type } = conn.krpc;

// above = meanAltitude > 1000, or false if the vessel is not loaded
const above = await Expression.variable("above", await Type.bool());
const meanAltitude = conn.getCall(() => flight.getMeanAltitude());
const check = await Expression.tryCatch(
  await Expression.assign(
    above,
    await Expression.greaterThan(
      await Expression.call(meanAltitude),
      await Expression.constantDouble(1000),
    ),
  ),
  "KRPC",
  "ObjectNotLoadedException",
  null,
  await Expression.assign(above, await Expression.constantBool(false)),
);
const above1000m = await Expression.blockWithVariables([above], [check, above]);

const event = await conn.addEvent(above1000m);
await event.wait();
console.log("Altitude reached 1000m");
