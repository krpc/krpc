"""Posts changelog hints on a pull request, as one comment.

A hint never fails the check: whether a change is user-facing needs a person to
judge. A failed GitHub API call does, after retries, so it can be re-run. Run by
the changelog-hints workflow from the base branch. Everything about the pull
request is fetched through the GitHub API as data, and no code from the pull
request runs.

Usage: changelog_hints.py --repo OWNER/NAME --pr NUMBER
"""

import argparse
import json
import re
import subprocess
import sys
import time
import urllib.parse
from dataclasses import dataclass, field
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "release"))

import changelog  # noqa: E402

MARKER = "<!-- changelog-hints -->"
CHECKED_LABEL = "changelog-checked"
BOT = "github-actions[bot]"

_HUNK_RE = re.compile(r"^@@ -\d+(?:,\d+)? \+(\d+)(?:,\d+)? @@")


@dataclass
class Hint:
    """One hint. ``needs_entry`` marks a hint that asks for an entry."""

    text: str
    needs_entry: bool = False


@dataclass
class File:
    """A file changed by the pull request, as the pull request files API
    lists it. ``patch`` is absent for a binary or very large diff."""

    filename: str
    status: str
    patch: str | None = None


@dataclass
class PullRequest:
    number: int
    body: str
    files: list[File]
    # The text of each fragment the pull request adds or changes, by path
    fragments: dict[str, str] = field(default_factory=dict)
    # The issues linked as closed by the pull request
    linked: set[int] = field(default_factory=set)
    labels: set[str] = field(default_factory=set)


# The directories that hold tests, at any depth in a component
TEST_DIRS = {"test", "tests"}
# The sources the clients' generated code comes from. A change to them changes
# each client's API with no change to the client's own files.
CLIENT_API_SOURCES = ("core/", "service/", "protobuf/", "tools/krpctools/")


def component_of(path):
    """The release component a changed file counts towards, or None. Tests and
    the changelog itself do not count."""
    for component in sorted(changelog.COMPONENTS, key=len, reverse=True):
        if path.startswith(component + "/"):
            rest = path[len(component) + 1 :]
            if TEST_DIRS & set(rest.split("/")[:-1]) or rest == "CHANGELOG.md":
                return None
            return component
    return None


def changes_client_api(files):
    """Whether the pull request changes the sources of the clients' generated code."""
    return any(
        f.filename.startswith(CLIENT_API_SOURCES)
        and not TEST_DIRS & set(f.filename.split("/")[:-1])
        for f in files
    )


def is_fragment(path):
    return path.startswith(changelog.FRAGMENT_DIR + "/")


def added_lines(file):
    """The line numbers the patch adds to the new file, or None for every line."""
    if file.status == "added" or file.patch is None:
        return None
    added = set()
    number = 0
    for line in file.patch.splitlines():
        match = _HUNK_RE.match(line)
        if match:
            number = int(match.group(1))
        elif line.startswith("+"):
            added.add(number)
            number += 1
        elif not line.startswith(("-", "\\")):
            number += 1
    return added


def entries(nodes):
    """The entries of a section, leaving out topic headings."""
    for node in nodes:
        if node.children:
            yield from entries(node.children)
        else:
            yield node


def changed(node, added):
    if added is None:
        return True
    return any(node.line + offset in added for offset in range(len(node.lines)))


def related_numbers(pr):
    numbers = {pr.number} | pr.linked
    numbers |= {int(number) for number in changelog.NUMBER_RE.findall(pr.body or "")}
    return numbers


