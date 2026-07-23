# CLAUDE.md

**kRPC** — an RPC server for Kerbal Space Program (KSP 1.12.5). A C# mod runs in the game, services
add RPCs, clients in several languages talk to it over protobuf (TCP, websockets, serial).
Current version: `tools/krpc-version.sh`. Full setup: `Development-Guide.md`.

## Layout

 * `server/` — server plugin; `core/` — shared C# library; `protobuf/` — wire protocol.
 * `service/` — service DLLs (SpaceCenter, Drawing, UI, InfernalRobotics, RemoteTech, …).
 * `client/` — one dir per language; `serialio`/`websockets` are protocol test harnesses.
 * `tools/krpctools/` — `clientgen` (stubs), `docgen` (API docs), `servicedefs`.
 * `tools/krpctest/` — in-game test framework; `tools/TestServer/` — game-less server.
 * `tools/build/` — Bazel rules/toolchains. KSP assemblies come from `@ksp` in `MODULE.bazel`; no `lib/`.
 * `doc/` — documentation sources.

## Build

Bazel, from the repo root. Hermetic — all toolchains and the KSP assemblies are downloaded; nothing
to install before or after. Deps in `MODULE.bazel`; Python deps locked in
`tools/build/python/requirements_lock.txt` (`bazel run //tools/build/python:requirements.update`).
Outputs in `bazel-bin`, test logs in `bazel-testlogs`.

 * `//:krpc` — mod release zip. `//server`, `//service/SpaceCenter`, `//client/python`, … — one component.
 * `//doc:html`, `//doc:pdf`; `bazel run //doc:serve` — serve with rebuild-on-edit.
 * `//:csproj` — generate the C# sources `kRPC.sln` needs.

### Tests and lint

 * `bazel test //:test` — full non-game suite. **The gate before proposing a change.**
   `bazel test //...` does not work.
 * `//:lint` — Python, C++, C, Java, doc lint.
 * `//core:test`, `//client/python:test`, … — one package, when iterating.
 * `//doc:spelling` — American-English gate; `//doc:check-documented` — every RPC documented.
 * `//:csproj-test` — `.csproj` file-list check.
 * Game tests are **not** in `//:test` — see below.
 * Client comms tests use a local `TestServer` (no KSP needed); SerialIO ones need `socat`.
 * Flags: `--test_output=streamed`, `--cache_test_results=false`, `--subcommands`,
   `--config=system-llvm` (C/C++ as CI), `--config=windows`.

### C# sources

 * **New C# file → add `<Compile Include="…" />` to the project's `.csproj`.** `//:csproj-test` catches misses.
 * Reproduce a CI compile failure: `dotnet build <project>.csproj -c Debug -warnaserror -p:SolutionDir=$PWD/`
   (needs generated files staged).

### KSP assemblies

The mod compiles against **stub** assemblies (`@ksp`, downloaded by Bazel): every method body is
`throw new NotImplementedException()`, so they give the API and nothing else. To find out what the
game actually does, disassemble the real ones from the install:

```bash
monodis --output=/tmp/ksp.il "$KSP_DIR/KSP_Data/Managed/Assembly-CSharp.dll"
```

The whole game is in that one assembly, and the listing is millions of lines — grep it
(`\.class.*<Name>`, `\.custom.*KSPScenario`) rather than reading it.

## Integration tests

Python tests driving a live game, in `service/*/test/`. Need a real KSP install via `KSP_DIR` or
`--ksp-dir`. Everything after `--` goes to pytest.

```bash
bazel run //:test-ingame                                                  # whole suite
bazel run //:test-ingame -- service/SpaceCenter/test/test_camera.py -v    # one file
bazel run //:test-ingame -- "…::TestClass::test_method"                   # one test
bazel run //:test-ingame -- -k test_camera_mode                           # by name
```

 * **Auto-launches KSP**: builds/installs DLLs and required mods, launches, loads a save, runs, stops.
   All code changes picked up automatically.
 * Cold run ~70–90s → **start with `run_in_background`**, wait via Monitor
   `until grep -qE " passed| failed"`. Never poll.
 * Progress logged per stage (`building and installing kRPC`, `launching KSP`, `waiting for kRPC
   server...`, `stopping KSP`).

### Mods

 * Declared per test class: `mods = ["RemoteTech"]`.
 * Supported names: `MODS` in `tools/krpctest/krpctest/install.py`; archives pinned in `MODULE.bazel`,
   exposed by `tools/mods/BUILD.bazel`.
 * Install reconciles GameData to **exactly** the requested set (plus kRPC) — required-but-missing and
   installed-but-unneeded are both wrong.
 * Pytest groups tests by mod set; game restarts at most once per set.

