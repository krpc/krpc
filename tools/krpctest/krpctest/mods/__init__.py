"""The third-party mods the integration tests can require, and the archives they install from.

The subdirectories here are config overlays, one per GameData folder, copied over a folder
after it is installed.
"""

# The archive of each mod and dependency, as its URL and SHA-256. Each holds its GameData
# folders under GameData/.
ARCHIVES = {
    # The archive also bundles an older ModuleManager, which is left out
    "remotetech": (
        "https://github.com/RemoteTechnologiesGroup/RemoteTech/releases/download/1.9.12/RemoteTech-1.9.12.zip",
        "cd425571cedd4256444aaa23dcbd5f2bf581ca61f132ec2f04643cefe92f65f4",
    ),
    "infernal_robotics": (
        "https://github.com/meirumeiru/InfernalRobotics/releases/download/v3.1.22/InfernalRobotics_v3.1.22.zip",
        "7e5819e06909c86e7cc117fb89582727ee9b8dfe169882e12835fa1929c59052",
    ),
    # The maintained fork
    "kerbal_alarm_clock": (
        "https://github.com/linuxgurugamer/KerbalAlarmClock/releases/download/3.14.2.7/KerbalAlarmClock-1.12.5-3.14.2.7.zip",
        "cdc06631ce478de4656d309ed04df08a2ebeb19e1d3b7fe79b221e38c1e04410",
    ),
    "docking_camera": (
        "https://github.com/linuxgurugamer/DockingCam/releases/download/1.3.9/DockingCamera-1.12.0-1.3.9.zip",
        "b2f6d153edc65d0e39049c2d1da8ad818d51705d66e1c1413f9a3f0520974413",
    ),
    "realchute": (
        "https://github.com/ChrisViral/RealChute/releases/download/v1.4.9.5/RealChute_v1.4.9.5.zip",
        "3b135ae03d2214d4721a655e2d63e7c93c9a69558d8eae2473fced2bbf92bdf0",
    ),
    # Runtime patching, which most of the mods reference. Its absence crashes kRPC's assembly
    # scanner at startup.
    "harmony": (
        "https://github.com/KSPModdingLibs/HarmonyKSP/releases/download/2.2.1.0/HarmonyKSP_2.2.1.0_for_KSP1.8%2B.zip",
        "51d63f6739eaf22f12ce9456adace54385520c67cc13735d5300d5aa15c79476",
    ),
    "clickthroughblocker": (
        "https://github.com/linuxgurugamer/ClickThroughBlocker/releases/download/2.1.10.23/ClickThroughBlocker-1.12.5-2.1.10.23.zip",
        "83bf6c3556a7d2c46634260100a535f1ccda99f83fc999c846b45a93c6369e3e",
    ),
    "toolbarcontrol": (
        "https://github.com/linuxgurugamer/ToolbarControl/releases/download/0.1.9.14/ToolbarControl-1.12.0-0.1.9.14.zip",
        "0f44f5500f8e914346cb2e6dd5c5e5f26df9f26caa6ce8179738a3842aac2a5b",
    ),
    # The part module alone. The overlay adds a part that uses it.
    "dmagic_science_animate": (
        "https://github.com/DMagic1/DMModuleScienceAnimateGeneric/releases/download/v0.23/DMModuleScienceAnimateGeneric_v0.23.zip",
        "a4821a1d76ddf0757b57a383bd4c76c153851159d533f7bcf31f1b74401d9b60",
    ),
    "agext": (
        "https://github.com/linuxgurugamer/AGExt/releases/download/2.4.1.4/AGExt-1.12.0-2.4.1.4.zip",
        "fa5f56288165321775c96674a1b64e49c353ccfec57fd4d7979afb1c1005f7aa",
    ),
    "far": (
        "https://github.com/KSPModStewards/Ferram-Aerospace-Research/releases/download/v0.16.1.2_Marangoni/FAR_0_16_1_2_Marangoni.zip",
        "422c6b404e7d050844f9b41218205d0bb85aed1a6e66941019d55b0ed0a30e8e",
    ),
    # KSP does not load FAR without it. It has no release of its own, so it comes from the FAR
    # archive.
    "modular_flight_integrator": (
        "https://github.com/KSPModStewards/Ferram-Aerospace-Research/releases/download/v0.16.1.2_Marangoni/FAR_0_16_1_2_Marangoni.zip",
        "422c6b404e7d050844f9b41218205d0bb85aed1a6e66941019d55b0ed0a30e8e",
    ),
    # The Unity job-system assemblies FAR's voxelizer is built against. This is KSPBurst-Lite,
    # the assemblies without the Burst compiler.
    "kspburst": (
        "https://github.com/KSPModdingLibs/KSPBurst/releases/download/v1.7.4.11/KSPBurst_1.7.4.11.zip",
        "6295c300c1a11b520b2037370f192a4fabe919ee1afb93bdb594e4499b298c45",
    ),
    "spacetuxlibrary": (
        "https://github.com/linuxgurugamer/SpaceTuxLibrary/releases/download/0.0.9/SpaceTuxLibrary-1.12.0-0.0.9.zip",
        "e916b6907e8310e08baa49532d04f92068864cb38b93afd664bf712b190197f1",
    ),
    "realfuels": (
        "https://github.com/KSP-RO/RealFuels/releases/download/v15.16.1.0/RealFuels_v15.16.1.0.zip",
        "5e14f5bd87111380025a3bc93e4e3f84cbe9c9d19c1dd8c0d3212c12ab11cc1e",
    ),
    # RealFuels' engine configs for stock parts. Frozen in CKAN.
    "realfuels_stock": (
        "https://github.com/ValentinBischof/RealFuels-Stock/releases/download/v5.1.0/RealFuels-Stock-v5.1.0.zip",
        "5ad5dde73fa84dcd1681e03385d9a4f9c6e65ea62248329c29e1038102412fac",
    ),
    "solver_engines": (
        "https://github.com/KSP-RO/SolverEngines/releases/download/v3.14.0/SolverEngines_v3.14.0.zip",
        "360ab293f3de1974ec7ce99f5fd8e4a3e348b5b9a40877c0cab67c5ec45e83a3",
    ),
    "routils": (
        "https://github.com/KSP-RO/ROUtils/releases/download/v1.2.0.0/ROUtils-v1.2.0.0.zip",
        "3118abbe9cbf61cbaa6f395d3bf0786ed402a8eaa54833799869dd57fdc99d39",
    ),
    "serializationfix": (
        "https://github.com/Phantomical/KSPSerializationFix/releases/download/v1.0.1/KSPSerializationFix.zip",
        "8232add723a443545c729b5d48e36806ed90d70576030e29b2f7436dac2c9445",
    ),
    "kspcommunityfixes": (
        "https://github.com/KSPModdingLibs/KSPCommunityFixes/releases/download/1.41.1/KSPCommunityFixes_1.41.1.zip",
        "3b63c37d7da9ec0c93063374df1c7a365b02fa66ad6b033c7a2b6ae6ff18215e",
    ),
    "community_resource_pack": (
        "https://github.com/UmbraSpaceIndustries/CommunityResourcePack/releases/download/v112.0.1/CommunityResourcePack_112.0.1.zip",
        "e6c264137c391bd96b2cdd996b163ec4d7f200eccee0b2555edaf8bae19db2ed",
    ),
    "kspcommunitypartmodules": (
        "https://github.com/KSPModdingLibs/KSPCommunityPartModules/releases/download/v0.5.1/KSPCommunityPartModules-v0.5.1.zip",
        "609fc54396af08399a37f9126f3839e898e5cfed688a49e0cdb3c5ee52dba294",
    ),
    "kspcommunitypartmodules_0_4": (
        "https://github.com/KSPModdingLibs/KSPCommunityPartModules/releases/download/v0.4.0/KSPCommunityPartModules-v0.4.0.zip",
        "bb1f98cdbc4e641bc21fa37b7859ddae76d93b1b14e700766a29ca6e7de3c809",
    ),
    "kos": (
        "https://github.com/KSP-KOS/KOS/releases/download/1.6.0.1/KOS-1.6.0.1.zip",
        "d61eb36652ff94e793e280d5afb49b22abe70451638ed0ac6d2850483d8a3574",
    ),
}

