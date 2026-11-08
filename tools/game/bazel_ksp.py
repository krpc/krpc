"""KSP with kRPC and its test add-on built from this repository with Bazel.

`bazel run //tools/game:test-ksp` and `bazel run //tools/game:run-ksp` pass this game as
`--game bazel_ksp:BazelKSP`, so each install builds and installs the current sources.
"""

import os
import subprocess

from krpctools.game.ksp import KSP

# The test-only assemblies the release omits. TestingTools is the add-on the tests drive the
# game with. KRPC.Benchmark is the benchmark service, which TestServer also loads, so an
# in-game measurement and a game-less one are the same measurement.
_ADDON_TARGETS = [
    "//tools/TestingTools",
    "//tools/benchmarks:KRPC.Benchmark.net472",
]


def repo_root():
    """The repository root: BUILD_WORKSPACE_DIRECTORY under `bazel run`, else the nearest
    directory above the working directory with a MODULE.bazel. The variable comes first, as
    the runfiles tree sits under an execroot with a MODULE.bazel of its own."""
    workspace = os.environ.get("BUILD_WORKSPACE_DIRECTORY")
    if workspace:
        return workspace
    path = os.getcwd()
    while not os.path.exists(os.path.join(path, "MODULE.bazel")):
        parent = os.path.dirname(path)
        if parent == path:
            raise RuntimeError(
                "Could not find the repository root (MODULE.bazel) above %s"
                % os.getcwd()
            )
        path = parent
    return path


class BazelKSP(KSP):
    """KSP with the kRPC release archive and the add-on from `bazel build`."""

    def _outputs(self, target):
        # rules_dotnet writes outputs to a configuration-specific directory, so ask Bazel
        # for their paths
        output = subprocess.check_output(
            ["bazel", "cquery", "--output=files", target], cwd=repo_root(), text=True
        )
        return [
            os.path.join(repo_root(), line)
            for line in output.splitlines()
            if line.strip()
        ]

    def install(self, mods=()):
        targets = list(_ADDON_TARGETS)
        if self.krpc is None:
            targets.append("//:krpc")
        subprocess.check_call(["bazel", "build", *targets], cwd=repo_root())
        super().install(mods)

    def krpc_source(self):
        if self.krpc is not None:
            return self.krpc, None
        # The archive's name embeds the version
        return self._outputs("//:krpc")[-1], None

    def addon_files(self):
        return [path for target in _ADDON_TARGETS for path in self._outputs(target)]

    def run_command(self, mods):
        return (
            "bazel run //tools/game:run-ksp -- --load-game=krpctest"
            + self._mods_option(mods)
        )
