kRPC for KSA
============

Version @VERSION@. An experimental kRPC server for Kitten Space Agency. Scripts in Python, C#, C++, C,
Java and Lua control the game over a network connection.

Built for KSA @KSA_VERSION@, StarMap @STARMAP_VERSION@ and the kRPC @KRPC_VERSION@ clients.

Install
-------

1. Install StarMap @STARMAP_VERSION@, the code mod loader:
   https://github.com/StarMapLoader/StarMap/releases

2. Copy the kRPC.KSA folder into the KSA mods folder:
   <Documents>/My Games/Kitten Space Agency/mods/

3. Enable the mod in <Documents>/My Games/Kitten Space Agency/manifest.toml:

     [[mods]]
     id = "kRPC.KSA"
     enabled = true

4. Launch the game through StarMap.

The server starts when the game has loaded. By default it listens on 127.0.0.1, ports
50000 and 50001, and accepts every connection. The settings are in
kRPC.KSA/settings.toml.

Connect
-------

Install the Python client with "pip install krpc==@KRPC_VERSION@", then:

  import krpc
  conn = krpc.connect()
  print(conn.ksa.active_vehicle.name)

The documentation is in krpc-ksa-docs-@VERSION@.zip, and the client stubs for C#, C++,
C and Java are in krpc-ksa-stubs-@VERSION@.zip.
