"The platforms the TestServer is published for"

# Serial IO needs the System.IO.Ports build for the platform, so krpctools ships a TestServer
# for each of these runtime identifiers
RIDS = [
    "linux-x64",
    "win-x64",
]
