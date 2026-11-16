"""Publishing a mod version on SpaceDock.

A script logs in with the account's username and password, and the session
cookie from the login authorizes the calls after it.
The API has no way to delete a version, which only the site's mod page can do.

https://github.com/KSP-SpaceDock/SpaceDock/blob/master/api.md
"""

import http.cookiejar
import json
import urllib.error
import urllib.parse
import urllib.request
import uuid

import lib

SITE = "https://spacedock.info"

# SpaceDock asks API clients to identify themselves
USER_AGENT = "krpc-release-tools (https://github.com/krpc/krpc)"


def _multipart(fields, files):
    """The body and content type of a multipart/form-data request."""
    boundary = uuid.uuid4().hex
    parts = []
    for name, value in fields.items():
        parts += [
            f"--{boundary}\r\n".encode(),
            f'Content-Disposition: form-data; name="{name}"\r\n\r\n'.encode(),
            str(value).encode(),
            b"\r\n",
        ]
    for name, path in files.items():
        parts += [
            f"--{boundary}\r\n".encode(),
            f'Content-Disposition: form-data; name="{name}"; '
            f'filename="{path.name}"\r\n'.encode(),
            b"Content-Type: application/zip\r\n\r\n",
            path.read_bytes(),
            b"\r\n",
        ]
    parts.append(f"--{boundary}--\r\n".encode())
    return b"".join(parts), f"multipart/form-data; boundary={boundary}"


class Session:
    """A connection to SpaceDock, holding the login cookie."""

    def __init__(self):
        self._opener = urllib.request.build_opener(
            urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar())
        )

    def request(self, method, path, fields=None, files=None):
        """Call the API, returning the decoded JSON response.

        A POST sends its fields and files as a multipart form, as the API
        expects. SpaceDock reports a failure as an error status with a JSON
        body naming the reason.
        """
        headers = {"User-Agent": USER_AGENT, "Accept": "application/json"}
        body = None
        if method == "POST":
            body, headers["Content-Type"] = _multipart(fields or {}, files or {})
        request = urllib.request.Request(
            f"{SITE}{path}", data=body, method=method, headers=headers
        )
        try:
            with self._opener.open(request, timeout=600) as response:
                result = json.load(response)
        except json.JSONDecodeError as exception:
            raise lib.ReleaseError(
                f"the SpaceDock response to {method} {path} is not JSON"
            ) from exception
        except urllib.error.HTTPError as exception:
            detail = exception.read().decode(errors="replace").strip()
            try:
                detail = json.loads(detail).get("reason", detail)
            except (json.JSONDecodeError, AttributeError):
                pass
            raise lib.ReleaseError(
                f"SpaceDock rejected {method} {path} "
                f"({exception.code} {exception.reason}): {detail}"
            ) from exception
        except OSError as exception:
            raise lib.ReleaseError(
                f"could not reach SpaceDock: {exception}"
            ) from exception
        if isinstance(result, dict) and result.get("error"):
            raise lib.ReleaseError(
                f"SpaceDock rejected {method} {path}: {result.get('reason')}"
            )
        return result

    def login(self, username, password):
        """Log in. Publishes nothing, so it also serves as a credential check."""
        self.request("POST", "/api/login", {"username": username, "password": password})

    def versions(self, mod):
        """The versions of a mod, newest first."""
        return [
            version["friendly_version"]
            for version in self.request("GET", f"/api/mod/{mod}")["versions"]
        ]

    def game_versions(self, game):
        """The versions SpaceDock knows of a game, newest first."""
        return [
            version["friendly_version"]
            for version in self.request("GET", f"/api/{game}/versions")
        ]

    def upload(self, mod, version, game_version, changelog, archive):
        """Upload a new version of a mod, and notify its followers.

        The new version becomes the mod's default download. Returns the URL
        SpaceDock gives for it.
        """
        result = self.request(
            "POST",
            f"/api/mod/{mod}/update",
            {
                "version": version,
                "game-version": game_version,
                "changelog": changelog,
                "notify-followers": "yes",
            },
            {"zipball": archive},
        )
        return urllib.parse.urljoin(SITE, result["url"])
