import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "InfernalRobotics Example" });
const vessel = await conn.spaceCenter.getActiveVessel();

const group = await conn.infernalRobotics.servoGroupWithName(vessel, "MyGroup");
if (group === null) {
  throw new Error("Group not found");
}

for (const servo of await group.getServos()) {
  console.log(await servo.getName(), await servo.getPosition());
}

await group.moveRight();
await new Promise((resolve) => setTimeout(resolve, 1000));
await group.stop();
