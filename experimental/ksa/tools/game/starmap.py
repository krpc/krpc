"""Launch KSA through StarMap:
starmap.py RELEASE STARMAP_DIR KSA_DIR LOG [game arguments].

StarMap loads the game into its own process, resolves its files from the working directory
and reads the game location from StarMapConfig.json beside it. We run the StarMap install in
STARMAP_DIR, which must be RELEASE, the release pinned in config.bzl. The game's native
libraries are found through LD_LIBRARY_PATH on Linux and PATH on Windows, as the loader does
not search the working directory.
"""

import filecmp
import json
import os
import subprocess
import sys
from pathlib import Path


def check(release: Path, directory: Path) -> None:
    """Exit unless directory holds the StarMap release."""
    installed = directory / "StarMap.dll"
    if not installed.exists():
        sys.exit(
            f"STARMAP_DIR, {directory}, holds no StarMap. Install the StarMap release "
            "pinned as starmap_version in experimental/ksa/config.bzl there."
        )
    if not filecmp.cmp(release / "StarMap.dll", installed, shallow=False):
        sys.exit(
            f"STARMAP_DIR, {directory}, holds a StarMap other than the release pinned as "
            "starmap_version in experimental/ksa/config.bzl. Install that release there."
        )


def main() -> None:
    release, starmap_dir, ksa_dir, log, *args = sys.argv[1:]
    directory = Path(starmap_dir)
    check(Path(release), directory)
    ksa = Path(ksa_dir).resolve()
    config = {"GameLocation": str(ksa), "RepositoryLocation": "", "GameArguments": []}
    (directory / "StarMapConfig.json").write_text(json.dumps(config, indent=2) + "\n")

    Path(log).parent.mkdir(parents=True, exist_ok=True)
    output = open(log, "wb")  # noqa: SIM115
    env = dict(os.environ)
    os.chdir(directory)
    if sys.platform == "win32":
        env["PATH"] = str(ksa) + os.pathsep + env.get("PATH", "")
        process = subprocess.Popen(
            [str(directory / "StarMap.exe"), *args],
            stdout=output,
            stderr=output,
            env=env,
        )
        sys.exit(process.wait())
    env["LD_LIBRARY_PATH"] = str(ksa) + (
        os.pathsep + env["LD_LIBRARY_PATH"] if env.get("LD_LIBRARY_PATH") else ""
    )
    # Replace this process, so the game is the process that launched it
    os.dup2(output.fileno(), 1)
    os.dup2(output.fileno(), 2)
    os.execvpe("dotnet", ["dotnet", "StarMap.dll", *args], env)


if __name__ == "__main__":
    main()
