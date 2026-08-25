"""Filesystem locations and the game's executable, shared by the test framework and the
install/run entrypoints.

This is a leaf module (it imports nothing from the rest of krpctest) so that
``krpctest.install`` and ``krpctest.run_ksp`` can reuse these helpers without creating an
import cycle with the ``krpctest`` package.
"""

import os
import subprocess
import sys

# The name of the game's executable here. An install is built for one platform, and the
# name it gives the executable is what says which.
_KSP_EXECUTABLE = "KSP_x64.exe" if sys.platform == "win32" else "KSP.x86_64"


def get_ksp_executable(ksp_dir=None):
    """The game's executable within the install. An install for another platform names its
    executable something else, so say that rather than leaving the caller to report that a
    file it never named is missing."""
    ksp_dir = get_ksp_dir(ksp_dir)
    path = os.path.join(ksp_dir, _KSP_EXECUTABLE)
    if not os.path.exists(path):
        raise RuntimeError(
            "No %s in %s. That is what the game's executable is called on this platform, so "
            "the install is either for another one or not a KSP install."
            % (_KSP_EXECUTABLE, ksp_dir)
        )
    return path


def kill_ksp():
    """Kill any running game, by name rather than by handle. The last resort of a run whose
    own game will not stop, so it does not outlive the run that started it."""
    if sys.platform == "win32":
        command = ["taskkill", "/f", "/im", _KSP_EXECUTABLE]
    else:
        command = ["pkill", "-f", "KSP[.]x86_64"]
    try:
        subprocess.call(command)
    except OSError:
        pass


def get_ksp_dir(ksp_dir=None):
    """Resolve the KSP install directory. Precedence: an explicit ksp_dir argument, then the
    KSP_DIR environment variable. There is no default - set KSP_DIR (or pass --ksp-dir) to the
    path of your KSP install."""
    if ksp_dir is None:
        ksp_dir = os.environ.get("KSP_DIR")
    if not ksp_dir:
        raise RuntimeError(
            "No KSP install specified. Set the KSP_DIR environment variable, or pass --ksp-dir, "
            "to the path of your KSP install."
        )
    if not os.path.exists(ksp_dir):
        raise RuntimeError("KSP dir not found at %s" % ksp_dir)
    return ksp_dir


def get_repo_root():
    """The repository root.

    Under `bazel run`, bazel names the root in BUILD_WORKSPACE_DIRECTORY. Take it from there
    rather than searching: the process starts in the runfiles tree, which sits under bazel's
    execroot, and the execroot has a MODULE.bazel of its own that the search below would
    find first.

    Otherwise, walk up from the working directory looking for MODULE.bazel. Tests run from a
    service's test directory, which is inside the repo, even though KSP_DIR points at a
    separate KSP install."""
    workspace = os.environ.get("BUILD_WORKSPACE_DIRECTORY")
    if workspace:
        return workspace
    path = os.getcwd()
    while True:
        if os.path.exists(os.path.join(path, "MODULE.bazel")):
            return path
        parent = os.path.dirname(path)
        if parent == path:
            raise RuntimeError(
                "Could not find the repository root (MODULE.bazel) above %s"
                % os.getcwd()
            )
        path = parent
