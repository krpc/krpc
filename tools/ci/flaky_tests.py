"""Reports the tests that failed and then passed when Bazel ran them again.

The ci config writes Bazel's build events to a JSON file. A target whose test
summary is FLAKY failed one or more attempts and then passed. The test result of
each failed attempt points to the JUnit XML it wrote, which names the tests that
failed. A flaky conformance test goes in the client's expected failures with the
@flaky marker.

report: run by each ci job after its tests. Annotates the run with each test that
failed in an earlier attempt of a flaky target, adds a table to the job summary,
and writes the flaky targets as JSON, which the job uploads as an artifact.

notify: run by the flaky-tests workflow when a ci run completes. Reads the
artifacts of the run and comments on its pull request, mentioning the maintainer
so the flakes reach them by email. A re-run of the same run edits its comment.
The artifacts may come from a fork, so their contents are treated as data.

Usage: flaky_tests.py report EVENTS [--json FILE]
       flaky_tests.py notify --repo OWNER/NAME --run ID
"""

import argparse
import json
import os
import re
import sys
import tempfile
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET
from collections.abc import Callable
from dataclasses import asdict, dataclass, field
from pathlib import Path

from gh_cli import gh, gh_create, gh_lines

MAINTAINER = "djungelorm"
BOT = "github-actions[bot]"
ARTIFACT_PREFIX = "flaky-tests-"

# The branch of a merge queue run, such as gh-readonly-queue/main/pr-1252-d344631c
_MERGE_QUEUE_BRANCH_RE = re.compile(r"(?:^|/)pr-(\d+)-[0-9a-f]+$")

# The limits on what an artifact contributes to a comment
MAX_REPORT_BYTES = 64 * 1024
MAX_FAILURES = 50
MAX_FIELD_LENGTH = 200
# The limit on a comment's body, below GitHub's 65536 characters
MAX_COMMENT_LENGTH = 60000


@dataclass
class Failure:
    """A test that failed in an attempt, by its id within its file."""

    test: str
    message: str


@dataclass
class FlakyTarget:
    """A test target that failed one or more attempts, then passed."""

    label: str
    failures: list[Failure] = field(default_factory=list)

    @property
    def protocol(self) -> str:
        """The protocol a conformance target tests over, the last part of its name."""
        return self.label.rpartition("-")[2]


def _field(value: object) -> str:
    """A string safe inside a markdown code span in a table cell."""
    text = value if isinstance(value, str) else ""
    text = re.sub(r"[`|\r\n]", " ", text).strip()
    return text[:MAX_FIELD_LENGTH]


def failures(text: str) -> list[Failure]:
    """The tests that failed, from the JUnit XML pytest wrote."""
    try:
        root = ET.fromstring(text)
    except ET.ParseError:
        return []
    found = []
    for case in root.iter("testcase"):
        # The XML Bazel writes for a run that wrote none has no classname
        module = case.get("classname", "").rpartition(".")[2]
        result = case.find("failure")
        if result is None:
            result = case.find("error")
        if not module or result is None:
            continue
        test = "%s.py::%s" % (module, case.get("name", ""))
        message = result.get("message", "").partition("\n")[0]
        found.append(Failure(_field(test), _field(message)))
    return found


def _path(uri: str) -> Path:
    """The local path of a file:// URI from a build event."""
    return Path(urllib.request.url2pathname(urllib.parse.urlparse(uri).path))


def flaky_targets(events: str) -> list[FlakyTarget]:
    """The flaky targets, from Bazel's build events as JSON, one event per line."""
    flaky = set()
    attempts: dict[str, list[tuple[int, str]]] = {}
    for line in events.splitlines():
        # Most events are of other kinds, so we skip them before decoding
        if '"testSummary"' not in line and '"testResult"' not in line:
            continue
        try:
            event = json.loads(line)
        except json.JSONDecodeError:
            # A Bazel killed mid-write leaves a truncated last line
            continue
        if "testSummary" in event["id"]:
            if event.get("testSummary", {}).get("overallStatus") == "FLAKY":
                flaky.add(event["id"]["testSummary"]["label"])
        elif "testResult" in event["id"]:
            result = event.get("testResult", {})
            if result.get("status") == "PASSED":
                continue
            for output in result.get("testActionOutput", []):
                if output.get("name") == "test.xml":
                    attempts.setdefault(event["id"]["testResult"]["label"], []).append(
                        (event["id"]["testResult"].get("attempt", 0), output["uri"])
                    )
    found = []
    for target_label in sorted(flaky):
        target = FlakyTarget(_field(target_label))
        # A test that failed more than one attempt is listed once
        seen = set()
        for _, uri in sorted(attempts.get(target_label, [])):
            try:
                text = _path(uri).read_text(encoding="utf-8", errors="replace")
            except OSError:
                continue
            for failure in failures(text):
                if failure.test not in seen:
                    seen.add(failure.test)
                    target.failures.append(failure)
        found.append(target)
    return found


