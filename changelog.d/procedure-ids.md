## core

- Procedure ids
  - **Breaking:** Derive each procedure id from a hash of its name, which changes every id (#1265)
  - Add `Id` to `KRPCProcedure` and `KRPCMethod`, and `GetterId` and `SetterId` to
    `KRPCProperty`, to resolve an id clash (#1265)
  - Add `KRPCProcedureId` to set the id of a procedure a class inherits (#1265)
  - Add `id` to the `Service` and `Procedure` messages that `KRPC.GetServices` returns (#1265)

## client/cnano

- **Breaking:** Call procedures by the hashed ids, which needs a server of the same version (#1265)
