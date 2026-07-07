
## Staging API migration (client authors)

kRPC now exposes first-class **Stage** objects on **Vessel** for activation and
decouple stages, plus vessel-level delta-v and burn-time properties. Legacy
`Parts.InStage`, `Parts.InDecoupleStage`, and `Vessel.ResourcesInDecoupleStage`
remain but are deprecated.

| Legacy | Replacement |
|--------|-------------|
| `parts.in_stage(n)` | `vessel.stage_at(n).parts` |
| `parts.in_decouple_stage(n)` | `vessel.decouple_stage_at(n).parts` |
| `vessel.resources_in_decouple_stage(n)` | `vessel.decouple_stage_at(n).resources()` |
| (new) per-stage Δv | `vessel.stage_at(n).delta_v` (activation only) |
| (new) vessel Δv | `vessel.delta_v`, `vessel.vacuum_delta_v`, `vessel.burn_time` |

`decouple_stage_at(n)` raises an argument error when `n` is negative or greater
than the maximum decouple stage index. The legacy
`resources_in_decouple_stage(n)` call returned empty resources for those
out-of-range indices instead.

See the issue discussion and the generated SpaceCenter API docs for the full
background and client-facing examples.
