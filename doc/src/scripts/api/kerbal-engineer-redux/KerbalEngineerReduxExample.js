import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Kerbal Engineer Redux Example" });
const vessel = await conn.spaceCenter.getActiveVessel();
const ker = conn.kerbalEngineerRedux;

for (const stage of (await ker.stages(vessel)).reverse()) {
  console.log(
    `Stage ${stage.number}: ${stage.deltaV.toFixed(0)} m/s, TWR ${stage.availableTWR.toFixed(2)}`,
  );
}

const burn = await ker.burnTime(vessel, 500);
if (burn === null) {
  console.log("Not enough delta-v for the burn");
} else {
  console.log(`A 500 m/s burn takes ${burn.duration.toFixed(1)} seconds`);
}
