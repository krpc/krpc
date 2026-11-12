" the KSP services "

def service(name, id, title, description, doc, deps = [], package = None):
    """An entry of SERVICES.

    Args:
      name: The service's name, as clients call it.
      id: The service's id, which the server and the clients encode in each call.
      title: The service's name in the changelog and the release notes.
      description: The description of the service's assembly.
      doc: The name of the service's API page, the template doc/api/<doc>.tmpl.
      deps: The services whose assemblies or types this service uses, or whose members its
        documentation links to. KRPC and StdLib are in the core, which every service uses.
      package: The service's directory under service/, where it differs from the name.

    Returns:
      The entry.
    """
    return struct(
        name = name,
        id = id,
        title = title,
        description = description,
        doc = doc,
        deps = deps,
        package = package or name,
    )

# The services of the core, which a service can name in its deps
CORE_SERVICES = ["KRPC", "StdLib"]

# The services, in the order the clients, the docs and the changelog list them
SERVICES = [
    service(
        name = "SpaceCenter",
        id = 2,
        title = "SpaceCenter",
        description = "Basic service for kRPC",
        doc = "space-center",
        deps = ["KRPC", "StdLib"],
    ),
    service(
        name = "Drawing",
        id = 3,
        title = "Drawing",
        description = "Drawing API for kRPC",
        doc = "drawing",
        deps = ["SpaceCenter", "StdLib", "UI"],
    ),
    service(
        name = "UI",
        id = 7,
        title = "User Interface",
        description = "User Interface API for kRPC",
        doc = "ui",
        deps = ["SpaceCenter", "StdLib"],
    ),
    service(
        name = "InfernalRobotics",
        id = 4,
        title = "InfernalRobotics",
        description = "Infernal Robotics API for kRPC",
        doc = "infernal-robotics",
        deps = ["SpaceCenter"],
    ),
    service(
        name = "KerbalAlarmClock",
        id = 5,
        title = "KerbalAlarmClock",
        description = "Kerbal Alarm Clock API for kRPC",
        doc = "kerbal-alarm-clock",
        deps = ["SpaceCenter"],
    ),
    service(
        name = "RemoteTech",
        id = 6,
        title = "RemoteTech",
        description = "RemoteTech API for kRPC",
        doc = "remote-tech",
        deps = ["SpaceCenter"],
    ),
    service(
        name = "LIDAR",
        id = 10,
        title = "LIDAR",
        description = "LiDAR API for kRPC",
        doc = "lidar",
        deps = ["SpaceCenter"],
        package = "LiDAR",
    ),
    service(
        name = "DockingCamera",
        id = 11,
        title = "DockingCamera",
        description = "DockingCamera API for kRPC",
        doc = "docking-camera",
        deps = ["SpaceCenter"],
    ),
    service(
        name = "Trajectories",
        id = 13,
        title = "Trajectories",
        description = "Trajectories API for kRPC",
        doc = "trajectories",
        deps = ["SpaceCenter"],
    ),
    service(
        name = "MechJeb",
        id = 14,
        title = "MechJeb",
        description = "MechJeb API for kRPC",
        doc = "mechjeb",
        deps = ["SpaceCenter"],
    ),
    service(
        name = "KerbalEngineerRedux",
        id = 15,
        title = "Kerbal Engineer Redux",
        description = "Kerbal Engineer Redux API for kRPC",
        doc = "kerbal-engineer-redux",
        deps = ["SpaceCenter"],
    ),
    service(
        name = "ScanSat",
        id = 16,
        title = "SCANsat",
        description = "SCANsat API for kRPC",
        doc = "scansat",
        deps = ["SpaceCenter"],
    ),
    service(
        name = "RasterPropMonitor",
        id = 17,
        title = "Raster Prop Monitor",
        description = "Raster Prop Monitor API for kRPC",
        doc = "raster-prop-monitor",
        deps = ["SpaceCenter", "StdLib"],
    ),
    service(
        name = "Debug",
        id = 12,
        title = "Debug",
        description = "Debug and cheat API for kRPC",
        doc = "debug",
        deps = ["SpaceCenter"],
    ),
]
