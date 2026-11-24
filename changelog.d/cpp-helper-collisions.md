## tools/krpctools

- Client generation
  - Fix the generated C++ code for a member named as the `_stream`, `_call` or `_call_template`
    function of another member, which becomes `foo__stream` (#1414)
