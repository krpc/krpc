## core

- Add variants of the server side function procedures that take ids in place of names, such as
  `KRPC.Type.ClassTypeById` and `KRPC.Expression.GetFieldByIndex` (#1336)

## client/cnano

- Add a `KRPC_CLASS_`, `KRPC_ENUMERATION_` or `KRPC_STRUCT_` macro for each type of a service, with
  its ids for server side functions (#1336)
- Add a `KRPC_FIELD_` macro for each field of a structure, with its position for
  `krpc_KRPC_Expression_GetFieldByIndex` (#1336)

## tools/krpctools

- Generate the C-nano `KRPC_CLASS_`, `KRPC_ENUMERATION_`, `KRPC_STRUCT_` and `KRPC_FIELD_` macros
  (#1336)
