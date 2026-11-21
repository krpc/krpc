"""The KSA user mods folder, and installing the kRPC mod into it."""

import os
import re
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path

import tomllib


def documents_dir() -> Path:
    """The user's Documents folder."""
    if sys.platform == "win32":
        output = subprocess.run(
            [
                "powershell",
                "-NoProfile",
                "-Command",
                "[Environment]::GetFolderPath('MyDocuments')",
            ],
            check=True,
            capture_output=True,
            text=True,
        )
        return Path(output.stdout.strip())
    try:
        output = subprocess.run(
            ["xdg-user-dir", "DOCUMENTS"], check=True, capture_output=True, text=True
        )
        return Path(output.stdout.strip())
    except (OSError, subprocess.CalledProcessError):
        return Path.home() / "Documents"


def mods_dir() -> Path:
    """KSA_MODS_DIR, else <Documents>/My Games/Kitten Space Agency/mods."""
    if "KSA_MODS_DIR" in os.environ:
        return Path(os.environ["KSA_MODS_DIR"])
    return documents_dir() / "My Games" / "Kitten Space Agency" / "mods"


def install(archive: str | os.PathLike) -> Path:
    """Replace the kRPC mod folder with the archive's, and enable the mod in the game's
    manifest.toml. Returns the mod folder."""
    mods = mods_dir()
    shutil.rmtree(mods / "kRPC", ignore_errors=True)
    mods.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(archive) as z:
        z.extractall(mods)
    manifest = mods.parent / "manifest.toml"
    content = manifest.read_text() if manifest.exists() else ""
    manifest.write_text(enable_krpc(content))
    return mods / "kRPC"


def enable_krpc(manifest: str) -> str:
    """The manifest.toml text with the kRPC entry enabled, added if it is missing."""
    entries = tomllib.loads(manifest).get("mods", [])
    krpc = [entry for entry in entries if entry.get("id") == "kRPC"]
    if any(entry.get("enabled") is True for entry in krpc):
        return manifest
    if not krpc:
        return manifest + '\n[[mods]]\nid = "kRPC"\nenabled = true\n'
    # Edit the text of the entry, so the rest of the file is left as the game wrote it
    tables = re.split(r"(?m)^(?=[ \t]*\[)", manifest)
    for i, table in enumerate(tables):
        if re.search(r'(?m)^[ \t]*id[ \t]*=[ \t]*"kRPC"[ \t]*$', table):
            table, count = re.subn(
                r"(?m)^[ \t]*enabled[ \t]*=.*$", "enabled = true", table
            )
            if count == 0:
                table = re.sub(
                    r'(?m)^([ \t]*id[ \t]*=[ \t]*"kRPC"[ \t]*)$',
                    r"\1\nenabled = true",
                    table,
                )
            tables[i] = table
    return "".join(tables)
