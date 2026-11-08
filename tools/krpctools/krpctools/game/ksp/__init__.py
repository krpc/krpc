"""Kerbal Space Program: the ``KSP`` game, its ``Mod`` base class and its ``TestCase``.

The supported third-party mods are in :mod:`krpctools.game.ksp.mods`.
"""

from .game import KSP, Mod
from .testcase import Mixin, TestCase

__all__ = ["KSP", "Mixin", "Mod", "TestCase"]
