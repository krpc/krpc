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
