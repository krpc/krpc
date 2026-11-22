"""Decides whether the daily workflow runs ci.

ci runs unless the newest daily run that passed ci tested the commit main is at,
and is younger than MAX_AGE. A failed or cancelled run does not count, so a
commit that is still red runs again the next day. MAX_AGE keeps the caches of
the daily jobs, which only daily runs touch, inside GitHub's 7 day expiry. A
failed lookup runs ci.

Usage: daily_due.py --repo OWNER/NAME --sha SHA
"""

import argparse
import datetime
import json
import os
import sys
from collections.abc import Callable
from dataclasses import dataclass

from gh_cli import gh, gh_lines

# The age of the last full daily run at which ci runs again. Below the 7 day
# expiry, as a scheduled run can start up to an hour late, or be dropped.
MAX_AGE = datetime.timedelta(days=5)
# The number of earlier daily runs searched for the last full run
MAX_RUNS = 30
# The job of ci that passes only once every other job has
GATE = "all-checks-passed"

Gh = Callable[..., str]


@dataclass
class Run:
    """A daily run that concluded success."""

    id: int
    sha: str
    created: datetime.datetime


def parse_time(value: str) -> datetime.datetime:
    """Parse a timestamp of the GitHub API."""
    return datetime.datetime.fromisoformat(value.replace("Z", "+00:00"))


def successful_runs(repo: str, api: Gh = gh) -> list[Run]:
    """The successful daily runs on main, newest first."""
    runs = json.loads(
        api(
            "run",
            "list",
            "--repo",
            repo,
            "--workflow",
            "daily.yml",
            "--branch",
            "main",
            "--status",
            "success",
            "--limit",
            str(MAX_RUNS),
            "--json",
            "databaseId,headSha,createdAt",
        )
    )
    return [
        Run(r["databaseId"], r["headSha"], parse_time(r["createdAt"])) for r in runs
    ]


def ran_ci(repo: str, run: Run, api: Gh = gh) -> bool:
    """Whether a daily run ran ci, which a run that skipped it did not."""
    # The jobs of a called workflow are named after the calling job, as in "ci / all-checks-passed"
    jobs = gh_lines(
        "api",
        "--paginate",
        f"repos/{repo}/actions/runs/{run.id}/jobs",
        "--jq",
        ".jobs[] | {name, conclusion}",
        api=api,
    )
    return any(
        job["name"].rpartition(" / ")[2] == GATE and job["conclusion"] == "success"
        for job in jobs
    )


def last_full_run(repo: str, api: Gh = gh) -> Run | None:
    """The newest daily run that ran ci and passed."""
    for run in successful_runs(repo, api):
        if ran_ci(repo, run, api):
            return run
    return None


def due(sha: str, now: datetime.datetime, last: Run | None) -> bool:
    """Whether ci runs, given the last full daily run."""
    return last is None or last.sha != sha or now - last.created >= MAX_AGE


def decide(repo: str, sha: str, now: datetime.datetime, api: Gh = gh) -> bool:
    """Whether ci runs, which it does when the lookup fails."""
    try:
        last = last_full_run(repo, api)
    except Exception as error:  # noqa: BLE001
        print(f"::warning::Could not find the last full daily run, so ci runs: {error}")
        return True
    if last is not None:
        print(f"last full daily run: {last.id}, at {last.sha}, created {last.created}")
    return due(sha, now, last)


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(
        description="Decides whether the daily workflow runs ci."
    )
    parser.add_argument("--repo", required=True)
    parser.add_argument("--sha", required=True)
    args = parser.parse_args(argv)
    result = decide(args.repo, args.sha, datetime.datetime.now(datetime.timezone.utc))
    print(f"ci runs: {result}")
    output = os.environ.get("GITHUB_OUTPUT")
    if output:
        with open(output, "a", encoding="utf-8") as f:
            f.write(f"due={str(result).lower()}\n")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
