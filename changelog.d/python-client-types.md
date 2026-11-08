## client/python

- Services
  - **Breaking:** Move the modules of the KSP services from `krpc.services` to `krpc.ksp`, such as `krpc.ksp.spacecenter` (#1304)

## tools/krpctools

- Name the module of another service a generated Python module imports with `--include`, for a mod whose service uses another mod's types (#1304)
