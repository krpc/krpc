## client/lua

- Null values
  - **Breaking:** Return `nil` for a null result, in place of `Types.none` (#1278)
  - Pass a `nil` argument as null, for a parameter that can be null and has no default (#1278)
  - Fix a `nil` argument dropping the arguments after it (#1278)
