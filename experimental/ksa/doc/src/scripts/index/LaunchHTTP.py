import json
import time
import urllib.request

SERVER = "http://127.0.0.1:50003"


def call(method, route, body=None):
    data = None if body is None else json.dumps(body).encode()
    request = urllib.request.Request(
        SERVER + route,
        data,
        {"Content-Type": "application/json"},
        method=method,
    )
    with urllib.request.urlopen(request) as response:
        return json.load(response).get("value")


def path(class_name, obj):
    return f"/ksa/{class_name}/{obj}"


vehicle = path(
    "vehicle",
    call(
        "POST",
        "/ksa/launch-vehicle",
        {"name": "Rocket", "site": "CCSFS LC-39A"},
    ),
)
name = call("GET", vehicle + "/name")
auto_pilot = path("auto-pilot", call("GET", vehicle + "/auto-pilot"))
control = path("control", call("GET", vehicle + "/control"))
flight = path("flight", call("GET", vehicle + "/flight"))

call(
    "POST",
    auto_pilot + "/target-pitch-and-heading",
    {"pitch": 90, "heading": 90},
)
call("POST", auto_pilot + "/engage")
call("PUT", control + "/throttle", {"value": 1})
call("POST", control + "/activate-next-sequence")
call("PUT", control + "/engine-on", {"value": True})
print(f"{name} has launched")

while call("GET", flight + "/mean-altitude") < 10000:
    time.sleep(0.1)

call("PUT", control + "/engine-on", {"value": False})
call("POST", auto_pilot + "/disengage")
print(f"{name} reached 10 km")
