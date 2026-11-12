"""Running tests against a live game.

The core is game-agnostic:

 * :mod:`krpctools.game.game` - the ``Game`` and ``Mod`` base classes, and the game a run
   tests against.
 * :mod:`krpctools.game.launch` - launching a game, waiting for its server, and stopping it.
 * :mod:`krpctools.game.install` - installing kRPC, the test add-on and the mods.
 * :mod:`krpctools.game.releases` - finding a kRPC release on a game's release train.
 * :mod:`krpctools.game.testcase` - the ``TestCase`` base class.
 * :mod:`krpctools.game.assertions`, :mod:`krpctools.game.geometry` - assertions and vector
   math.
 * :mod:`krpctools.game.images` - checking and writing PNG and JPEG images.
 * :mod:`krpctools.game.pytest_plugin` - the pytest plugin, and :mod:`krpctools.game.cli`
   the krpc-game command.

:mod:`krpctools.game.ksp` extends it for Kerbal Space Program.
"""

from .game import Game, Mod, register

__all__ = ["Game", "Mod", "register"]
