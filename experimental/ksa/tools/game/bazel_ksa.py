"""KSA with kRPC for KSA and its TestingTools service built from this repository with Bazel.

`bazel run //experimental/ksa/tools/game:test-ksa` and `bazel run //experimental/ksa/tools/game:run-ksa` pass this
game as `--game bazel_ksa:BazelKSA`, so each install builds and installs the current sources.
"""

import logging
import os
import subprocess
import sys
import time
from pathlib import Path

import mods_folder
from python.runfiles import runfiles

from krpctools.game import game

log = logging.getLogger("krpctools.game")

# The mod with the TestingTools service
_MOD_TARGET = "//experimental/ksa:mod-testing"

# The save each test class starts from, written once a launched game has loaded its system
SAVE = "krpctest"

# Seconds to wait for a launched game to load a system with a controlled vehicle
LOAD_TIMEOUT = 300


def repo_root():
    """The repository root: BUILD_WORKSPACE_DIRECTORY under `bazel run`, else the nearest
    directory above the working directory with a MODULE.bazel."""
    workspace = os.environ.get("BUILD_WORKSPACE_DIRECTORY")
    if workspace:
        return Path(workspace)
    path = Path.cwd()
    while not (path / "MODULE.bazel").exists():
        if path.parent == path:
            raise RuntimeError(
                "Could not find the repository root (MODULE.bazel) above %s"
                % Path.cwd()
            )
        path = path.parent
    return path


def _test_settings():
    r = runfiles.Create()
    path = (
        r.Rlocation("_main/experimental/ksa/tools/TestingTools/settings.toml", "")
        if r is not None
        else None
    )
    if path is None or not os.path.exists(path):
        raise RuntimeError("The test settings are not in the runfiles")
    return path


def _starmap_release():
    r = runfiles.Create()
    path = r.Rlocation("starmap/StarMap.dll", "") if r is not None else None
    if path is None or not os.path.exists(path):
        raise RuntimeError("StarMap not found in the runfiles")
    return Path(path).parent


class BazelKSA(game.Game):
    """KSA, launched through StarMap, with the mod from `bazel build`."""

    name = "ksa"
    directory_variable = "KSA_DIR"
    client_name = "ksatest"

    def log_path(self):
        return str(repo_root() / "experimental" / "ksa" / "bin" / "ksa.log")

    def starmap_dir(self):
        """The StarMap folder the game runs from, STARMAP_DIR."""
        directory = os.environ.get("STARMAP_DIR")
        if not directory:
            raise RuntimeError(
                "No StarMap folder specified. Set STARMAP_DIR to a StarMap install."
            )
        if not os.path.isabs(directory):
            raise RuntimeError(
                "STARMAP_DIR must be an absolute path, not %s" % directory
            )
        return Path(directory)

    # Installing

    def install(self, mods=()):
        if self.krpc is None:
            subprocess.check_call(["bazel", "build", _MOD_TARGET], cwd=repo_root())
        super().install(mods)

    def krpc_source(self):
        if self.krpc is not None:
            return self.krpc, None
        output = subprocess.check_output(
            ["bazel", "cquery", "--output=files", _MOD_TARGET],
            cwd=repo_root(),
            text=True,
        )
        return str(repo_root() / output.split()[-1]), None

    def install_krpc(self, path):
        mods_folder.install(path)

    def install_addon(self):
        # The mod archive holds the TestingTools service. The test settings replace the
        # settings in the game's user folder.
        mods_folder.install_settings(_test_settings())

    def install_mod(self, mod, path):
        raise NotImplementedError("KSA has no mods to install")

    def remove_mod(self, mod):
        raise NotImplementedError("KSA has no mods to remove")

    # Launching

    def launch_command(self, args=()):
        launcher = Path(__file__).resolve().parent / "starmap.py"
        return [
            sys.executable,
            str(launcher),
            str(_starmap_release()),
            str(self.starmap_dir()),
            self.game_dir(),
            self.log_path(),
            *args,
        ]

    def run_command(self, mods):
        return "bazel run //experimental/ksa/tools/game:run-ksa"

    def kill_all(self):
        if sys.platform == "win32":
            subprocess.call(
                ["taskkill", "/f", "/im", "StarMap.exe"],
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
            )
            return
        # StarMap runs under dotnet, so its process is found by its command line
        for proc in Path("/proc").iterdir():
            try:
                cmdline = (proc / "cmdline").read_bytes().split(b"\0")
            except OSError:
                continue
            if b"StarMap.dll" in cmdline and proc.name.isdigit():
                try:
                    os.kill(int(proc.name), 9)
                except OSError:
                    pass

    # The running game

    def ensure(self, mods=()):
        process = self.process
        super().ensure(mods)
        if self.process is not None and self.process is not process:
            self._prepare()

    def _prepare(self):
        """Wait for a launched game to load its system, and save it for the tests."""
        conn = self.connect()
        deadline = time.monotonic() + LOAD_TIMEOUT
        log.info("waiting for KSA to load a system")
        while conn.ksa.active_vehicle is None:
            if time.monotonic() > deadline:
                self.stop()
                raise RuntimeError(
                    "KSA did not load a system with a controlled vehicle, see %s"
                    % self.log_path()
                )
            time.sleep(1)
        conn.testing_tools.suppress_update_popup()
        conn.ksa.save(SAVE)

    def stop(self):
        if self.process is not None:
            try:
                self.connect().testing_tools.quit()
                self.process.wait(timeout=30)
            except Exception:  # noqa: BLE001
                pass
        super().stop()
