## [v0.8.0]

- Fix `Alarm.PlaySound` having no effect (#1179)
- Keep working when a Kerbal Alarm Clock update removes members, and fail only the RPCs that use
  them (#1341)
- Limit the service to the scenes Kerbal Alarm Clock runs in, apart from `Available`, which
  reports whether it is installed in any scene (#1341)

## [v0.7.0]

- An alarm keeps working across a quickload, and raises `KRPC.ObjectDestroyedException` once
  the mod no longer has an alarm with its id (#1051)

## [v0.6.0]
- Make available in all game scenes (#944)
- Add `AlarmType.ScienceLab` (#982)
- Add `AlarmAction.Custom` and `AlarmAction.Converted` (#982)
- Add `Alarm.Enabled`, `Alarm.PlaySound`, `Alarm.Triggered`, `Alarm.SupportsRepeat` and
  `Alarm.SupportsRepeatPeriod` (#982)
- Fix `Alarm.Remaining` always failing with an invalid cast error (#982)
- Fix alarm equality, so the same alarm obtained twice compares equal (#982)
- Fix accessing an alarm after it has been removed; it now fails with an "Alarm does not exist" error (#982)

## [v0.5.0]
- Update API wrapper to work with KerbalAlarmClock v3.13.0.0

## [v0.3.7]
- Add `KerbalAlarmClock.Available`

## [v0.1.12]
- Renamed `Alarm.Delete` to `Alarm.Remove` (to not clash with C++ keyword)

## [v0.1.10]
- Service now available in all game scenes (#135)

## [v0.1.9]
- Initial version - API for Kerbal Alarm Clock