def hints(pr):
    """The hints for a pull request, in a stable order."""
    if CHECKED_LABEL in pr.labels:
        return []
    out = []
    changed_components = sorted(
        {c for c in (component_of(f.filename) for f in pr.files) if c is not None}
    )
    fragment_files = [
        f for f in pr.files if is_fragment(f.filename) and f.status != "removed"
    ]
    removes_fragment = any(
        is_fragment(f.filename) and f.status == "removed" for f in pr.files
    )
    edits_changelog = sorted(
        f.filename
        for f in pr.files
        if f.filename.endswith("/CHANGELOG.md")
        and f.filename[: -len("/CHANGELOG.md")] in changelog.COMPONENTS
    )

    fragments = []
    unreadable = False
    for file in fragment_files:
        text = pr.fragments.get(file.filename)
        if text is None:
            continue
        try:
            changelog.check_name(file.filename)
            fragments.append((file, changelog.parse_fragment(text, file.filename)))
        except changelog.ChangelogError as error:
            unreadable = True
            out.append(
                Hint(
                    f"The changelog file `{file.filename}` has a mistake, which "
                    "needs fixing before the other changelog checks can run: "
                    f"{_code(_problem(error, file.filename))}"
                )
            )

    edits_changelog = edits_changelog if not removes_fragment else []
    if not fragment_files:
        # The CHANGELOG.md hint below covers a pull request that edits one
        if changed_components and not edits_changelog:
            out.append(
                Hint(
                    f"This pull request changes files in "
                    f"{_names(f'{c}/...' for c in changed_components)}, but has no "
                    "changelog entry yet. If users will notice the change, please "
                    f"add one in a new file in `{changelog.FRAGMENT_DIR}/`.",
                    needs_entry=True,
                )
            )
    elif not unreadable:
        # The sections this pull request adds to, by component
        sections = {}
        for file, fragment in fragments:
            added = added_lines(file)
            for component, nodes in fragment.sections.items():
                if any(changed(node, added) for node in entries(nodes)):
                    sections.setdefault(component, []).append(file.filename)
        for component in changed_components:
            if component not in sections:
                out.append(
                    Hint(
                        f"This pull request changes files in `{component}/...`, but "
                        f"adds no entry under a `## {component}` heading in "
                        f"`{changelog.FRAGMENT_DIR}/`. If users will notice the "
                        "change, please add one.",
                        needs_entry=True,
                    )
                )
        unchanged = set(sections) - set(changed_components)
        if changes_client_api(pr.files):
            unchanged = {c for c in unchanged if not c.startswith("client/")}
        for component in sorted(unchanged):
            out.append(
                Hint(
                    f"{_names(sections[component])} has an entry under "
                    f"`## {component}`, but this pull request doesn't change files "
                    f"in `{component}/...`. Should the entry go under a different "
                    "component?"
                )
            )

    related = related_numbers(pr)
    for file, fragment in [] if unreadable else fragments:
        added = added_lines(file)
        for nodes in fragment.sections.values():
            for node in entries(nodes):
                if not changed(node, added):
                    continue
                location = f"line {node.line} of `{file.filename}`"
                numbers = [int(n) for n in changelog.NUMBER_RE.findall(node.text)]
                if not numbers:
                    out.append(
                        Hint(
                            f"Please end the entry on {location} with the number of "
                            "the issue it fixes, or of this pull request, e.g. "
                            f"`(#{pr.number})`."
                        )
                    )
                for number in numbers:
                    if number not in related:
                        out.append(
                            Hint(
                                f"The entry on {location} refers to `#{number}`, "
                                "which this pull request doesn't link to. Is that "
                                "the right number? If it is, please link to it, "
                                "either in the description or as a linked issue."
                            )
                        )

    if edits_changelog:
        out.append(
            Hint(
                f"Changelog entries go in a new file in `{changelog.FRAGMENT_DIR}/`. "
                f"Please don't edit {_names(edits_changelog)} directly."
            )
        )
    return out


def _problem(error, path):
    """The message of a fragment error, with the path dropped."""
    message = str(error).removeprefix(f"{path}:")
    match = re.match(r"(\d+): (.*)", message, re.DOTALL)
    return f"line {match[1]}: {match[2]}" if match else message.strip()


def _names(items):
    """A list of names in code spans, joined in prose."""
    names = [f"`{item}`" for item in items]
    if len(names) == 1:
        return names[0]
    return ", ".join(names[:-1]) + " and " + names[-1]


def _code(text):
    """A code span holding text, which may itself hold backticks."""
    runs = [len(run) for run in re.findall(r"`+", text)]
    if not runs:
        return f"`{text}`"
    fence = "`" * (max(runs) + 1)
    return f"{fence} {text} {fence}"


