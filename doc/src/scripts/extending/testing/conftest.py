import os

from krpctools.game import register
from krpctools.game.ksp import KSP, Mod


class Heatshields(Mod):
    name = "Heatshields"
    folders = ("Heatshields",)

    def present(self, game, conn):
        return conn.testing_tools.part_available("heatshield.large")


build = os.path.join(os.path.dirname(__file__), "build")
register(KSP(mods=[Heatshields(os.path.join(build, "Heatshields.zip"))]))
