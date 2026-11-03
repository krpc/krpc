import json
import re
import tempfile
import unittest
from pathlib import Path

from flaky_tests import (
    MAX_COMMENT_LENGTH,
    MAX_FAILURES,
    MAX_FIELD_LENGTH,
    MAX_REPORT_BYTES,
    Failure,
    FlakyTarget,
    JobReport,
    annotations,
    comment_body,
    failure_lines,
    failures,
    flaky_targets,
    from_json,
    job_urls,
    notify,
    pull_request,
    read_reports,
    to_json,
)

ATTEMPT_XML = """\
<?xml version="1.0" encoding="utf-8"?>
<testsuites><testsuite name="pytest" errors="1" failures="2" skipped="1" tests="5">
<testcase classname="tests.test_calls" name="test_service_procedure" time="0.1" />
<testcase classname="tests.test_threading" name="test_close_from_an_end_callback[remove]">
<failure message="AssertionError: lost the race&#10;assert 1 == 2">trace</failure>
</testcase>
<testcase classname="tests.test_streams" name="test_rate[a - b]">
<error message="failed on setup with &quot;RuntimeError: setup&quot;">trace</error>
</testcase>
<testcase classname="tests.test_errors" name="test_setter_error">
<failure message="[XPASS(strict)] known" />
</testcase>
<testcase classname="tests.test_errors" name="test_known">
<skipped type="pytest.xfail" message="known" />
</testcase>
</testsuite></testsuites>
"""

# The XML Bazel writes for an attempt that wrote none, such as one that timed out
BAZEL_XML = """\
<testsuites><testsuite name="client/python/conformance-tcpip" tests="1" failures="0" errors="1">
<testcase name="client/python/conformance-tcpip" classname="" status="run">
<error message="exited with error code 142" />
</testcase></testsuite></testsuites>
"""


class TestFailures(unittest.TestCase):
    def test_failures_and_errors(self):
        self.assertEqual(
            [
                Failure(
                    "test_threading.py::test_close_from_an_end_callback[remove]",
                    "AssertionError: lost the race",
                ),
                Failure(
                    "test_streams.py::test_rate[a - b]",
                    'failed on setup with "RuntimeError: setup"',
                ),
                Failure("test_errors.py::test_setter_error", "[XPASS(strict)] known"),
            ],
            failures(ATTEMPT_XML),
        )

    def test_fields_stay_in_code_spans(self):
        xml = (
            '<testsuite><testcase classname="tests.test_x" name="test_y[a|b]">'
            '<failure message="expected `None` | got 1" /></testcase></testsuite>'
        )
        self.assertEqual(
            [Failure("test_x.py::test_y[a b]", "expected  None    got 1")],
            failures(xml),
        )

    def test_xml_from_bazel_names_no_test(self):
        self.assertEqual([], failures(BAZEL_XML))

    def test_invalid_xml(self):
        self.assertEqual([], failures("<testsuites>"))


def events(directory, label, status, attempt_xmls):
    """Bazel's build events for a target whose failed attempts wrote the XML."""
    lines = []
    for attempt, xml in enumerate(attempt_xmls, 1):
        # A colon in a Windows file name opens an alternate data stream
        name = re.sub(r"[^\w.-]", "_", label)
        path = Path(directory) / ("%s-attempt_%d.xml" % (name, attempt))
        path.write_text(xml)
        result = {
            "status": "FAILED",
            "testActionOutput": [
                {"name": "test.log", "uri": "file:///unused/test.log"},
                {"name": "test.xml", "uri": path.as_uri()},
            ],
        }
        event_id = {"label": label, "run": 1, "shard": 1, "attempt": attempt}
        lines.append({"id": {"testResult": event_id}, "testResult": result})
    passed = {"status": "PASSED", "testActionOutput": []}
    if status != "FAILED":
        event_id = {"label": label, "attempt": len(attempt_xmls) + 1}
        lines.append({"id": {"testResult": event_id}, "testResult": passed})
    summary_event = {"overallStatus": status, "attemptCount": len(attempt_xmls) + 1}
    lines.append(
        {"id": {"testSummary": {"label": label}}, "testSummary": summary_event}
    )
    lines.append({"id": {"progress": {}}, "progress": {}})
    return "".join(json.dumps(x) + "\n" for x in lines)


