## tools/krpctools

- Testing against a live game
  - Add `krpc-game`, which installs kRPC, its test add-on and the requested mods into KSP, and
    runs the game (#1299)
  - Add `krpctools.game`, with a pytest plugin and test case classes for testing a mod against
    a running game (#1299)
