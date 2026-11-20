import json
import time
import urllib.request

SERVER = "http://127.0.0.1:50003"


def call(method, path, body=None):
    data = None if body is None else json.dumps(body).encode()
    request = urllib.request.Request(
        SERVER + path,
        data,
        {"Content-Type": "application/json"},
        method=method,
    )
    with urllib.request.urlopen(request) as response:
        return json.load(response).get("value")


def path(cls, obj):
    return f"/space-center/{cls}/{obj}"


vessel = path("vessel", call("GET", "/space-center/active-vessel"))
name = call("GET", vessel + "/name")
auto_pilot = path("auto-pilot", call("GET", vessel + "/auto-pilot"))
control = path("control", call("GET", vessel + "/control"))
flight = path("flight", call("POST", vessel + "/flight"))

call(
    "POST",
    auto_pilot + "/target-pitch-and-heading",
    {"pitch": 90, "heading": 90},
)
call("PUT", auto_pilot + "/engaged", {"value": True})
call("PUT", control + "/throttle", {"value": 1})
call("POST", control + "/activate-next-stage")
print(f"{name} has launched")

while call("GET", flight + "/mean-altitude") < 10000:
    time.sleep(0.1)

call("PUT", control + "/throttle", {"value": 0})
call("PUT", auto_pilot + "/engaged", {"value": False})
print(f"{name} reached 10 km")