# Managed third-party mods: name accepted by --mods -> list of components it installs, each
# an (archive in ARCHIVES, GameData subdir it installs as) pair. Most mods are a single
# component; KerbalAlarmClock, RealChute and AGExt also pull in the shared
# ClickThroughBlocker / ToolbarControl / Harmony dependencies, whose assemblies their plugins
# hard-depend on, and FAR pulls in ModularFlightIntegrator and KSPBurst.
MODS = {
    "RemoteTech": [("remotetech", "RemoteTech")],
    "InfernalRobotics": [("infernal_robotics", "MagicSmokeIndustries")],
    "KerbalAlarmClock": [
        ("kerbal_alarm_clock", "TriggerTech"),
        ("harmony", "000_Harmony"),
        ("clickthroughblocker", "000_ClickThroughBlocker"),
        ("toolbarcontrol", "001_ToolbarControl"),
    ],
    "DockingCamera": [
        ("docking_camera", "DockingCamKURS"),
        ("harmony", "000_Harmony"),
        ("clickthroughblocker", "000_ClickThroughBlocker"),
        ("toolbarcontrol", "001_ToolbarControl"),
    ],
    "RealChute": [
        ("realchute", "RealChute"),
        ("harmony", "000_Harmony"),
        ("clickthroughblocker", "000_ClickThroughBlocker"),
        ("toolbarcontrol", "001_ToolbarControl"),
    ],
    "DMagic": [("dmagic_science_animate", "DMagicScienceAnimate")],
    "AGExt": [
        ("agext", "Diazo"),
        ("harmony", "000_Harmony"),
        ("clickthroughblocker", "000_ClickThroughBlocker"),
        ("toolbarcontrol", "001_ToolbarControl"),
        ("spacetuxlibrary", "SpaceTuxLibrary"),
    ],
    "FAR": [
        ("far", "FerramAerospaceResearch"),
        ("modular_flight_integrator", "ModularFlightIntegrator"),
        ("kspburst", "000_KSPBurst"),
        ("harmony", "000_Harmony"),
    ],
    "RealFuels": [
        ("realfuels", "RealFuels"),
        ("realfuels_stock", "RealFuels-Stock"),
        ("solver_engines", "SolverEngines"),
        ("routils", "ROUtils"),
        ("serializationfix", "000_SerializationFix"),
        ("kspcommunityfixes", "KSPCommunityFixes"),
        ("harmony", "000_Harmony"),
        ("community_resource_pack", "CommunityResourcePack"),
    ],
    "KSPCommunityPartModules": [
        ("kspcommunitypartmodules", "KSPCommunityPartModules"),
        ("harmony", "000_Harmony"),
    ],
    "KSPCommunityPartModules-0.4": [
        ("kspcommunitypartmodules_0_4", "KSPCommunityPartModules"),
        ("harmony", "000_Harmony"),
    ],
    "kOS": [("kos", "kOS")],
}
