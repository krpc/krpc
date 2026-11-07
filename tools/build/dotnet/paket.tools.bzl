"GENERATED"

load("@rules_dotnet//dotnet:defs.bzl", "nuget_repo")

def tools():
    "tools"
    nuget_repo(
        name = "paket.tools",
        packages = [
            {"name": "Paket", "id": "Paket", "version": "11.0.1", "sha512": "sha512-i6fCViUL3/f8FaHPMknsee2UL67PGMmYKKalq6qNd2k6CG4DbzFtG6bSGlAWkFRx4/92U/NRNgxEYFPSasVWsQ==", "sources": ["https://api.nuget.org/v3/index.json"], "dependencies": {"net10.0": [], "net11": [], "net20": [], "net30": [], "net35": [], "net40": [], "net403": [], "net45": [], "net451": [], "net452": [], "net46": [], "net461": [], "net462": [], "net47": [], "net471": [], "net472": [], "net48": [], "net5.0": [], "net6.0": [], "net7.0": [], "net8.0": [], "net9.0": [], "netcoreapp1.0": [], "netcoreapp1.1": [], "netcoreapp2.0": [], "netcoreapp2.1": [], "netcoreapp2.2": [], "netcoreapp3.0": [], "netcoreapp3.1": [], "netstandard": [], "netstandard1.0": [], "netstandard1.1": [], "netstandard1.2": [], "netstandard1.3": [], "netstandard1.4": [], "netstandard1.5": [], "netstandard1.6": [], "netstandard2.0": [], "netstandard2.1": []}, "targeting_pack_overrides": [], "framework_list": [], "tools": {"net10.0": [{"name": "paket", "entrypoint": "paket.dll", "runner": "dotnet"}]}},
        ],
    )
