" the KSP services, and the mods they wrap or support "

def service(name, id, title, description, doc, deps = [], mods = [], package = None):
    """An entry of SERVICES.

    Args:
      name: The service's name, as clients call it.
      id: The service's id, which the server and the clients encode in each call.
      title: The service's name in the changelog and the release notes.
      description: The description of the service's assembly.
      doc: The name of the service's API page, the template doc/api/<doc>.tmpl.
      deps: The services whose assemblies or types this service uses, or whose members its
        documentation links to. KRPC and StdLib are in the core, which every service uses.
      mods: The mods the service wraps, by their names in MODS.
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
        mods = mods,
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
        mods = ["InfernalRobotics"],
    ),
    service(
        name = "KerbalAlarmClock",
        id = 5,
        title = "KerbalAlarmClock",
        description = "Kerbal Alarm Clock API for kRPC",
        doc = "kerbal-alarm-clock",
        deps = ["SpaceCenter"],
        mods = ["KerbalAlarmClock"],
    ),
    service(
        name = "RemoteTech",
        id = 6,
        title = "RemoteTech",
        description = "RemoteTech API for kRPC",
        doc = "remote-tech",
        deps = ["SpaceCenter"],
        mods = ["RemoteTech"],
    ),
    service(
        name = "LIDAR",
        id = 10,
        title = "LIDAR",
        description = "LiDAR API for kRPC",
        doc = "lidar",
        deps = ["SpaceCenter"],
        mods = ["LiDAR"],
        package = "LiDAR",
    ),
    service(
        name = "DockingCamera",
        id = 11,
        title = "DockingCamera",
        description = "DockingCamera API for kRPC",
        doc = "docking-camera",
        deps = ["SpaceCenter"],
        mods = ["DockingCamera"],
    ),
    service(
        name = "Trajectories",
        id = 13,
        title = "Trajectories",
        description = "Trajectories API for kRPC",
        doc = "trajectories",
        deps = ["SpaceCenter"],
        mods = ["Trajectories"],
    ),
    service(
        name = "MechJeb",
        id = 14,
        title = "MechJeb",
        description = "MechJeb API for kRPC",
        doc = "mechjeb",
        deps = ["SpaceCenter"],
        mods = ["MechJeb"],
    ),
    service(
        name = "KerbalEngineerRedux",
        id = 15,
        title = "Kerbal Engineer Redux",
        description = "Kerbal Engineer Redux API for kRPC",
        doc = "kerbal-engineer-redux",
        deps = ["SpaceCenter"],
        mods = ["KerbalEngineerRedux"],
    ),
    service(
        name = "ScanSat",
        id = 16,
        title = "SCANsat",
        description = "SCANsat API for kRPC",
        doc = "scansat",
        deps = ["SpaceCenter"],
        mods = ["SCANsat"],
    ),
    service(
        name = "RasterPropMonitor",
        id = 17,
        title = "Raster Prop Monitor",
        description = "Raster Prop Monitor API for kRPC",
        doc = "raster-prop-monitor",
        deps = ["SpaceCenter", "StdLib"],
        mods = ["RasterPropMonitor"],
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

def mod(name, versions, repo = None, assembly = None, present = None):
    """An entry of MODS.

    Args:
      name: The mod's name, by which a test or another mod requires it.
      versions: The versions kRPC tests, latest first. Each is a version(). A mod with none has
        no pin, and the tests cannot install it.
      repo: The mod's GitHub repository, as owner/name.
      assembly: The name of the mod's main assembly, for each mod a service wraps or supports.
      present: How a test tells the mod loaded in the running game. A mod without one counts
        as loaded when a test requires it.

    Returns:
      The entry.
    """
    return struct(
        name = name,
        repo = repo,
        assembly = assembly,
        present = present,
        versions = [
            struct(
                version = v.version,
                url = "https://github.com/%s/%s" % (repo, v.source.format(version = v.version)) if v.source else None,
                sha256 = v.sha256,
                archive = v.archive,
                folders = v.folders,
                files = v.files,
                requires = v.requires,
                assembly_version = v.assembly_version,
            )
            for v in versions
        ],
    )

def version(
        version,
        source = None,
        sha256 = None,
        archive = None,
        folders = [],
        files = [],
        requires = [],
        assembly_version = None):
    """A version of a mod in MODS.

    Args:
      version: The release, as the mod numbers it.
      source: The archive's path in the mod's repository, in which {version} is the release.
      sha256: The archive's hash.
      archive: The mod whose archive of the same release holds this one, in place of a source.
      folders: The GameData folders the version installs.
      files: The files the version installs at the top of GameData.
      requires: The mods installed alongside it, as name or name==version.
      assembly_version: The major.minor version of the loaded assembly, for assembly_version().

    Returns:
      The version.
    """
    return struct(
        version = version,
        source = source,
        sha256 = sha256,
        archive = archive,
        folders = folders,
        files = files,
        requires = requires,
        assembly_version = assembly_version,
    )

def service_property(service, property = "available"):
    """A presence check: a property of a kRPC service, by their Python names.

    Args:
      service: The service.
      property: The property.

    Returns:
      The check.
    """
    return struct(kind = "service_property", service = service, property = property)

def part(name):
    """A presence check: a part in the part catalog.

    Args:
      name: The in-game part name, in which KSP replaces underscores with periods.

    Returns:
      The check.
    """
    return struct(kind = "part", name = name)

def part_module(name):
    """A presence check: a part module the game has loaded.

    Args:
      name: The part module's class name.

    Returns:
      The check.
    """
    return struct(kind = "part_module", name = name)

def assembly_version():
    """A presence check: the mod's assembly, loaded at the version's assembly_version.

    Returns:
      The check.
    """
    return struct(kind = "assembly_version")

# The third-party mods kRPC wraps or supports, which an in-game test can require by name.
# Overlays for their installs are in tools/krpctools/krpctools/game/ksp/mods/<name>/.
MODS = [
    # Dependencies, never requested on their own

    # Runtime patching, which most of the mods below reference. Its absence crashes kRPC's
    # assembly scanner at startup.
    mod(
        name = "Harmony",
        repo = "KSPModdingLibs/HarmonyKSP",
        versions = [version(
            "2.2.1.0",
            source = "releases/download/{version}/HarmonyKSP_{version}_for_KSP1.8%2B.zip",
            sha256 = "51d63f6739eaf22f12ce9456adace54385520c67cc13735d5300d5aa15c79476",
            folders = ["000_Harmony"],
        )],
    ),
    mod(
        name = "ClickThroughBlocker",
        repo = "linuxgurugamer/ClickThroughBlocker",
        versions = [version(
            "2.1.10.23",
            source = "releases/download/{version}/ClickThroughBlocker-1.12.5-{version}.zip",
            sha256 = "83bf6c3556a7d2c46634260100a535f1ccda99f83fc999c846b45a93c6369e3e",
            folders = ["000_ClickThroughBlocker"],
            requires = ["Harmony"],
        )],
    ),
    mod(
        name = "ToolbarControl",
        repo = "linuxgurugamer/ToolbarControl",
        versions = [version(
            "0.1.9.14",
            source = "releases/download/{version}/ToolbarControl-1.12.0-{version}.zip",
            sha256 = "0f44f5500f8e914346cb2e6dd5c5e5f26df9f26caa6ce8179738a3842aac2a5b",
            folders = ["001_ToolbarControl"],
            requires = ["ClickThroughBlocker"],
        )],
    ),
    # LinuxGuruGamer's shared library, whose KSP_Log AGExt references
    mod(
        name = "SpaceTuxLibrary",
        repo = "linuxgurugamer/SpaceTuxLibrary",
        versions = [version(
            "0.0.9",
            source = "releases/download/{version}/SpaceTuxLibrary-1.12.0-{version}.zip",
            sha256 = "e916b6907e8310e08baa49532d04f92068864cb38b93afd664bf712b190197f1",
            folders = ["SpaceTuxLibrary"],
        )],
    ),
    # Loads the body textures SCANsat's visual maps are drawn from
    mod(
        name = "KSPTextureLoader",
        repo = "Phantomical/KSPTextureLoader",
        versions = [version(
            "1.0.41",
            source = "releases/download/v{version}/KSPTextureLoader.zip",
            sha256 = "8d34d60c69e2c13e331d267c6eb14362c5dc9319eb013acbd67f59d6deabe382",
            folders = ["KSPTextureLoader"],
        )],
    ),
    # Lets several mods hook the flight integrator. KSP does not load FAR without it. It has no
    # release of its own, so it comes from the FAR archive.
    mod(
        name = "ModularFlightIntegrator",
        versions = [version(
            "0.16.1.2",
            archive = "FAR",
            folders = ["ModularFlightIntegrator"],
        )],
    ),
    # The Unity job-system assemblies FAR's voxelizer is built against, which KSP does not
    # ship. This is KSPBurst-Lite, the assemblies without the Burst compiler.
    mod(
        name = "KSPBurst",
        repo = "KSPModdingLibs/KSPBurst",
        versions = [version(
            "1.7.4.11",
            source = "releases/download/v{version}/KSPBurst_{version}.zip",
            sha256 = "6295c300c1a11b520b2037370f192a4fabe919ee1afb93bdb594e4499b298c45",
            folders = ["000_KSPBurst"],
            requires = ["Harmony"],
        )],
    ),
    # RealFuels' engine configs for stock parts. Without them no stock engine becomes a
    # ModuleEnginesRF. Frozen in CKAN.
    mod(
        name = "RealFuels-Stock",
        repo = "ValentinBischof/RealFuels-Stock",
        versions = [version(
            "5.1.0",
            source = "releases/download/v{version}/RealFuels-Stock-v{version}.zip",
            sha256 = "5ad5dde73fa84dcd1681e03385d9a4f9c6e65ea62248329c29e1038102412fac",
            folders = ["RealFuels-Stock"],
        )],
    ),
    # The base class of RealFuels' engine module
    mod(
        name = "SolverEngines",
        repo = "KSP-RO/SolverEngines",
        versions = [version(
            "3.14.0",
            source = "releases/download/v{version}/SolverEngines_v{version}.zip",
            sha256 = "360ab293f3de1974ec7ce99f5fd8e4a3e348b5b9a40877c0cab67c5ec45e83a3",
            folders = ["SolverEngines"],
        )],
    ),
    # The utility library of RealFuels and Procedural Fairings
    mod(
        name = "ROUtils",
        repo = "KSP-RO/ROUtils",
        versions = [version(
            "1.2.0.0",
            source = "releases/download/v{version}/ROUtils-v{version}.zip",
            sha256 = "3118abbe9cbf61cbaa6f395d3bf0786ed402a8eaa54833799869dd57fdc99d39",
            folders = ["ROUtils"],
            requires = ["KSPCommunityFixes", "SerializationFix"],
        )],
    ),
    # Serializes ROUtils' curves
    mod(
        name = "SerializationFix",
        repo = "Phantomical/KSPSerializationFix",
        versions = [version(
            "1.0.1",
            source = "releases/download/v{version}/KSPSerializationFix.zip",
            sha256 = "8232add723a443545c729b5d48e36806ed90d70576030e29b2f7436dac2c9445",
            folders = ["000_SerializationFix"],
        )],
    ),
    mod(
        name = "KSPCommunityFixes",
        repo = "KSPModdingLibs/KSPCommunityFixes",
        versions = [version(
            "1.41.1",
            source = "releases/download/{version}/KSPCommunityFixes_{version}.zip",
            sha256 = "3b63c37d7da9ec0c93063374df1c7a365b02fa66ad6b033c7a2b6ae6ff18215e",
            folders = ["KSPCommunityFixes"],
            requires = ["Harmony"],
        )],
    ),
    # The resource definitions RealFuels' configs are written against
    mod(
        name = "CommunityResourcePack",
        repo = "UmbraSpaceIndustries/CommunityResourcePack",
        versions = [version(
            "112.0.1",
            source = "releases/download/v{version}/CommunityResourcePack_{version}.zip",
            sha256 = "e6c264137c391bd96b2cdd996b163ec4d7f200eccee0b2555edaf8bae19db2ed",
            folders = ["CommunityResourcePack"],
        )],
    ),

    # The mods the services wrap
    # No version is pinned, as the LIDAR service has no in-game tests
    mod(
        name = "LiDAR",
        assembly = "LiDAR",
        versions = [],
    ),
    mod(
        name = "RemoteTech",
        repo = "RemoteTechnologiesGroup/RemoteTech",
        assembly = "RemoteTech",
        present = service_property("remote_tech"),
        versions = [version(
            "1.9.12",
            source = "releases/download/{version}/RemoteTech-{version}.zip",
            sha256 = "cd425571cedd4256444aaa23dcbd5f2bf581ca61f132ec2f04643cefe92f65f4",
            # The archive also bundles an older ModuleManager, which is left out
            folders = ["RemoteTech"],
        )],
    ),
    mod(
        name = "InfernalRobotics",
        repo = "meirumeiru/InfernalRobotics",
        assembly = "InfernalRoboticsNext",
        present = service_property("infernal_robotics"),
        versions = [version(
            "3.1.22",
            source = "releases/download/v{version}/InfernalRobotics_v{version}.zip",
            sha256 = "7e5819e06909c86e7cc117fb89582727ee9b8dfe169882e12835fa1929c59052",
            folders = ["MagicSmokeIndustries"],
        )],
    ),
    # The maintained fork
    mod(
        name = "KerbalAlarmClock",
        repo = "linuxgurugamer/KerbalAlarmClock",
        assembly = "KerbalAlarmClock",
        present = service_property("kerbal_alarm_clock"),
        versions = [version(
            "3.14.2.7",
            source = "releases/download/{version}/KerbalAlarmClock-1.12.5-{version}.zip",
            sha256 = "cdc06631ce478de4656d309ed04df08a2ebeb19e1d3b7fe79b221e38c1e04410",
            folders = ["TriggerTech"],
            requires = ["ToolbarControl"],
        )],
    ),
    # Docking Camera (KURS)
    mod(
        name = "DockingCamera",
        repo = "linuxgurugamer/DockingCam",
        assembly = "DockingCamKURS",
        present = service_property("docking_camera"),
        versions = [version(
            "1.3.9",
            source = "releases/download/{version}/DockingCamera-1.12.0-{version}.zip",
            sha256 = "b2f6d153edc65d0e39049c2d1da8ad818d51705d66e1c1413f9a3f0520974413",
            folders = ["DockingCamKURS"],
            requires = ["ToolbarControl"],
        )],
    ),
    # The MechJeb service is for the flight scene only, so we find the mod by MechJebCore, the
    # part module it adds to every command part
    mod(
        name = "MechJeb",
        repo = "MuMech/MechJeb2",
        assembly = "MechJeb2",
        present = part_module("MechJebCore"),
        versions = [version(
            "2.15.4.0",
            source = "releases/download/{version}/MechJeb2-{version}.zip",
            sha256 = "658c30132b00f4df972b1d39ecd3e046749ebb7544a619a15620becaf5aa1c86",
            folders = ["MechJeb2"],
        )],
    ),
    mod(
        name = "KerbalEngineerRedux",
        repo = "jrbudda/KerbalEngineer",
        assembly = "KerbalEngineer",
        present = service_property("kerbal_engineer_redux"),
        versions = [version(
            "1.1.9.5",
            source = "releases/download/{version}/KerbalEngineer-{version}.zip",
            sha256 = "821e5b8a4af1c4af87b1e6aadd02ee9662be78ab3343844f9711c324aa3c2a3a",
            folders = ["KerbalEngineer"],
        )],
    ),
    mod(
        name = "SCANsat",
        repo = "KSPModStewards/SCANsat",
        assembly = "SCANsat",
        present = service_property("scan_sat"),
        versions = [version(
            "22.1",
            source = "releases/download/v{version}/SCANsat-{version}.zip",
            sha256 = "34cabb87618d61934fcc4dd6332294f62cf183ba3828e092c07d67608821bd4a",
            folders = ["SCANsat"],
            requires = ["KSPTextureLoader"],
        )],
    ),
    # The full package, whose patches give the stock pods RPM IVAs. Its patches need
    # ModuleManager, which kRPC installs.
    mod(
        name = "RasterPropMonitor",
        repo = "FirstPersonKSP/RasterPropMonitor",
        assembly = "RasterPropMonitor",
        present = service_property("raster_prop_monitor"),
        versions = [version(
            "1.1.0.1",
            source = "releases/download/{version}/RasterPropMonitor-{version}.zip",
            sha256 = "a1f3b414e11afa9fef7f132c7ddaa45e2fe84f35fa5db93dbcc65b2fade8c897",
            folders = ["JSI"],
        )],
    ),
    # Trajectory predictions accounting for atmospheric drag and lift
    mod(
        name = "Trajectories",
        repo = "linuxgurugamer/KSPTrajectories",
        assembly = "Trajectories",
        versions = [version(
            "2.4.5.5",
            source = "releases/download/{version}/Trajectories-1.12.5-{version}.zip",
            sha256 = "92d9670da884e40608193200a876bafcf70ea59869b5bc1b3fbf31d541313b16",
            folders = ["Trajectories"],
            requires = ["ClickThroughBlocker", "ToolbarControl", "SpaceTuxLibrary"],
        )],
    ),

    # The mods SpaceCenter supports

    # Wrapped by the Parachute class. Its part modules do not register without
    # ClickThroughBlocker and ToolbarControl.
    mod(
        name = "RealChute",
        repo = "ChrisViral/RealChute",
        assembly = "RealChute",
        present = part("RC.stack"),
        versions = [version(
            "1.4.9.5",
            source = "releases/download/v{version}/RealChute_v{version}.zip",
            sha256 = "3b135ae03d2214d4721a655e2d63e7c93c9a69558d8eae2473fced2bbf92bdf0",
            folders = ["RealChute"],
            requires = ["ToolbarControl"],
        )],
    ),
    # DMagic's science part module, wrapped by the Experiment class. The archive is the part
    # module alone, and the overlay adds a part that uses it.
    mod(
        name = "DMagic",
        repo = "DMagic1/DMModuleScienceAnimateGeneric",
        assembly = "DMModuleScienceAnimateGeneric",
        present = part("dmagicSensorTest"),
        versions = [version(
            "0.23",
            source = "releases/download/v{version}/DMModuleScienceAnimateGeneric_v{version}.zip",
            sha256 = "a4821a1d76ddf0757b57a383bd4c76c153851159d533f7bcf31f1b74401d9b60",
            folders = ["DMagicScienceAnimate"],
        )],
    ),
    # Action Groups Extended, wrapped by the Control class. It adds no part, and patches a
    # ModuleAGX module onto every part.
    mod(
        name = "AGExt",
        repo = "linuxgurugamer/AGExt",
        assembly = "AGExt",
        present = part_module("ModuleAGX"),
        versions = [version(
            "2.4.1.4",
            source = "releases/download/{version}/AGExt-1.12.0-{version}.zip",
            sha256 = "fa5f56288165321775c96674a1b64e49c353ccfec57fd4d7979afb1c1005f7aa",
            folders = ["Diazo"],
            requires = ["ToolbarControl", "SpaceTuxLibrary"],
        )],
    ),
    # Ferram Aerospace Research, which replaces the stock aerodynamics
    mod(
        name = "FAR",
        repo = "KSPModStewards/Ferram-Aerospace-Research",
        assembly = "FerramAerospaceResearch",
        present = service_property("space_center", "far_available"),
        versions = [version(
            "0.16.1.2",
            source = "releases/download/v{version}_Marangoni/FAR_0_16_1_2_Marangoni.zip",
            sha256 = "422c6b404e7d050844f9b41218205d0bb85aed1a6e66941019d55b0ed0a30e8e",
            folders = ["FerramAerospaceResearch"],
            requires = ["ModularFlightIntegrator", "KSPBurst", "Harmony"],
        )],
    ),
    # Real propellants, limited ignitions, ullage and pressure feeding
    mod(
        name = "RealFuels",
        repo = "KSP-RO/RealFuels",
        assembly = "RealFuels",
        present = service_property("space_center", "real_fuels_available"),
        versions = [version(
            "15.16.1.0",
            source = "releases/download/v{version}/RealFuels_v{version}.zip",
            sha256 = "5e14f5bd87111380025a3bc93e4e3f84cbe9c9d19c1dd8c0d3212c12ab11cc1e",
            folders = ["RealFuels"],
            requires = [
                "RealFuels-Stock",
                "SolverEngines",
                "ROUtils",
                "CommunityResourcePack",
            ],
        )],
    ),
    # Replaces CommNet's range model with a link budget
    mod(
        name = "RealAntennas",
        repo = "KSP-RO/RealAntennas",
        assembly = "RealAntennas",
        present = service_property("space_center", "real_antennas_available"),
        versions = [version(
            "2.12.0.0",
            source = "releases/download/v{version}/RealAntennas-v{version}.zip",
            sha256 = "2630574a64131c9990d0613aac75d9458b5e2172d3e4c502287841f90e358e51",
            folders = ["RealAntennas"],
            requires = ["ToolbarControl", "KSPCommunityFixes", "KSPBurst"],
        )],
    ),
    # Switches part subtypes, which changes their resources and mass. The archive is the part
    # module alone, and the overlay adds a part that uses it.
    mod(
        name = "B9PartSwitch",
        repo = "blowfishpro/B9PartSwitch",
        assembly = "B9PartSwitch",
        present = part("b9PartSwitchTest"),
        versions = [version(
            "2.20.0",
            source = "releases/download/v{version}/B9PartSwitch_v{version}.zip",
            sha256 = "ebf4c01f37a92f3ec0d74a9c653fae707298cf22ab0484c18a0e5670783a2121",
            folders = ["B9PartSwitch"],
        )],
    ),
    # Near Future Solar, whose ModuleCurvedSolarPanel the SolarPanel class wraps
    mod(
        name = "NearFutureSolar",
        repo = "post-kerbin-mining-corporation/NearFutureSolar",
        assembly = "NearFutureSolar",
        present = part("nfs-panel-deploying-curved-25-1"),
        versions = [version(
            "1.3.3",
            source = "releases/download/{version}/NearFutureSolar_1_3_3.zip",
            sha256 = "4dab8ed0b69bad969a75e89668a95ddc912b7618c7de5f6e87fe570adba6c4df",
            # The archive also bundles B9PartSwitch and ModuleManager, installed on their own
            folders = ["NearFutureSolar"],
            requires = ["B9PartSwitch"],
        )],
    ),
    # Procedural Fairings, whose fairing sides the Fairing class wraps. Its parts load without
    # its plugin, so the check looks for the plugin's module.
    mod(
        name = "ProceduralFairings",
        repo = "KSP-RO/ProceduralFairings",
        assembly = "ProceduralFairings",
        present = part_module("ProceduralFairingDecoupler"),
        versions = [version(
            "6.8.0.0",
            source = "releases/download/v{version}/ProceduralFairings-v{version}.zip",
            sha256 = "bd574414e01ebecb6fd056bb7c62a7de7cdae4160c1550c98931128890918400",
            folders = ["ProceduralFairings"],
            requires = ["ROUtils", "KSPCommunityFixes"],
        )],
    ),
    # Firespitter, whose FSswitchEngineThrustTransform module is one of the engine thrust
    # reversers the Engine class drives
    mod(
        name = "Firespitter",
        repo = "snjo/Firespitter",
        assembly = "Firespitter",
        present = part("FSnoseEngine"),
        versions = [version(
            "7.17",
            source = "releases/download/v{version}/Firespitter_{version}.zip",
            sha256 = "ce19d8abc5ef3343752146644f4f8baa25daae4f38124541399590d4156e87fc",
            folders = ["Firespitter"],
        )],
    ),
    # KerbalActuators, whose WBIPropSpinner module is another engine thrust reverser. The
    # archive is the part module alone, and the overlay adds the parts that use it.
    mod(
        name = "KerbalActuators",
        repo = "Angel-125/KerbalActuators",
        assembly = "KerbalActuators",
        present = part("kerbalActuatorsReverserTest"),
        versions = [version(
            "1.9.0",
            source = "releases/download/v{version}/KerbalActuators.zip",
            sha256 = "ff5ab1d099158440a5259da56b8e31d778a977c5469be3f3defca2439a6b7ff7",
            # The archive also bundles ModuleManager, installed on its own
            folders = ["WildBlueIndustries"],
        )],
    ),
    # Provides ModuleNameTag, which Part.Tag uses when kOS is not installed
    mod(
        name = "KSPCommunityPartModules",
        repo = "KSPModdingLibs/KSPCommunityPartModules",
        assembly = "KSPCommunityPartModules",
        present = assembly_version(),
        versions = [
            version(
                "0.5.1",
                source = "releases/download/v{version}/KSPCommunityPartModules-v{version}.zip",
                sha256 = "609fc54396af08399a37f9126f3839e898e5cfed688a49e0cdb3c5ee52dba294",
                folders = ["KSPCommunityPartModules"],
                requires = ["Harmony"],
                assembly_version = "0.5",
            ),
            # Predates ModuleNameTag, to test that kRPC still loads with it
            version(
                "0.4.0",
                source = "releases/download/v{version}/KSPCommunityPartModules-v{version}.zip",
                sha256 = "bb1f98cdbc4e641bc21fa37b7859ddae76d93b1b14e700766a29ca6e7de3c809",
                folders = ["KSPCommunityPartModules"],
                requires = ["Harmony"],
                assembly_version = "0.4",
            ),
        ],
    ),
    # kOS, whose KOSNameTag part module Part.Tag shares
    mod(
        name = "kOS",
        repo = "KSP-KOS/KOS",
        assembly = "kOS",
        present = part("kOSMachine1m"),
        versions = [version(
            "1.6.0.1",
            source = "releases/download/{version}/KOS-{version}.zip",
            sha256 = "d61eb36652ff94e793e280d5afb49b22abe70451638ed0ac6d2850483d8a3574",
            folders = ["kOS"],
        )],
    ),
    # TweakScale Rescaled, which scales parts and the stats of their modules
    mod(
        name = "TweakScaleRescaled",
        repo = "JonnyOThan/TweakScale",
        assembly = "Scale",
        present = assembly_version(),
        versions = [version(
            "3.3.2.1",
            source = "releases/download/{version}/TweakScale-{version}.zip",
            sha256 = "669199d6ec88eab5335ee96fc0066c6ac035edc332e079c627c636078847d38d",
            folders = ["TweakScale"],
            files = ["999_Scale_Redist.dll"],
            requires = ["Harmony"],
            assembly_version = "3.3",
        )],
    ),
]
