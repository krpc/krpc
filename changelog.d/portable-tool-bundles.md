## tools/krpctools

- Add `krpc-testserver`, which runs the `TestServer` bundled with krpctools (#1228)
- Run `krpc-servicedefs` and the `TestServer` on Windows (#1228)
- Fix serial IO in the `TestServer` (#1228)
- **Breaking:** Stop releasing the `TestServer` archive and Docker image, replaced by
  `krpc-testserver` (#1228)
- **Breaking:** Pass the directories of the assemblies a service references to
  `krpc-servicedefs` with `--reference-dir`, in place of the Kerbal Space Program directory
  (#1228)
- **Deprecated:** `--ksp` in `krpc-clientgen`, superseded by `--reference-dir` (#1228)
