"""Running kRPC's tests against a live game.

The core is game-agnostic:

 * :mod:`krpctest.game` - the ``Game`` and ``Mod`` base classes, and the game a run tests
   against.
 * :mod:`krpctest.launch` - launching a game, waiting for its server, and stopping it.
 * :mod:`krpctest.install` - installing kRPC, the test add-on and the mods.
 * :mod:`krpctest.testcase` - the ``TestCase`` base class.
 * :mod:`krpctest.assertions`, :mod:`krpctest.geometry` - assertions and vector math.

:mod:`krpctest.ksp` extends it for Kerbal Space Program. ``TestCase`` and ``Mixin`` here are
the KSP ones.
"""

from .game import Game, Mod
from .ksp import Mixin, TestCase

__all__ = ["Game", "Mixin", "Mod", "TestCase"]
