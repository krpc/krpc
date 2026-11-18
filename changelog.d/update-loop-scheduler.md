## core

- Server status
  - **Breaking:** Replace `MaxTimePerUpdate`, `BlockingRecv` and `RecvTimeout` in the settings
    with `TargetFrameRate`, `MaxTimePerFrame` and `MaxCallWait` (#1376)
  - **Breaking:** Replace the statistics per update with statistics per frame, and add
    `FrameRate`, `BudgetPerFrame` and `MedianTurnaround` (#1376)
- Wait for a client's next call by its measured time between calls, so a client that pauses
  for a few milliseconds between calls gets several calls per update (#1376)

## server

- **Breaking:** Replace "Max. time per update" with "Max. time per frame", 10 ms by default,
  and "Blocking receives" and "Receive timeout" with "Max. call wait", 5 ms by default (#1376)
- Hold the frame rate at the new "Target frame rate" setting with adaptive rate control,
  60 fps by default (#1376)
- Add presets to the advanced settings: "Game first", "Balanced" and "Calls first" (#1376)
- Reorganize the server info window around the frame, and add a "Limited by" row naming what
  holds down the call rate (#1376)
- Fix "Clear statistics" leaving the data read and written (#1376)

## tools/krpctools

- Add `--preset` to `krpc-testserver`, for its rate settings (#1376)
