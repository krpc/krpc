" the KSP services, and the mods they wrap or support "

def service(name, id, title, description, doc, deps = [], mods = [], supports = [], package = None):
    """An entry of SERVICES.

    Args:
      name: The service's name, as clients call it.
      id: The service's id, which the server and the clients encode in each call.
      title: The service's name in the changelog and the release notes.
      description: The description of the service's assembly.
      doc: The name of the service's API page, the template doc/api/<doc>.tmpl.
      deps: The services whose assemblies or types this service uses, or whose members its
        documentation links to. KRPC and StdLib are in the core, which every service uses.
      mods: The mods the service wraps, by their names in MODS. Its Available property reports
        whether the mod is installed.
      supports: The mods the service supports without wrapping them, by their names in MODS. Its
        <mod>Available property reports whether each one is installed.
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
        supports = supports,
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
        supports = [
            "FAR",
            "AGExt",
            "RealFuels",
            "RealChute",
            "ProceduralFairings",
            "NearFutureSolar",
            "DMagic",
            "Firespitter",
            "KerbalActuators",
            "KSPCommunityPartModules",
            "kOS",
            "B9PartSwitch",
            "TweakScaleRescaled",
            "RealAntennas",
        ],
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
        deps = ["SpaceCenter", "StdLib"],
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
        deps = ["SpaceCenter", "StdLib"],
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

def mod(name, versions, title = None, repo = None, assembly = None):
    """An entry of MODS.

    Args:
      name: The mod's name, by which a test or another mod requires it.
      title: The mod's name in the docs, where it differs from the name.
      versions: The versions kRPC tests, latest first. Each is a version(). A mod with none has
        no pin, and the tests cannot install it.
      repo: The mod's GitHub repository, as owner/name.
      assembly: The name of the mod's main assembly, for each mod a service wraps or supports.
        Each version records the SHA-256 of its <assembly>.dll, which the tests compare with
        the installed file.

    Returns:
      The entry.
    """
    return struct(
        name = name,
        title = title or name,
        repo = repo,
        assembly = assembly,
        versions = [
            struct(
                version = v.version,
                url = "https://github.com/%s/%s" % (repo, v.source.format(version = v.version)) if v.source else None,
                sha256 = v.sha256,
                archive = v.archive,
                folders = v.folders,
                files = v.files,
                requires = v.requires,
                assembly_sha256 = v.assembly_sha256,
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
        assembly_sha256 = None):
    """A version of a mod in MODS.

    Args:
      version: The release, as the mod numbers it.
      source: The archive's path in the mod's repository, in which {version} is the release.
      sha256: The archive's hash.
      archive: The mod whose archive of the same release holds this one, in place of a source.
      folders: The GameData folders the version installs.
      files: The files the version installs at the top of GameData.
      requires: The mods installed alongside it, as name or name==version.
      assembly_sha256: The SHA-256 of the mod's <assembly>.dll in this version's archive, which
        tells the installed version.

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
        assembly_sha256 = assembly_sha256,
    )

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
        repo = "nullprofile/LaserDist",
        assembly = "LiDAR",
        versions = [],
    ),
    mod(
        name = "RemoteTech",
        repo = "RemoteTechnologiesGroup/RemoteTech",
        assembly = "RemoteTech",
        versions = [version(
            "1.9.12",
            source = "releases/download/{version}/RemoteTech-{version}.zip",
            sha256 = "cd425571cedd4256444aaa23dcbd5f2bf581ca61f132ec2f04643cefe92f65f4",
            assembly_sha256 = "64aedaba7e8c0488d83d052ba945e163c650292bf61c5fa40cd329b72e4fffa1",
            # The archive also bundles an older ModuleManager, which is left out
            folders = ["RemoteTech"],
        )],
    ),
    mod(
        name = "InfernalRobotics",
        title = "Infernal Robotics Next",
        repo = "meirumeiru/InfernalRobotics",
        assembly = "InfernalRoboticsNext",
        versions = [version(
            "3.1.22",
            source = "releases/download/v{version}/InfernalRobotics_v{version}.zip",
            sha256 = "7e5819e06909c86e7cc117fb89582727ee9b8dfe169882e12835fa1929c59052",
            assembly_sha256 = "225287744be59b54a0f36fcb56f8884e699d59871748508f6488cfae01ad5278",
            folders = ["MagicSmokeIndustries"],
        )],
    ),
    # The maintained fork
    mod(
        name = "KerbalAlarmClock",
        title = "Kerbal Alarm Clock",
        repo = "linuxgurugamer/KerbalAlarmClock",
        assembly = "KerbalAlarmClock",
        versions = [version(
            "3.14.2.7",
            source = "releases/download/{version}/KerbalAlarmClock-1.12.5-{version}.zip",
            sha256 = "cdc06631ce478de4656d309ed04df08a2ebeb19e1d3b7fe79b221e38c1e04410",
            assembly_sha256 = "9c16035f6614059aff68e3b5e2390ca79c9818419978389896ec95d84beca14d",
            folders = ["TriggerTech"],
            requires = ["ToolbarControl"],
        )],
    ),
    # Docking Camera (KURS)
    mod(
        name = "DockingCamera",
        title = "Docking Camera (KURS)",
        repo = "linuxgurugamer/DockingCam",
        assembly = "DockingCamKURS",
        versions = [version(
            "1.3.9",
            source = "releases/download/{version}/DockingCamera-1.12.0-{version}.zip",
            sha256 = "b2f6d153edc65d0e39049c2d1da8ad818d51705d66e1c1413f9a3f0520974413",
            assembly_sha256 = "348ce1304d2656ac0dc2eec7d528b3e33326b4a2bf42028d1f72d6fcd72bd665",
            folders = ["DockingCamKURS"],
            requires = ["ToolbarControl"],
        )],
    ),
    mod(
        name = "MechJeb",
        repo = "MuMech/MechJeb2",
        assembly = "MechJeb2",
        versions = [version(
            "2.15.4.0",
            source = "releases/download/{version}/MechJeb2-{version}.zip",
            sha256 = "658c30132b00f4df972b1d39ecd3e046749ebb7544a619a15620becaf5aa1c86",
            assembly_sha256 = "996e445368516755c78a2b139564cd8bcf2bbddf52c0e8f8c26581848bb4a011",
            folders = ["MechJeb2"],
        )],
    ),
    mod(
        name = "KerbalEngineerRedux",
        title = "Kerbal Engineer Redux",
        repo = "jrbudda/KerbalEngineer",
        assembly = "KerbalEngineer",
        versions = [version(
            "1.1.9.5",
            source = "releases/download/{version}/KerbalEngineer-{version}.zip",
            sha256 = "821e5b8a4af1c4af87b1e6aadd02ee9662be78ab3343844f9711c324aa3c2a3a",
            assembly_sha256 = "ee8cbd4aab1ee3c2a5cc61354c03975c1921fc4cc5d288b9e1d70c4646526960",
            folders = ["KerbalEngineer"],
        )],
    ),
    mod(
        name = "SCANsat",
        repo = "KSPModStewards/SCANsat",
        assembly = "SCANsat",
        versions = [version(
            "22.1",
            source = "releases/download/v{version}/SCANsat-{version}.zip",
            sha256 = "34cabb87618d61934fcc4dd6332294f62cf183ba3828e092c07d67608821bd4a",
            assembly_sha256 = "add4459202eba1f67478c303ab0eee4ff7de034ee1791aca0bcefade00a63cfb",
            folders = ["SCANsat"],
            requires = ["KSPTextureLoader"],
        )],
    ),
    # The full package, whose patches give the stock pods RPM IVAs. Its patches need
    # ModuleManager, which kRPC installs.
    mod(
        name = "RasterPropMonitor",
        title = "Raster Prop Monitor",
        repo = "FirstPersonKSP/RasterPropMonitor",
        assembly = "RasterPropMonitor",
        versions = [version(
            "1.1.0.1",
            source = "releases/download/{version}/RasterPropMonitor-{version}.zip",
            sha256 = "a1f3b414e11afa9fef7f132c7ddaa45e2fe84f35fa5db93dbcc65b2fade8c897",
            assembly_sha256 = "eceb62609d2b60ef07ed8c8c9fbb2708c3c714cc0e2b6cfc6c832b9bbbd2ae0a",
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
            assembly_sha256 = "d0592c8a3cbbf985afd170b2487c3c4307c0dbeb03b13d59ac75bb80f12e9783",
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
        versions = [version(
            "1.4.9.5",
            source = "releases/download/v{version}/RealChute_v{version}.zip",
            sha256 = "3b135ae03d2214d4721a655e2d63e7c93c9a69558d8eae2473fced2bbf92bdf0",
            assembly_sha256 = "884acb3f4537a2164fa4b6d798be538c6fed54997c5de0a7674527bc973b56d5",
            folders = ["RealChute"],
            requires = ["ToolbarControl"],
        )],
    ),
    # DMagic's science part module, wrapped by the Experiment class. The archive is the part
    # module alone, and the overlay adds a part that uses it.
    mod(
        name = "DMagic",
        title = "DMModuleScienceAnimateGeneric",
        repo = "DMagic1/DMModuleScienceAnimateGeneric",
        assembly = "DMModuleScienceAnimateGeneric",
        versions = [version(
            "0.23",
            source = "releases/download/v{version}/DMModuleScienceAnimateGeneric_v{version}.zip",
            sha256 = "a4821a1d76ddf0757b57a383bd4c76c153851159d533f7bcf31f1b74401d9b60",
            assembly_sha256 = "95ef484f628a11050a2c2ae439f0b13b518e612b94ff76e7e2f4d1ae57a837cb",
            folders = ["DMagicScienceAnimate"],
        )],
    ),
    # Action Groups Extended, wrapped by the Control class. It adds no part, and patches a
    # ModuleAGX module onto every part.
    mod(
        name = "AGExt",
        title = "Extended Action Groups",
        repo = "linuxgurugamer/AGExt",
        assembly = "AGExt",
        versions = [version(
            "2.4.1.4",
            source = "releases/download/{version}/AGExt-1.12.0-{version}.zip",
            sha256 = "fa5f56288165321775c96674a1b64e49c353ccfec57fd4d7979afb1c1005f7aa",
            assembly_sha256 = "3aae6db9599ef8ff8819bd20066fc423ca769750bab43a228c0399f352d6b681",
            folders = ["Diazo"],
            requires = ["ToolbarControl", "SpaceTuxLibrary"],
        )],
    ),
    # Ferram Aerospace Research, which replaces the stock aerodynamics
    mod(
        name = "FAR",
        title = "Ferram Aerospace Research",
        repo = "KSPModStewards/Ferram-Aerospace-Research",
        assembly = "FerramAerospaceResearch",
        versions = [version(
            "0.16.1.2",
            source = "releases/download/v{version}_Marangoni/FAR_0_16_1_2_Marangoni.zip",
            sha256 = "422c6b404e7d050844f9b41218205d0bb85aed1a6e66941019d55b0ed0a30e8e",
            assembly_sha256 = "cd902484e9b9cdc5f09914968e6b66d966e25fa5a854e561e124a96b17f7f4b0",
            folders = ["FerramAerospaceResearch"],
            requires = ["ModularFlightIntegrator", "KSPBurst", "Harmony"],
        )],
    ),
    # Real propellants, limited ignitions, ullage and pressure feeding
    mod(
        name = "RealFuels",
        repo = "KSP-RO/RealFuels",
        assembly = "RealFuels",
        versions = [version(
            "15.16.1.0",
            source = "releases/download/v{version}/RealFuels_v{version}.zip",
            sha256 = "5e14f5bd87111380025a3bc93e4e3f84cbe9c9d19c1dd8c0d3212c12ab11cc1e",
            assembly_sha256 = "a994c36a8c20a59ef66c81a308ec2cd3c2d2d2b56ca3ad9721b76b25401d6556",
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
        versions = [version(
            "2.12.0.0",
            source = "releases/download/v{version}/RealAntennas-v{version}.zip",
            sha256 = "2630574a64131c9990d0613aac75d9458b5e2172d3e4c502287841f90e358e51",
            assembly_sha256 = "e137f94f72bfec9a37b251bac9cf70dcfc5084ab62851b5d493237a7396ea1d2",
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
        versions = [version(
            "2.20.0",
            source = "releases/download/v{version}/B9PartSwitch_v{version}.zip",
            sha256 = "ebf4c01f37a92f3ec0d74a9c653fae707298cf22ab0484c18a0e5670783a2121",
            assembly_sha256 = "dcac96c5274d797bb0be965b5b9b941196b3d75fe2af6f29cc0b4bb5335c3439",
            folders = ["B9PartSwitch"],
        )],
    ),
    # Near Future Solar, whose ModuleCurvedSolarPanel the SolarPanel class wraps
    mod(
        name = "NearFutureSolar",
        title = "Near Future Solar",
        repo = "post-kerbin-mining-corporation/NearFutureSolar",
        assembly = "NearFutureSolar",
        versions = [version(
            "1.3.3",
            source = "releases/download/{version}/NearFutureSolar_1_3_3.zip",
            sha256 = "4dab8ed0b69bad969a75e89668a95ddc912b7618c7de5f6e87fe570adba6c4df",
            assembly_sha256 = "f59848a6303963f047f7021a0de305b6ba01bbbe22065b98a52d84b5998d823f",
            # The archive also bundles B9PartSwitch and ModuleManager, installed on their own
            folders = ["NearFutureSolar"],
            requires = ["B9PartSwitch"],
        )],
    ),
    # Procedural Fairings, whose fairing sides the Fairing class wraps
    mod(
        name = "ProceduralFairings",
        repo = "KSP-RO/ProceduralFairings",
        assembly = "ProceduralFairings",
        versions = [version(
            "6.8.0.0",
            source = "releases/download/v{version}/ProceduralFairings-v{version}.zip",
            sha256 = "bd574414e01ebecb6fd056bb7c62a7de7cdae4160c1550c98931128890918400",
            assembly_sha256 = "f07322ee2cbb995fd13752e3a4fdede19f2526cbb24b6382edf47ba29a88795b",
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
        versions = [version(
            "7.17",
            source = "releases/download/v{version}/Firespitter_{version}.zip",
            sha256 = "ce19d8abc5ef3343752146644f4f8baa25daae4f38124541399590d4156e87fc",
            assembly_sha256 = "f727ac6a9718e30f46b0976238eb0aa7061e1163d83cd65129b9877297bac5bc",
            folders = ["Firespitter"],
        )],
    ),
    # KerbalActuators, whose WBIPropSpinner module is another engine thrust reverser. The
    # archive is the part module alone, and the overlay adds the parts that use it.
    mod(
        name = "KerbalActuators",
        repo = "Angel-125/KerbalActuators",
        assembly = "KerbalActuators",
        versions = [version(
            "1.9.0",
            source = "releases/download/v{version}/KerbalActuators.zip",
            sha256 = "ff5ab1d099158440a5259da56b8e31d778a977c5469be3f3defca2439a6b7ff7",
            assembly_sha256 = "62acfc5ffa732c22d36a4a5397faedfd2aa52b633b818f413fbb6973c0b5e870",
            # The archive also bundles ModuleManager, installed on its own
            folders = ["WildBlueIndustries"],
        )],
    ),
    # Provides ModuleNameTag, which Part.Tag uses when kOS is not installed
    mod(
        name = "KSPCommunityPartModules",
        repo = "KSPModdingLibs/KSPCommunityPartModules",
        assembly = "KSPCommunityPartModules",
        versions = [
            version(
                "0.5.1",
                source = "releases/download/v{version}/KSPCommunityPartModules-v{version}.zip",
                sha256 = "609fc54396af08399a37f9126f3839e898e5cfed688a49e0cdb3c5ee52dba294",
                assembly_sha256 = "30dc584415c4ad62bb461580b1af184424e3786328678becb32874b94c9a0748",
                folders = ["KSPCommunityPartModules"],
                requires = ["Harmony"],
            ),
            # Predates ModuleNameTag, to test that kRPC still loads with it
            version(
                "0.4.0",
                source = "releases/download/v{version}/KSPCommunityPartModules-v{version}.zip",
                sha256 = "bb1f98cdbc4e641bc21fa37b7859ddae76d93b1b14e700766a29ca6e7de3c809",
                assembly_sha256 = "b3c39f995adbd86f1ef98e9dcca04cd571ca52b50b7c842e4ae9329d0225c864",
                folders = ["KSPCommunityPartModules"],
                requires = ["Harmony"],
            ),
        ],
    ),
    # kOS, whose KOSNameTag part module Part.Tag shares
    mod(
        name = "kOS",
        repo = "KSP-KOS/KOS",
        assembly = "kOS",
        versions = [version(
            "1.6.0.1",
            source = "releases/download/{version}/KOS-{version}.zip",
            sha256 = "d61eb36652ff94e793e280d5afb49b22abe70451638ed0ac6d2850483d8a3574",
            assembly_sha256 = "fa778010db230cd9ada5ff1fc8e21880da4771579278efa8af70f19d8202260e",
            folders = ["kOS"],
        )],
    ),
    # TweakScale Rescaled, which scales parts and the stats of their modules
    mod(
        name = "TweakScaleRescaled",
        title = "TweakScale Rescaled",
        repo = "JonnyOThan/TweakScale",
        assembly = "Scale",
        versions = [version(
            "3.3.2.1",
            source = "releases/download/{version}/TweakScale-{version}.zip",
            sha256 = "669199d6ec88eab5335ee96fc0066c6ac035edc332e079c627c636078847d38d",
            assembly_sha256 = "a6550aa20f1ebb11db7d3668de988725c9aa0c93b0bd44d8890d63bc351f70f9",
            folders = ["TweakScale"],
            files = ["999_Scale_Redist.dll"],
            requires = ["Harmony"],
        )],
    ),
]
