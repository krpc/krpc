## client/cnano

- **Breaking:** Replace `krpc_KRPC_CompileFunction` and `krpc_KRPC_RunFunction` with
  `krpc_compile_function` and `krpc_run_function`, in `krpc_cnano/functions.h` (#1267)
- Add a `_call` function for each procedure, such as `krpc_SpaceCenter_Flight_Speed_call`, for
  `krpc_KRPC_Expression_Call` (#1267)

## tools/krpctools

- Generate a `_call` function for each procedure of a C-nano service (#1267)
