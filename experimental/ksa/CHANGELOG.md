## [v0.3.0] - unreleased

- Mod
  - **Breaking:** Use the kRPC 0.8 protocol, so clients and client stubs need kRPC 0.8 (#1399)
  - **Breaking:** Rename the server settings in `kRPC/settings.toml`, with one port per server
    (#1399)
  - **Breaking:** Keep the settings in `kRPC/settings.toml` in the game's user folder, so they
    apply to every save and are kept when the mod updates (#1399)
  - Create the settings file with the defaults when it is missing, and replace settings in it
    that are not valid with their defaults (#1399)
  - Run on Apple silicon Macs, including the gRPC and serial servers (#1399)
  - Budget the server's time per frame, and choose the rate control settings from a preset in
    the server window (#1399)
  - Name a server added in the server window "Server", "Server 2" and so on (#1399)
  - Remember which servers are expanded in the server window (#1399)
  - Show the number of connected clients beside each server in the server window (#1399)
  - Add RTS/CTS flow control to serial servers, set in the server window (#1399)
  - Add JSON over HTTP servers to the server window (#1399)
  - Set the origins whose web pages may connect to a WebSockets or HTTP server in the server
    window (#1399)
  - Fix serial servers failing to start on Linux and macOS, with the native serial port library
    not found (#1399)
  - Support KSA v2026.11.16.5901 (#1401)
  - Move the kRPC menu to a `kRPC` item in the game's Mods menu, which opens the server window
    (#1400)
  - Fix every call failing with no vehicle under control, e.g. after the vehicle is destroyed
    (#1407)

- Crew and life support
  - Add life support: the supplies of a vehicle and its habitat zones, how long they last,
    and moving meals and canisters between habitats (#1400)
  - Add `KSA.LifeSupportEnabled` and `KSA.RadiationEnabled` (#1400)
  - Add the condition and cause of death of kittens, and whether a vehicle can be flown (#1400)
  - **Breaking:** Move a kitten in `Seat.Kitten` only within its habitat zone, and refuse an
    incapacitated or retired kitten (#1400)
  - Refuse `Kitten.GoOnEva` for a kitten too weak to go, or without supplies for its suit, and
    add `Kitten.CanGoOnEva` and `Kitten.CanBoard` (#1400)
  - Add the life support of a kitten's suit, and taking and stowing supplies on EVA (#1400)
  - Add `DockingPort.HatchOpen` (#1400)
  - Add `Vehicle.CrewEvents` and `NextCrewEvent`, each kitten's most urgent coming event, as
    the game's crew clock shows them (#1402)
  - Add the time until a kitten's condition worsens, it gets radiation sickness, or it takes a
    lethal dose (#1402)
  - Add the habitat zones linked through closed hatches, and the hatches to open (#1402)
  - Add `Vehicle.Mission`, the mission planned in the editor, with the times of its legs (#1402)
  - Add whether a habitat zone's supplies last the mission, and the kittens planned for each leg
    (#1402)
- Engines and mass properties
  - Add solid rocket motors to `Engine`, with their grain mass, and the chamber pressure of
    every engine (#1402)
  - Add the thrust and specific impulse of an engine at a given pressure, and its maximum
    thrust (#1402)
  - Fix `Engine.Thrust` and `Vehicle.Thrust` to include solid rocket motors, which fire
    whatever the throttle (#1402)
  - Add the dry mass, inertia, RCS torque and accelerations of a vehicle (#1402)
- Control and power
  - Add `Vehicle.ControlState`, the reason a vehicle cannot be flown (#1402)
  - Add the power switch of a probe core: its mode, storm standby and power status (#1402)
  - Add `Part.PowerForecast`, the charge, draw and time left of a part's electrical circuit
    (#1402)
  - Add the radiation dose, health and reboots of a probe core's avionics (#1402)

- Flight and orbits
  - **Breaking:** Return the scale height at sea level from `Body.ScaleHeight` (#1400)
  - Add the air temperature, speed of sound, Mach number and breathable air to `Flight` (#1400)
  - Add the temperature, speed of sound, oxygen fraction and molar mass of an atmosphere to
    `Body` (#1400)
  - Add the depth, submerged portion and buoyant force of a vehicle to `Flight` (#1402)
  - Add the aerodynamic force, torque, lift and drag on a vehicle to `Flight`, and the force at
    another position and velocity (#1402)
  - Add `Orbit.SpeedAt`, `MeanAnomalyAtUT`, `EccentricAnomalyAtUT`, `TrueAnomalyAtRadius`,
    `SemiLatusRectum` and `SpecificAngularMomentum` (#1402)
  - Add creating an orbit from its elements, or from a position and velocity (#1402)
  - Move a burn by setting `Burn.UT`, and add `Burn.BurnVector` and `RemainingBurnVector`
    (#1402)

- Parts
  - Add `KSA.RaycastDistance` and `KSA.RaycastPart`, to find the parts along a ray (#1402)
  - Add `Part.Highlight`, to highlight a part in one of the game's colors (#1402)

- Radiation and space weather
  - Add the radiation shielding of a part, its cover by the vehicle's tanks, and the dose rate
    behind them (#1402)
  - Add the solar wind and radiation storm at a vehicle, and the coronal mass ejections heading
    for it, with their forecast arrival (#1402)
  - Add the geomagnetic storm of a body, and the activity and recent coronal mass ejections of
    a star (#1402)

- kFS
  - Add a `KFS` service, to build, load, start and stop programs on kFS, the game's prototype
    flight software (#1403)
  - Add reading and writing a program's global variables, and its log and trace channels (#1403)
  - Refuse changes to `AutoPilot` while a kFS program controls the vehicle (#1403)

- Game
  - Stop a warp in progress when `KSA.SimulationSpeed` is set (#1402)

- Clients
  - Add stubs and documentation for the JavaScript client (#1399)
  - Add documentation for calling the server over HTTP, with its API reference (#1404)
  - Add a WebSockets server on port 50001 to the default settings, for the JavaScript client
    (#1399)
- Release
  - Move the example scripts out of the release archive, into the tutorials in the
    documentation (#1399)

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
