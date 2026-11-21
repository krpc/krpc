import os

from krpctools.game import game


def pytest_report_header():
    # The real path of the install names the game version
    try:
        return f"KSA: {os.path.realpath(game.current().game_dir())}"
    except RuntimeError:
        return None
