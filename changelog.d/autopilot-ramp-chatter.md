## service/SpaceCenter

- Fix the auto-pilot keeping its flexible-craft mitigations after separating a stage or docking (#1373)
- Make the auto-pilot turn with a moving target, such as a gravity turn's pitch (#1373)
- Make `AutoPilot.Wait` measure the angular velocity relative to the target (#1373)
