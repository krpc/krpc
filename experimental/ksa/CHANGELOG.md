## [v0.3.0] - unreleased

- Mod
  - **Breaking:** Use the kRPC 0.8 protocol, so clients and client stubs need kRPC 0.8
  - **Breaking:** Rename the server settings in `kRPC/settings.toml`, with one port per server
  - **Breaking:** Keep the settings in `kRPC/settings.toml` in the game's user folder, so they
    apply to every save and are kept when the mod updates
  - Create the settings file with the defaults when it is missing, and replace settings in it
    that are not valid with their defaults
  - Run on Apple silicon Macs, including the gRPC and serial servers
  - Budget the server's time per frame, and choose the rate control settings from a preset in
    the server window
  - Name a server added in the server window "Server", "Server 2" and so on
  - Remember which servers are expanded in the server window
  - Show the number of connected clients beside each server in the server window
  - Add RTS/CTS flow control to serial servers, set in the server window
  - Add JSON over HTTP servers to the server window
  - Set the origins whose web pages may connect to a WebSockets or HTTP server in the server
    window
  - Fix serial servers failing to start on Linux and macOS, with the native serial port library
    not found
  - Support KSA v2026.11.16.5901
  - Move the kRPC menu to a `kRPC` item in the game's Mods menu, which opens the server window

- Crew and life support
  - Add life support: the supplies of a vehicle and its habitat zones, how long they last,
    and moving meals and canisters between habitats
  - Add `KSA.LifeSupportEnabled` and `KSA.RadiationEnabled`
  - Add the condition and cause of death of kittens, and whether a vehicle can be flown
  - **Breaking:** Move a kitten in `Seat.Kitten` only within its habitat zone, and refuse an
    incapacitated or retired kitten
  - Refuse `Kitten.GoOnEva` for a kitten too weak to go, or without supplies for its suit, and
    add `Kitten.CanGoOnEva` and `Kitten.CanBoard`
  - Add the life support of a kitten's suit, and taking and stowing supplies on EVA
  - Add `DockingPort.HatchOpen`

- Flight and orbits
  - **Breaking:** Return the scale height at sea level from `Body.ScaleHeight`
  - Add the air temperature, speed of sound, Mach number and breathable air to `Flight`
  - Add the temperature, speed of sound, oxygen fraction and molar mass of an atmosphere to
    `Body`

- Clients
  - Add stubs and documentation for the JavaScript client
  - Add a WebSockets server on port 50001 to the default settings, for the JavaScript client
- Release
  - Move the example scripts out of the release archive, into the tutorials in the
    documentation

## [v0.2.0]

- Mod
  - **Breaking:** Rename the mod folder and id from `kRPC.KSA` to `kRPC` (#1205)
  - Support KSA v2026.10.25.5707 (#1205)
  - Show a pop-up listing the RPCs found broken when KSA is not the version kRPC was built
    for (#1206, #1210)
  - Put the `kRPC` mod folder at the top of the release archive, so the Borea mod manager can
    install it (#1205)
  - Add a kRPC menu to the game's menu bar, with a server window and an info window that start
    and stop the server and edit its settings (#1206)
  - Reset the server settings to the defaults when the mod is updated (#1206)
  - Add a gRPC server, with a `.proto` file and an API reference for each service, to
    control the game from any language gRPC supports (#1222)
  - **Breaking:** Use `float` for values that do not need `double` precision, such as
    `Flight.Speed` and `Engine.Thrust` (#1214)

- Vehicles and parts
  - **Breaking:** Return the separated vehicles from `Decoupler.Decouple` and
    `DockingPort.Undock` (#1207)
  - Add the target part, debris, on-rails and structural load to `Vehicle` (#1207)
  - Add `Vehicle.Destroy` and `Vehicle.Recover` (#1213)
  - Add part velocities, and part lists by name and by module (#1207)
  - Add engine gimbals, propellant tanks, fuel links, and the minimum throttle and propellants
    of engines (#1210)
  - Add power, lights, deployable parts and magnetic scoops (#1211)
  - Add kittens, their radiation doses, crew seats and EVA (#1212)
  - **Breaking:** Return `null` from `KSA.ActiveVehicle` while the player controls a kitten on
    EVA (#1212)
  - Add `KSA.VehicleDesigns`, the designs `KSA.LaunchVehicle` can launch (#1213)
  - Fix `KSA.LaunchVehicle` failing in the game's placement code for an EVA kitten, which now
    throws an exception (#1212)

- Flight and orbits
  - Add manual rotation and translation inputs to `Control`, and pulsed thrust (#1207)
  - Add surface directions, the biome and radiation dose rates to `Flight` (#1207, #1211)
  - Add atmosphere, ocean, biome and position queries to `Body` (#1207)
  - Add shape, energy, node and closest approach members to `Orbit` (#1207)
  - Add the time to, position, reference frame and orbit average dose rate of a `Burn` (#1207,
    #1211)
  - Add relative and hybrid reference frames (#1207)
  - **Breaking:** `Burn.Orbit` and `Orbit.NextOrbit` follow the game's current prediction
    (#1209)
  - Fix an edited `Burn` getting a new object (#1209)

- Game
  - Add `KSA.Save`, `KSA.Load` and `KSA.SaveExists` (#1207)
  - Add the camera, screenshots and on-screen messages (#1213)
  - Add `KSA.Debug`, which teleports vehicles, sets their attitude, fills and empties their
    resources, and runs console commands (#1213)
  - Add drawing of lines, shapes and text in the scene (#1215)
  - Add user interface windows, with labels, buttons, toggles, input fields, sliders and
    dropdowns (#1215)
  - Raise `KRPC.ObjectDestroyedException` from a call through an object whose game object is
    gone (#1209)
  - Fix `KRPC.GameStateLoads` not counting save loads (#1209)

## [v0.1.0]

- Add the kRPC server and the `KSA` service for Kitten Space Agency, against the kRPC 0.7.0
  clients (#1190)
