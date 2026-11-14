import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Kerbal Alarm Clock Example" });
const kac = conn.kerbalAlarmClock;

const alarm = await kac.createAlarm(
  kac.AlarmType.raw,
  "My New Alarm",
  (await conn.spaceCenter.getUT()) + 10,
);

await alarm.setNotes("10 seconds have now passed since the alarm was created.");
await alarm.setAction(kac.AlarmAction.messageOnly);