### Iterating without reloading

```bash
bazel run //:run-ksp -- --load-game=krpctest --mods=RemoteTech
bazel run //:test-ingame -- --no-launch service/RemoteTech/test/test_remotetech.py -v
```

 * `--no-launch` (`KRPC_AUTO_LAUNCH=0`) errors on a missing server instead of launching a second game.
   Pass it whenever a game should already be up — a still-loading game looks like no game.
 * Only works for tests whose mod set the running game already has.
 * `//:run-ksp` builds, installs, launches, tails the kRPC log. `--load-game` brings the server up;
   other `--load-*` switch vessel or launch a craft (`-- --help`). Re-run after any C# change.
 * Run *killed* rather than interrupted → cleanup hook skipped → `pkill -f KSP.x86_64`.
 * Trace from in-game with `KRPC.Utils.Logger.WriteLine("…")`, then grep `$KSP_DIR/KSP.log`.

## Comments and docs

 * Explain what the code does now and why; must make sense to someone who never saw the old version.
 * **Never reference what a change replaced** — no "replaces X", "instead of the old Y", "previously",
   "no longer needed", no migration/phase references. That belongs in the commit message and changelog.
 * American English everywhere ("meters", "color", "normalized"); `//doc:spelling` enforces it.
   Legitimate unknown words → `doc/src/dictionary.txt`.

## Commits

 * One thing per commit.
 * Short message: single-line summary, then one or two short paragraphs.
 * No "Authored by" / `Co-Authored-By` lines.

## Pull requests

 * Concise technical prose; bullets only for discrete items. Multi-part change → lead paragraphs with
   a short bold phrase (`**Root cause.**`, `**Fix.**`, `**Testing.**`).
 * State the problem, the change, and — for a fix — how it was verified.
 * Do not state that tests CI covers would pass (like `bazel test //:test`).
 * Do not repeat implementation details that are clear from the commit messages.
 * End with `Fixes #NNN` when an issue is linked; omit otherwise.
 * **No AI-attribution or "Generated with" footer** — PR bodies and commit messages alike.
 * One paragraph per line; GitHub renders single newlines literally. Reflow wrapped text.
 * Assign the current milestone (`tools/krpc-version.sh`) + label by type (`bug`/`enhancement`/`docs`)
   and component (`server`, `client:python`, `service:space-center`, `tools`, …), e.g.
   `gh pr edit N --milestone "0.6.0" --add-label bug --add-label server`.

## Changelogs

Per-component `CHANGELOG.md`, markdown in the style of [keepachangelog.com](https://keepachangelog.com)
but with entries grouped logically rather than by add/change/fix type. Entries are `- ` bullets under a
`## [X.Y.Z]` version header, newest version first; wrapped lines indent 2 spaces. The current version
(`tools/krpc-version.sh`) is headed `## [X.Y.Z] - unreleased`; releasing drops the suffix (`30-tag.py`).
Add entries under that header, creating it if needed, only for components actually modified.
`doc/src/changelog.rst` is generated from these files.

 * **Before a branch is pushed, consolidate into a dedicated final commit** (single-commit PRs
   excepted) — every PR touches these files. On an unpushed local branch this is relaxed: you may
   commit changelog entries freely, e.g. alongside the change they describe. Squash them into that
   single final commit before the branch is pushed.
 * Relative to the **last release**, not the previous commit. A fix to behavior introduced during the
   current unreleased dev cycle gets **no entry**.
 * User-facing changes only. No changes since the last release → no version section at all; never add
   an empty or padding section.
 * Code identifiers (RPCs, types, settings, file names) go in backticks for monospace, e.g. `` `SpaceCenter.Camera` ``.
 * At most one marker: `**Breaking:**` (users must react) or `**Deprecated:**` (exists, avoid). Both →
   `**Breaking:**`, mention the deprecation in the text. A marker opens the entry, before the text.
 * Suffix `(#NNN)`. Issues and PRs share one number sequence. The number is only required before the
   branch is pushed as a PR — on an unpushed local branch you may commit an entry without it and fill
   it in later. To predict it, take one more than the highest existing number:

   ```bash
   gh api repos/:owner/:repo/issues -X GET -f state=all -f per_page=1 --jq '.[0].number'
   ```

   Create the PR right after pushing, then check the number it got. If it matches, nothing to do; if
   someone else took the number in between, fix the entry and **amend it into the changelog commit**
   (`git commit --amend`, then `git push --force-with-lease`) rather than adding a second one — the
   changelog stays a single commit. Only suffix the entries this PR adds; leave existing ones alone.
