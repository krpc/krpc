## [0.6.0] - unreleased
- Make available in all game scenes (#944)
- Add `AlarmType.ScienceLab` (#982)
- Add `AlarmAction.Custom` and `AlarmAction.Converted` (#982)
- Add `Alarm.Enabled`, `Alarm.PlaySound`, `Alarm.Triggered`, `Alarm.SupportsRepeat` and
  `Alarm.SupportsRepeatPeriod` (#982)
- Fix `Alarm.Remaining` always failing with an invalid cast error (#982)
- Fix alarm equality, so the same alarm obtained twice compares equal (#982)
- Fix accessing an alarm after it has been removed; it now fails with an "Alarm does not exist" error (#982)

## [0.5.0]
- Update API wrapper to work with KerbalAlarmClock v3.13.0.0

## [0.3.7]
- Add `KerbalAlarmClock.Available`

## [0.1.12]
- Renamed `Alarm.Delete` to `Alarm.Remove` (to not clash with C++ keyword)

## [0.1.10]
- Service now available in all game scenes (#135)

## [0.1.9]
- Initial version - API for Kerbal Alarm Clock (http://forum.kerbalspaceprogram.com/threads/24786)
