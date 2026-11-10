## core

- Type ids
  - Give each class, enumeration, structure and exception an id, a hash of its name (#1324)
  - Add `Id` to `KRPCClass`, `KRPCEnum`, `KRPCStruct` and `KRPCException`, to resolve an id
    clash (#1324)
  - Add `id` to the `Class`, `Enumeration`, `Struct` and `Exception` messages that
    `KRPC.GetServices` returns, and `service_id` and `id` to the `Type` message (#1324)
  - Add `service_id` and `exception_id` to the `Error` message (#1324)
  - **Breaking:** Rename `name` in the `Error` message to `exception` (#1324)
  - **Breaking:** Reject two types of a service with the same name, ignoring case (#1324)