def comment_body(found, repo, labels=()):
    """The hints comment. With no hints, it says the earlier hints are resolved."""
    if not found:
        if CHECKED_LABEL in labels:
            status = "A maintainer has checked the changelog of this pull request."
        else:
            status = "The changelog looks good now. Thanks!"
        return f"{MARKER}\n{status}"
    guide = f"https://github.com/{repo}/blob/main/CONTRIBUTING.md#changelog-entries"
    lines = [
        MARKER,
        "Thanks for your pull request! A few notes on the changelog:",
        "",
    ]
    lines.extend(f"- {hint.text}" for hint in found)
    lines.append("")
    footer = f"The contributing guide explains [how to add a changelog entry]({guide})."
    if any(hint.needs_entry for hint in found):
        footer += (
            " If the change needs no entry, a maintainer will add the "
            f"`{CHECKED_LABEL}` label."
        )
    lines.append(footer)
    return "\n".join(lines)


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


def gh_lines(*args):
    """Run a paginated gh api call whose --jq emits one JSON value per line."""
    return [json.loads(line) for line in gh(*args).splitlines() if line]


def fetch(repo, number):
    """Fetch a pull request and the text of the fragments it adds or changes."""
    data = json.loads(gh("api", f"repos/{repo}/pulls/{number}"))
    files = [
        File(f["filename"], f["status"], f.get("patch"))
        for f in gh_lines(
            "api", "--paginate", f"repos/{repo}/pulls/{number}/files", "--jq", ".[]"
        )
    ]
    owner, name = repo.split("/")
    query = (
        "query($owner: String!, $name: String!, $number: Int!) {"
        " repository(owner: $owner, name: $name) {"
        " pullRequest(number: $number) {"
        " closingIssuesReferences(first: 100) { nodes { number } } } } }"
    )
    linked = json.loads(
        gh(
            "api",
            "graphql",
            "-f",
            f"query={query}",
            "-F",
            f"owner={owner}",
            "-F",
            f"name={name}",
            "-F",
            f"number={number}",
            "--jq",
            "[.data.repository.pullRequest.closingIssuesReferences.nodes[].number]",
        )
    )
    fragments = {}
    for file in files:
        if is_fragment(file.filename) and file.status != "removed":
            fragments[file.filename] = gh(
                "api",
                "-H",
                "Accept: application/vnd.github.raw",
                f"repos/{repo}/contents/{urllib.parse.quote(file.filename)}"
                f"?ref={data['head']['sha']}",
            )
    return PullRequest(
        number=number,
        body=data.get("body") or "",
        files=files,
        fragments=fragments,
        linked=set(linked),
        labels={label["name"] for label in data["labels"]},
    )


def update_comment(repo, number, body, found):
    """Create or edit the hints comment. A pull request with no hints and no
    comment gets none. Runs that race can both create a comment, so the comments
    past the first are deleted."""
    existing = [
        comment["id"]
        for comment in gh_lines(
            "api",
            "--paginate",
            f"repos/{repo}/issues/{number}/comments",
            "--jq",
            ".[] | {id, body, login: .user.login}",
        )
        if comment["login"] == BOT and comment["body"].startswith(MARKER)
    ]
    for comment in existing[1:]:
        gh("api", "-X", "DELETE", f"repos/{repo}/issues/comments/{comment}")
    if not found and not existing:
        return
    body = json.dumps({"body": body})
    if existing:
        gh(
            "api",
            "-X",
            "PATCH",
            f"repos/{repo}/issues/comments/{existing[0]}",
            "--input",
            "-",
            stdin=body,
        )
    else:
        # Not retried, as a retry after a lost response would post a second comment
        gh(
            "api",
            f"repos/{repo}/issues/{number}/comments",
            "--input",
            "-",
            stdin=body,
            retry=False,
        )


def main():
    parser = argparse.ArgumentParser(
        description="Post changelog hints on a pull request"
    )
    parser.add_argument("--repo", required=True, help="OWNER/NAME")
    parser.add_argument("--pr", required=True, type=int, help="pull request number")
    args = parser.parse_args()
    pr = fetch(args.repo, args.pr)
    found = hints(pr)
    for hint in found:
        print(hint.text)
    update_comment(args.repo, args.pr, comment_body(found, args.repo, pr.labels), found)


if __name__ == "__main__":
    main()
