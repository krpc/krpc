""" The versions of kRPC for KSA, of the kRPC clients it works with, and of the game and loader it
is built and tested against.

tools/release/publish-docs.sh reads mod_version from this file too.
"""

# The version of the mod
mod_version = "0.3.0"

# The release of the kRPC client libraries the mod works with
client_version = "0.8.0"

# The KSA build the mod is compiled and tested against
ksa_version = "2026.11.16.5901"

# The StarMap release, and the sha256 of its zip
starmap_version = "0.5.4"
starmap_sha256 = "bcf14ac68dfb6558152586fd98776fc5013006a0406c37792f5f6afcf0561366"
