"""Filesystem locations shared by the test framework and the install/run entrypoints.

This is a leaf module (it imports nothing from the rest of krpctest) so that
``krpctest.install`` and ``krpctest.run_ksp`` can reuse these helpers without creating an
import cycle with the ``krpctest`` package.
"""

import os


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
    """The repository root, found by walking up from the working directory looking for
    MODULE.bazel. Tests run from a service's test directory, which is inside the repo,
    even though KSP_DIR points at a separate KSP install."""
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
