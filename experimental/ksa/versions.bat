@rem The versions kRPC for KSA is built against, called by the scripts beside it.
@rem The mod's own version is <Version> in Directory.Build.props.

@rem The kRPC release: the server core and krpctools come from its PyPI packages, and
@rem the stubs target its clients
set "krpc_version=0.7.0"

@rem The StarMap release, and the sha256 of its zip
set "starmap_version=0.4.8"
set "starmap_sha256=5324240f15d6841835158940057359ef011149bbc30b4d50f7b27480df7b2d5c"

@rem The KSA build the mod is compiled and tested against
set "ksa_version=2026.10.25.5707"

for /f "tokens=3 delims=<>" %%v in ('findstr /c:"<Version>" "%~dp0Directory.Build.props"') do set "mod_version=%%v"
