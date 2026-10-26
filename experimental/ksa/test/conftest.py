import ksatest


def pytest_addoption(parser):
    parser.addoption(
        "--no-launch",
        action="store_true",
        default=False,
        help="run against a game started separately, rather than launching one",
    )


def pytest_configure(config):
    ksatest.auto_launch = not config.getoption("--no-launch")


def pytest_sessionfinish(session):
    ksatest.stop_game()
