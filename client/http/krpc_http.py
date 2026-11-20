"""A minimal client for the kRPC HTTP server, built on the standard library.

Each call is one HTTP request. The server gives every procedure a path, from the
service definitions, and carries arguments and results as JSON.
"""

import http.client
import json
from typing import Any


class Error(Exception):
    """A call the server answered with an error."""

    def __init__(self, status: int, error: dict[str, str]) -> None:
        super().__init__("%d %s" % (status, error["description"]))
        self.status = status
        self.service = error["service"]
        self.name = error["name"]
        self.description = error["description"]


class Client:
    """Calls procedures over one keep-alive connection to the server."""

    def __init__(self, host: str = "127.0.0.1", port: int = 50003) -> None:
        self.connection = http.client.HTTPConnection(host, port, timeout=30)

    def close(self) -> None:
        self.connection.close()

    def request(
        self,
        method: str,
        path: str,
        body: Any = None,
        headers: dict[str, str] | None = None,
    ) -> tuple[int, dict[str, str], Any]:
        """Send a request, and return the status, headers and parsed body."""
        all_headers = dict(headers or {})
        data = None
        if body is not None:
            data = json.dumps(body).encode("utf-8")
            all_headers.setdefault("Content-Type", "application/json")
        self.connection.request(method, path, data, all_headers)
        response = self.connection.getresponse()
        content = json.loads(response.read().decode("utf-8"))
        return response.status, dict(response.getheaders()), content

    def call(self, method: str, path: str, body: Any = None) -> Any:
        """Call the procedure at a path, and return its value, or raise an Error."""
        status, _, content = self.request(method, path, body)
        if status != 200:
            raise Error(status, content["error"])
        return content.get("value")

    def get(self, path: str) -> Any:
        """Read a property."""
        return self.call("GET", path)

    def put(self, path: str, value: Any) -> None:
        """Set a property."""
        self.call("PUT", path, {"value": value})

    def post(self, path: str, **arguments: Any) -> Any:
        """Call a procedure or method, with its arguments by parameter name."""
        return self.call("POST", path, arguments or None)
