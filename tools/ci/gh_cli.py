"""Runs the GitHub CLI for the CI scripts, retrying a failed call."""

import json
import subprocess
import sys
import time

# The waits before each retry of a failed GitHub API call, in seconds
RETRY_DELAYS = (10, 30)
# The time limit on one call, in seconds
CALL_TIMEOUT = 60


def gh(*args, stdin=None, retry=True):
    """Run gh and return its standard output, retrying a failed call."""
    delays = RETRY_DELAYS if retry else ()
    for attempt in range(len(delays) + 1):
        try:
            return subprocess.run(
                ["gh", *args],
                input=stdin,
                capture_output=True,
                text=True,
                check=True,
                timeout=CALL_TIMEOUT,
            ).stdout
        except subprocess.CalledProcessError as error:
            print(f"gh {args[0]} {args[-1]} failed: {error.stderr}", file=sys.stderr)
            if attempt == len(delays):
                raise
        except subprocess.TimeoutExpired:
            print(f"gh {args[0]} {args[-1]} timed out", file=sys.stderr)
            if attempt == len(delays):
                raise
        time.sleep(delays[attempt])
    raise AssertionError("unreachable")


def gh_create(path, fields, api=gh):
    """Create an object with a POST to the GitHub API, and return the response."""
    # Not retried, as the object may exist after a lost response
    return api("api", path, "--input", "-", stdin=json.dumps(fields), retry=False)


def gh_lines(*args, api=gh):
    """Run a paginated gh api call whose --jq emits one JSON value per line."""
    return [json.loads(line) for line in api(*args).splitlines() if line]
