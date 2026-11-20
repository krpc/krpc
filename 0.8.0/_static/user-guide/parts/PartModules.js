import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const parts = await vessel.getParts();

// [docs]
// Print the fields the right-click menu shows for each engine
for (const module of await parts.modulesWithName("ModuleEngines")) {
  for (const field of await module.getFieldList()) {
    if (await field.getVisible()) {
      console.log(
        await (await module.getPart()).getTitle(),
        await field.getGuiName(),
        await field.getValue(),
      );
    }
  }
}

// Limit the thrust of each engine to half
for (const module of await parts.modulesWithName("ModuleEngines")) {
  for (const field of await module.getFieldList()) {
    if ((await field.getName()) === "thrustPercentage") {
      await field.setFloatValue(50);
    }
  }
}

// Extend the solar panels, as their Extend button does
const panels = await parts.modulesWithName("ModuleDeployableSolarPanel");
for (const module of panels) {
  for (const event of await module.getEventList()) {
    if ((await event.getName()) === "Extend" && (await event.getActive())) {
      await event.trigger();
    }
  }
}
// [/docs]
await conn.close();