def _escape_data(text: str) -> str:
    return text.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")


def _escape_property(text: str) -> str:
    return _escape_data(text).replace(":", "%3A").replace(",", "%2C")


def annotations(found: list[FlakyTarget]) -> list[str]:
    """The GitHub workflow commands that annotate the run with each flaky test."""
    lines = []
    for target in found:
        title = _escape_property("Flaky test %s" % target.label)
        if not target.failures:
            lines.append(
                "::warning title=%s::An attempt failed and a retry passed" % title
            )
        for failure in target.failures:
            text = "%s failed and passed on a retry: %s" % (
                failure.test,
                failure.message,
            )
            lines.append("::warning title=%s::%s" % (title, _escape_data(text)))
    return lines


def summary(found: list[FlakyTarget], max_length: int | None = None) -> str:
    """A markdown table of the flaky tests, with the line that marks each in the expected
    failures. Each field sits in a code span, so it holds no backtick, pipe or line
    break. Rows past max_length characters are dropped."""
    rows = [
        "| Target | Expected failures line |",
        "| --- | --- |",
    ]
    for target in found:
        if not target.failures:
            rows.append(
                "| `%s` | (no failed test in the attempt's XML) |" % target.label
            )
        for failure in target.failures:
            line = "%s@%s @flaky  # %s" % (
                failure.test,
                target.protocol,
                failure.message,
            )
            rows.append("| `%s` | `%s` |" % (target.label, line))
    if max_length is not None:
        length = 0
        for count, row in enumerate(rows):
            length += len(row) + 1
            if length > max_length:
                omitted = len(rows) - count
                rows = rows[:count] + ["", "%d more rows omitted." % omitted]
                break
    return "\n".join(rows) + "\n"


def to_json(found: list[FlakyTarget]) -> str:
    """The flaky targets as the JSON a job uploads."""
    return json.dumps([asdict(target) for target in found], indent=2)


def from_json(text: str) -> list[FlakyTarget]:
    """The flaky targets in a report from an artifact, which may come from a fork."""
    found = []
    data = json.loads(text)
    for item in data if isinstance(data, list) else []:
        if not isinstance(item, dict):
            continue
        target = FlakyTarget(_field(item.get("label")))
        items = item.get("failures")
        for failure in items if isinstance(items, list) else []:
            if isinstance(failure, dict):
                target.failures.append(
                    Failure(_field(failure.get("test")), _field(failure.get("message")))
                )
        target.failures = target.failures[:MAX_FAILURES]
        found.append(target)
    return found


def read_reports(directory: Path) -> list[FlakyTarget]:
    """The flaky targets in the downloaded artifacts, skipping an oversized or invalid
    report."""
    found = []
    for path in sorted(directory.glob("**/*.json")):
        if path.stat().st_size > MAX_REPORT_BYTES:
            print("Skipping %s, which is too large" % path, file=sys.stderr)
            continue
        try:
            found.extend(from_json(path.read_text(encoding="utf-8")))
        except (UnicodeDecodeError, json.JSONDecodeError):
            print("Skipping %s, which is not a report" % path, file=sys.stderr)
    return found


Gh = Callable[..., str]


def pull_request(repo: str, run: dict, api: Gh = gh) -> int | None:
    """The number of the pull request a ci run tested, or None."""
    branch = run.get("head_branch") or ""
    if run.get("event") == "merge_group":
        match = _MERGE_QUEUE_BRANCH_RE.search(branch)
        return int(match.group(1)) if match else None
    if run.get("event") != "pull_request":
        return None
    for pull in run.get("pull_requests") or []:
        return pull["number"]
    # The run's pull_requests list is empty for a fork, so we look the pull request up
    # by its head
    owner = ((run.get("head_repository") or {}).get("owner") or {}).get("login")
    pulls = json.loads(
        api(
            "api",
            "-X",
            "GET",
            "repos/%s/pulls" % repo,
            "-f",
            "state=all",
            "-f",
            "head=%s:%s" % (owner, branch),
        )
    )
    # The head may have moved on since the run, so we fall back to the open pull
    # request, then the newest
    for pull in pulls:
        if pull["head"]["sha"] == run.get("head_sha"):
            return pull["number"]
    for pull in pulls:
        if pull.get("state") == "open":
            return pull["number"]
    return pulls[0]["number"] if pulls else None


