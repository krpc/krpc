"""The .NET tools bundled in krpctools, which run on the dotnet host of an installed .NET 10
runtime."""

import os
import platform
import shutil
import sys
from importlib.resources import files

# The TestServer is published for each platform, as serial IO needs the System.IO.Ports build
# for it. Other platforms take the Linux build, which runs everything except serial IO.
RID = "win-x64" if sys.platform == "win32" else "linux-x64"

DOTNET = "dotnet.exe" if sys.platform == "win32" else "dotnet"

# The architecture names in the DOTNET_ROOT_<ARCH> variables and the install_location_<arch>
# files
ARCHITECTURES = {"x86_64": "x64", "amd64": "x64", "aarch64": "arm64", "arm64": "arm64"}


def bin_dir():
    """The directory of the bundled tools."""
    return str(files("krpctools") / "bin")


def testserver():
    """The path to the bundled TestServer assembly for this platform."""
    return os.path.join(bin_dir(), "TestServer", RID, "TestServer.dll")


def servicedefinitions_dir():
    """The directory of the bundled ServiceDefinitions tool."""
    return os.path.join(bin_dir(), "ServiceDefinitions")


def _registered_locations(architecture):
    """The install locations the .NET installers register."""
    if sys.platform == "win32":
        import winreg  # noqa: PLC0415

        key = r"SOFTWARE\dotnet\Setup\InstalledVersions\%s" % (architecture or "x64")
        try:
            with winreg.OpenKey(
                winreg.HKEY_LOCAL_MACHINE,
                key,
                0,
                winreg.KEY_READ | winreg.KEY_WOW64_32KEY,
            ) as handle:
                return [winreg.QueryValueEx(handle, "InstallLocation")[0]]
        except OSError:
            return []
    locations = []
    names = ["install_location"]
    if architecture:
        names.insert(0, "install_location_" + architecture)
    for name in names:
        try:
            with open(os.path.join("/etc/dotnet", name), encoding="utf-8") as config:
                location = config.readline().strip()
        except OSError:
            continue
        if location:
            locations.append(location)
    return locations


def _default_locations():
    """The locations the .NET installers and Linux packages install to by default."""
    if sys.platform == "win32":
        program_files = os.environ.get("ProgramFiles", r"C:\Program Files")
        return [os.path.join(program_files, "dotnet")]
    return ["/usr/lib/dotnet", "/usr/share/dotnet", "/usr/local/share/dotnet"]


def find():
    """The path to the dotnet host. It is looked for in DOTNET_ROOT_<ARCH> and DOTNET_ROOT, then
    on the PATH, then in the registered and default install locations. Returns None if there is
    no dotnet host."""
    architecture = ARCHITECTURES.get(platform.machine().lower())
    variables = ["DOTNET_ROOT"]
    if architecture:
        variables.insert(0, "DOTNET_ROOT_" + architecture.upper())
    roots = [os.environ[name] for name in variables if os.environ.get(name)]
    on_path = shutil.which("dotnet")
    candidates = [os.path.join(root, DOTNET) for root in roots]
    if on_path:
        candidates.append(on_path)
    candidates += [
        os.path.join(root, DOTNET)
        for root in _registered_locations(architecture) + _default_locations()
    ]
    for candidate in candidates:
        if os.path.isfile(candidate):
            return candidate
    return None


def command(assembly, args=()):
    """The command that runs an assembly on the dotnet host."""
    dotnet = find()
    if dotnet is None:
        raise RuntimeError(
            "dotnet not found. Install the .NET 10 runtime from "
            "https://dotnet.microsoft.com/download/dotnet/10.0, or set DOTNET_ROOT to its "
            "install directory"
        )
    return [dotnet, assembly, *args]
