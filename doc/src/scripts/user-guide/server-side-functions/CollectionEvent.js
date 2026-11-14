import { connect } from "@krpc-org/krpc";

const conn = await connect();
const { Expression, Type } = conn.krpc;
const vessel = await conn.spaceCenter.getActiveVessel();
const parts = await vessel.getParts();

// A function taking an engine, returning true if it is out of fuel
const engine = await Expression.parameter(
  "engine",
  await Type.classType("SpaceCenter", "Engine"),
);
const [firstEngine] = await parts.getEngines();
const hasFuel = conn.getCall(() => firstEngine.getHasFuel());
const outOfFuel = await Expression.lambda(
  [engine],
  await Expression.not(await Expression.callWithArguments(hasFuel, new Map([[0, engine]]))),
);

// Whether any engine satisfies it
const engines = conn.getCall(() => parts.getEngines());
const expr = await Expression.any(await Expression.call(engines), outOfFuel);

const event = await conn.addEvent(expr);
await event.wait();
console.log("An engine has run out of fuel");
