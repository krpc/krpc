import { connect, KRPC } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const parts = await vessel.getParts();
const root = await parts.getRoot();
const temperature = await conn.addStream(() => root.getTemperature());

try {
  while (true) {
    await temperature.wait();
    console.log("Temperature", temperature.value);
  }
} catch (error) {
  if (!(error instanceof KRPC.ObjectDestroyedException)) {
    throw error;
  }
  console.log("The part was destroyed");
}