class TestFlakyTargets(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()

    def tearDown(self):
        self.directory.cleanup()

    def events(self, label, status, attempt_xmls):
        return events(self.directory.name, label, status, attempt_xmls)

    def test_flaky_target(self):
        found = flaky_targets(
            self.events(
                "//client/csharp:conformance-localsocket", "FLAKY", [ATTEMPT_XML]
            )
            + self.events("//client/java:conformance-tcpip", "PASSED", [])
        )
        self.assertEqual(
            ["//client/csharp:conformance-localsocket"], [x.label for x in found]
        )
        self.assertEqual(3, len(found[0].failures))

    def test_truncated_line(self):
        text = self.events("//client/lua:conformance-tcpip", "FLAKY", [ATTEMPT_XML])
        text += '{"id": {"testSummary": {"label": "//a:b"'
        self.assertEqual(
            ["//client/lua:conformance-tcpip"], [x.label for x in flaky_targets(text)]
        )

    def test_failed_target_is_not_flaky(self):
        text = self.events(
            "//client/java:conformance-tcpip", "FAILED", [ATTEMPT_XML, ATTEMPT_XML]
        )
        self.assertEqual([], flaky_targets(text))

    def test_attempt_that_named_no_test(self):
        found = flaky_targets(
            self.events("//client/python:conformance-tcpip", "FLAKY", [BAZEL_XML])
        )
        self.assertEqual([], found[0].failures)
        self.assertEqual(
            [
                "- `//client/python:conformance-tcpip`: no failed test in the "
                "attempt's XML"
            ],
            failure_lines(found),
        )

    def test_missing_attempt_xml(self):
        text = self.events("//client/lua:conformance-tcpip", "FLAKY", [ATTEMPT_XML])
        for path in Path(self.directory.name).glob("*.xml"):
            path.unlink()
        self.assertEqual([], flaky_targets(text)[0].failures)

    def test_failures_of_every_attempt(self):
        text = self.events(
            "//client/cpp:conformance-tcpip", "FLAKY", [ATTEMPT_XML, ATTEMPT_XML]
        )
        self.assertEqual(3, len(flaky_targets(text)[0].failures))


class TestReport(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        xml = (
            '<testsuite><testcase classname="tests.test_threading" '
            'name="test_race[remove]"><failure message="a: b, c" /></testcase></testsuite>'
        )
        self.found = flaky_targets(
            events(
                self.directory.name,
                "//client/csharp:conformance-localsocket",
                "FLAKY",
                [xml],
            )
        )

    def tearDown(self):
        self.directory.cleanup()

    def test_annotation_escapes_the_title(self):
        self.assertEqual(
            [
                "::warning title=Flaky test //client/csharp%3Aconformance-localsocket::"
                "test_threading.py::test_race[remove] failed and passed on a retry: a: b, c"
            ],
            annotations(self.found),
        )

    def test_failure_lines(self):
        self.assertEqual(
            [
                "- `test_threading.py::test_race[remove]` in "
                "`//client/csharp:conformance-localsocket`: `a: b, c`"
            ],
            failure_lines(self.found),
        )

    def test_failure_without_a_message(self):
        found = [FlakyTarget("//a:b", [Failure("test_x.py::test_y", "")])]
        self.assertEqual(["- `test_x.py::test_y` in `//a:b`"], failure_lines(found))


FLAKY = [
    FlakyTarget(
        "//client/csharp:conformance-localsocket",
        [Failure("test_threading.py::test_race[remove]", "AssertionError: lost")],
    )
]

MERGE_QUEUE_RUN = {
    "event": "merge_group",
    "head_branch": "gh-readonly-queue/main/pr-1252-d344631cc25d54d41bc449ca5f1f037a1d2f0fce",
    "head_sha": "abc",
    "html_url": "https://github.com/krpc/krpc/actions/runs/7",
}

FORK_RUN = {
    "event": "pull_request",
    "head_branch": "fix-streams",
    "head_sha": "def",
    "head_repository": {"owner": {"login": "contributor"}},
    "html_url": "https://github.com/krpc/krpc/actions/runs/8",
}


class TestArtifacts(unittest.TestCase):
    def test_round_trip(self):
        self.assertEqual(FLAKY, from_json(to_json(FLAKY)))

    def test_hostile_fields(self):
        text = json.dumps(
            [
                {
                    "label": "//a:b` @someone |x\n## heading",
                    "failures": [{"test": 5, "message": "m" * 1000}] * 100,
                },
                "not a target",
            ]
        )
        found = from_json(text)
        self.assertEqual(1, len(found))
        self.assertEqual("//a:b  @someone  x ## heading", found[0].label)
        self.assertEqual(MAX_FAILURES, len(found[0].failures))
        self.assertEqual("", found[0].failures[0].test)
        self.assertEqual(MAX_FIELD_LENGTH, len(found[0].failures[0].message))

    def test_hostile_fields_stay_in_code_spans(self):
        found = from_json(json.dumps([{"label": "`@someone|`", "failures": []}]))
        self.assertTrue(failure_lines(found)[0].startswith("- `@someone`:"))

    def test_not_a_list(self):
        self.assertEqual([], from_json('{"label": "//a:b"}'))

    def test_read_reports_skips_bad_files(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name, text in [
                ("flaky-tests-windows-test-client-csharp", to_json(FLAKY)),
                ("flaky-tests-linux-test-client-csharp", to_json(FLAKY)),
                ("flaky-tests-windows", "not json"),
                ("flaky-tests-big", " " * (MAX_REPORT_BYTES + 1)),
                ("flaky-tests-empty", "[]"),
            ]:
                (root / name).mkdir()
                (root / name / "flaky-tests.json").write_text(text)
            self.assertEqual(
                [
                    JobReport("linux-test-client-csharp", FLAKY),
                    JobReport("windows-test-client-csharp", FLAKY),
                ],
                read_reports(root),
            )


class FakeGh:
    """Answers the gh calls notify makes, and records them."""

    def __init__(self, run, artifacts, pulls=(), jobs=()):
        self.run = run
        self.artifacts = artifacts
        self.pulls = list(pulls)
        self.jobs = list(jobs)
        self.calls = []
        self.comments = []
        self.existing = []

    def __call__(self, *args, stdin=None, retry=True):
        self.calls.append(args)
        if args[0] == "run":
            directory = Path(args[args.index("--dir") + 1])
            for name in self.artifacts:
                (directory / name).mkdir()
                (directory / name / "flaky-tests.json").write_text(to_json(FLAKY))
            return ""
        path = next(x for x in args[1:] if x.startswith("repos/"))
        if path.endswith("/jobs"):
            return "\n".join(json.dumps(x) for x in self.jobs)
        if path.endswith("/artifacts"):
            return "\n".join([*self.artifacts, "docs-html"])
        if path.endswith("/comments") and "--paginate" in args:
            return "\n".join(json.dumps(x) for x in self.existing)
        if "/comments" in path:
            self.comments.append((path, json.loads(stdin or "{}")["body"], retry))
            return "{}"
        if path.endswith("/pulls"):
            return json.dumps(self.pulls)
        return json.dumps(self.run)


class TestPullRequest(unittest.TestCase):
    def test_merge_queue_run(self):
        self.assertEqual(
            1252, pull_request("krpc/krpc", MERGE_QUEUE_RUN, FakeGh({}, []))
        )

    def test_fork_run(self):
        pulls = [
            {"number": 1300, "head": {"sha": "old"}},
            {"number": 1301, "head": {"sha": "def"}},
        ]
        api = FakeGh({}, [], pulls)
        self.assertEqual(1301, pull_request("krpc/krpc", FORK_RUN, api))
        self.assertIn("head=contributor:fix-streams", api.calls[0])

    def test_same_repository_run(self):
        run = dict(FORK_RUN, pull_requests=[{"number": 1250}])
        api = FakeGh({}, [], [{"number": 1301, "state": "open", "head": {"sha": "x"}}])
        self.assertEqual(1250, pull_request("krpc/krpc", run, api))
        self.assertEqual([], api.calls)

    def test_head_moved_on(self):
        pulls = [
            {"number": 1302, "state": "closed", "head": {"sha": "new"}},
            {"number": 1301, "state": "open", "head": {"sha": "newer"}},
        ]
        api = FakeGh({}, [], pulls)
        self.assertEqual(1301, pull_request("krpc/krpc", FORK_RUN, api))

    def test_head_moved_on_and_closed(self):
        pulls = [{"number": 1301, "state": "closed", "head": {"sha": "new"}}]
        api = FakeGh({}, [], pulls)
        self.assertEqual(1301, pull_request("krpc/krpc", FORK_RUN, api))

    def test_no_pull_request(self):
        self.assertIsNone(pull_request("krpc/krpc", FORK_RUN, FakeGh({}, [], [])))

    def test_push_run(self):
        self.assertIsNone(pull_request("krpc/krpc", {"event": "push"}, FakeGh({}, [])))


JOBS = [
    {"name": "linux-test-client-csharp", "html_url": "https://example.com/job/1"},
    {"name": "windows-test-client-csharp", "html_url": "https://example.com/job/2"},
]

REPORTS = [
    JobReport("linux-test-client-csharp", FLAKY),
    JobReport("windows-test-client-csharp", FLAKY),
]


class TestNotify(unittest.TestCase):
    def test_body(self):
        urls = {job["name"]: job["html_url"] for job in JOBS}
        body = comment_body(REPORTS, "7", MERGE_QUEUE_RUN, urls)
        line = (
            "- `test_threading.py::test_race[remove]` in "
            "`//client/csharp:conformance-localsocket`: `AssertionError: lost`"
        )
        self.assertEqual(
            "<!-- flaky-tests run 7 -->\n"
            "@djungelorm the [merge queue run](https://github.com/krpc/krpc/actions/runs/7) "
            "passed these tests only on a retry. These flaky tests may be unrelated to "
            "this pull request.\n\n"
            "**[linux-test-client-csharp](https://example.com/job/1)**\n\n"
            + line
            + "\n\n**[windows-test-client-csharp](https://example.com/job/2)**\n\n"
            + line
            + "\n\n",
            body,
        )

    def test_unknown_job_is_not_a_link(self):
        body = comment_body(
            [JobReport("](https://evil.example)", FLAKY)], "7", MERGE_QUEUE_RUN, {}
        )
        self.assertIn("**`](https://evil.example)`**", body)

    def test_job_urls(self):
        api = FakeGh({}, [], jobs=JOBS)
        self.assertEqual(
            {
                "linux-test-client-csharp": "https://example.com/job/1",
                "windows-test-client-csharp": "https://example.com/job/2",
            },
            job_urls("krpc/krpc", "7", api),
        )

    def test_comments_on_the_pull_request(self):
        api = FakeGh(
            MERGE_QUEUE_RUN,
            [
                "flaky-tests-linux-test-client-csharp",
                "flaky-tests-windows-test-client-csharp",
            ],
            jobs=JOBS,
        )
        notify("krpc/krpc", "7", api)
        self.assertEqual(1, len(api.comments))
        path, body, retry = api.comments[0]
        self.assertEqual("repos/krpc/krpc/issues/1252/comments", path)
        self.assertIn("@djungelorm", body)
        self.assertIn("**[linux-test-client-csharp](https://example.com/job/1)**", body)
        self.assertIn(
            "**[windows-test-client-csharp](https://example.com/job/2)**", body
        )
        self.assertEqual(2, body.count("test_race[remove]"))
        self.assertFalse(retry)

    def test_long_body_is_cut(self):
        target = FlakyTarget(
            "//client/csharp:conformance-tcpip",
            [Failure("test_x.py::test_%d" % i, "x" * 150) for i in range(1000)],
        )
        reports = [JobReport("linux-test-client-csharp", [target])]
        body = comment_body(reports, "7", MERGE_QUEUE_RUN, {})
        self.assertLessEqual(len(body), MAX_COMMENT_LENGTH)
        self.assertTrue(body.endswith("more lines omitted.\n"))

    def test_rerun_edits_the_comment(self):
        api = FakeGh(MERGE_QUEUE_RUN, ["flaky-tests-linux-test-client-csharp"])
        api.existing = [
            {
                "id": 5,
                "body": "<!-- flaky-tests run 6 -->",
                "login": "github-actions[bot]",
            },
            {"id": 6, "body": "<!-- flaky-tests run 7 -->", "login": "someone"},
            {
                "id": 7,
                "body": "<!-- flaky-tests run 7 -->",
                "login": "github-actions[bot]",
            },
        ]
        notify("krpc/krpc", "7", api)
        self.assertEqual(1, len(api.comments))
        self.assertEqual("repos/krpc/krpc/issues/comments/7", api.comments[0][0])
        self.assertIn("PATCH", api.calls[-1])

    def test_no_flaky_artifacts(self):
        api = FakeGh(MERGE_QUEUE_RUN, [])
        notify("krpc/krpc", "7", api)
        self.assertEqual([], api.comments)
        self.assertEqual(1, len(api.calls))


if __name__ == "__main__":
    unittest.main()