def marker(run_id: str) -> str:
    """The first line of the comment that reports a run, which finds it again."""
    return "<!-- flaky-tests run %s -->" % run_id


def comment_body(found: list[FlakyTarget], run_id: str, run: dict) -> str:
    """The comment that reports the flaky tests of a run."""
    kind = {"merge_group": "merge queue run", "pull_request": "pull request run"}.get(
        run.get("event", ""), "run"
    )
    header = (
        "%s\n@%s the [%s](%s) passed these tests only on a retry. These flaky tests "
        "may be unrelated to this pull request.\n\n"
        % (marker(run_id), MAINTAINER, kind, run.get("html_url", ""))
    )
    return header + summary(found, MAX_COMMENT_LENGTH - len(header))


def notify(repo: str, run_id: str, api: Gh = gh) -> None:
    """Comment on the pull request of a ci run with the flaky tests its jobs found."""
    names = api(
        "api",
        "repos/%s/actions/runs/%s/artifacts" % (repo, run_id),
        "--paginate",
        "--jq",
        ".artifacts[].name",
    ).split()
    if not any(name.startswith(ARTIFACT_PREFIX) for name in names):
        print("No flaky tests in run %s" % run_id)
        return
    with tempfile.TemporaryDirectory() as directory:
        api(
            "run",
            "download",
            run_id,
            "--repo",
            repo,
            "--pattern",
            ARTIFACT_PREFIX + "*",
            "--dir",
            directory,
        )
        found = read_reports(Path(directory))
    if not found:
        return
    run = json.loads(api("api", "repos/%s/actions/runs/%s" % (repo, run_id)))
    body = comment_body(found, run_id, run)
    _write_summary(body)
    number = pull_request(repo, run, api)
    if number is None:
        print(
            "Run %s tested no pull request, so there is nothing to comment on" % run_id
        )
        return
    path = "repos/%s/issues/%d/comments" % (repo, number)
    existing = [
        comment["id"]
        for comment in gh_lines(
            "api",
            "--paginate",
            path,
            "--jq",
            ".[] | {id, body, login: .user.login}",
            api=api,
        )
        if comment["login"] == BOT and comment["body"].startswith(marker(run_id))
    ]
    if existing:
        api(
            "api",
            "-X",
            "PATCH",
            "repos/%s/issues/comments/%s" % (repo, existing[0]),
            "--input",
            "-",
            stdin=json.dumps({"body": body}),
        )
        print("Updated the comment on #%d" % number)
        return
    gh_create(path, {"body": body}, api=api)
    print("Commented on #%d" % number)


def _write_summary(text: str) -> None:
    step_summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if step_summary:
        with open(step_summary, "a", encoding="utf-8") as fp:
            fp.write(text)


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Reports the tests that passed on a retry"
    )
    commands = parser.add_subparsers(dest="command", required=True)
    report = commands.add_parser("report", help="Report the flaky tests of a job")
    report.add_argument("events", type=Path, help="Bazel's build events, as JSON")
    report.add_argument("--json", type=Path, help="The file to write the report to")
    notify_parser = commands.add_parser(
        "notify", help="Comment on a run's pull request"
    )
    notify_parser.add_argument("--repo", required=True)
    notify_parser.add_argument("--run", required=True, help="The id of the ci run")
    args = parser.parse_args()
    if args.command == "notify":
        notify(args.repo, args.run)
        return 0
    if not args.events.exists():
        print("No build events in %s" % args.events)
        return 0
    found = flaky_targets(args.events.read_text(encoding="utf-8"))
    for line in annotations(found):
        print(line)
    if found:
        _write_summary(
            "### Flaky tests\n\nThese tests failed and then passed when Bazel ran them "
            "again.\n\n" + summary(found)
        )
        if args.json:
            args.json.write_text(to_json(found), encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
