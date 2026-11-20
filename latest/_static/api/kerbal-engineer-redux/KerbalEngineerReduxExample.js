import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Kerbal Engineer Redux Example" });
const vessel = await conn.spaceCenter.getActiveVessel();
const ker = conn.kerbalEngineerRedux;

for (const stage of (await ker.stages(vessel)).reverse()) {
  const deltaV = stage.deltaV.toFixed(0);
  const twr = stage.availableTWR.toFixed(2);
  console.log(`Stage ${stage.number}: ${deltaV} m/s, TWR ${twr}`);
}

const burn = await ker.burnTime(vessel, 500);
if (burn === null) {
  console.log("Not enough delta-v for the burn");
} else {
  console.log(`A 500 m/s burn takes ${burn.duration.toFixed(1)} seconds`);
}
