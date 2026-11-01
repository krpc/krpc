## core

- Server status
  - **Breaking:** Return `KRPC.GetStatus` as the struct `KRPC.ServerStatus`, with the settings
    and statistics in nested structs, and remove the `Status` message (#1244)
  - **Breaking:** Rename the stream counters to `Streams`, `StreamEvaluations` and
    `StreamEvaluationRate` (#1244)
  - Add `TickHoldTimeout`, `DeferredCalls` and `StreamUpdatesSent` to the server status (#1244)
  - Add `ClientInfo.ProtocolVersion`, the protocol version of each client (#1244)

## server

- Show the tick hold timeout, deferred calls and stream updates in the info window, and name
  the stream counters as evaluations (#1244)
- Fix the two "Time per update" rows in the info window showing each other's value (#1244)

## client/cnano

- **Breaking:** Return `krpc_KRPC_GetStatus` as `krpc_KRPC_ServerStatus_t`, whose `version` is
  allocated on the heap (#1244)

## client/csharp

- **Breaking:** Return `KRPC.GetStatus` as the struct `ServerStatus`, with the settings and
  statistics under `Settings` and `Statistics` (#1244)

## client/cpp

- **Breaking:** Return `KRPC::get_status` as the struct `KRPC::ServerStatus`, whose fields are
  members, with the settings and statistics under `settings` and `statistics` (#1244)

## client/java

- **Breaking:** Return `KRPC.getStatus` as the struct `KRPC.ServerStatus`, with the settings
  and statistics under `getSettings` and `getStatistics` (#1244)

## client/lua

- **Breaking:** Return `get_status` as a struct, with the settings and statistics under
  `settings` and `statistics` (#1244)

## client/python

- **Breaking:** Return `KRPC.get_status` as the named tuple `ServerStatus`, with the
  settings and statistics under `settings` and `statistics` (#1244)
