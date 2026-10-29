# The versions kRPC for KSA is built against, sourced by the scripts beside it.
# The mod's own version is <Version> in Directory.Build.props.

# The kRPC release: the server core and krpctools come from its PyPI packages, and
# the stubs target its clients
krpc_version=0.7.0

# The StarMap release, and the sha256 of its zip
starmap_version=0.4.8
starmap_sha256=5324240f15d6841835158940057359ef011149bbc30b4d50f7b27480df7b2d5c

# The KSA build the mod is compiled and tested against
ksa_version=2026.10.25.5707

mod_version="$(sed -n 's|.*<Version>\(.*\)</Version>.*|\1|p' "$(dirname "${BASH_SOURCE[0]}")/Directory.Build.props")"
