kRPC for KSA @VERSION@
==================

Control Kitten Space Agency from scripts running outside the game. A server inside the
game exposes the game's API, and the kRPC @KRPC_VERSION@ client libraries call it from
Python, C#, C++, C, Java and Lua. kRPC for KSA is experimental, and its API may change
between releases.

Requires Kitten Space Agency @KSA_VERSION@ and the StarMap @STARMAP_VERSION@ code mod
loader.

    kRPC/   the mod
    stubs/      client stubs for C#, C++, C and Java, and the service definitions
    examples/   example Python scripts


INSTALL
-------

1. Install StarMap @STARMAP_VERSION@:
   https://github.com/StarMapLoader/StarMap/releases

2. Copy the kRPC folder into the KSA mods folder, so that you have
       <Documents>/My Games/Kitten Space Agency/mods/kRPC/KRPC.KSA.dll

3. Enable the mod in <Documents>/My Games/Kitten Space Agency/manifest.toml:

       [[mods]]
       id = "kRPC"
       enabled = true

4. Launch the game through StarMap.

The Borea mod manager can do all four steps: it installs StarMap and the mod, and
launches the game through StarMap.

Upgrading from 0.1.0, where the mod was named kRPC.KSA: delete the mods/kRPC.KSA folder,
and change its manifest.toml entry to id = "kRPC".

The server starts when the game has loaded its mods. By default it listens on 127.0.0.1,
ports 50000 and 50001, and accepts every connection. The settings are in
kRPC/settings.toml.


CONNECT
-------

Install the Python client with "pip install krpc==@KRPC_VERSION@", then:

    import krpc
    conn = krpc.connect()
    print(conn.ksa.active_vehicle.name)

The examples folder has scripts that print telemetry, launch the Rocket into orbit, and
plan and execute burns. The clients for C#, C++, C and Java compile against the stubs in
the stubs folder. Its README.md shows each language.


LINKS
-----

Documentation   https://krpc.github.io/krpc/experimental/ksa/
Source          https://github.com/krpc/krpc/tree/main/experimental/ksa
Bug reports     https://github.com/krpc/krpc/issues

kRPC for KSA is licensed under the LGPL v3, see LICENSE, COPYING and COPYING.LESSER. The
bundled Google.Protobuf and System.IO.Ports libraries have their own licenses, in
LICENSE.Google.Protobuf and LICENSE.System.IO.Ports.
