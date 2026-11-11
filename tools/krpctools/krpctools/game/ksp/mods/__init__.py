"""The third-party KSP mods kRPC supports, which a test can require by name.

Each mod pins the archive it installs from, the GameData folders it takes from it, the mods
it needs alongside it, and how to tell it loaded in the running game. A mod with no presence
check is only installed as another's dependency. The subdirectories here hold each mod's
config overlays, one per GameData folder.
"""

from ..game import Mod

_GITHUB = "https://github.com/"


# Presence checks


class _ServiceMod(Mod):
    """A mod wrapped by a kRPC service, whose available property reports it."""

    service = ""

    def present(self, game, conn):
        return getattr(conn, self.service).available


class _PartMod(Mod):
    """A mod that adds a part, found in the part catalog by TestingTools. The name is the
    in-game part name, in which KSP replaces underscores with periods."""

    part = ""

    def present(self, game, conn):
        return conn.testing_tools.part_available(self.part)


# Dependencies, never requested on their own


class Harmony(Mod):
    """Runtime patching, which most of the mods below reference. Its absence crashes kRPC's
    assembly scanner at startup."""

    name = "Harmony"
    source = _GITHUB + (
        "KSPModdingLibs/HarmonyKSP/releases/download/2.2.1.0/"
        "HarmonyKSP_2.2.1.0_for_KSP1.8%2B.zip"
    )
    sha256 = "51d63f6739eaf22f12ce9456adace54385520c67cc13735d5300d5aa15c79476"
    folders = ("000_Harmony",)


class ClickThroughBlocker(Mod):
    name = "ClickThroughBlocker"
    source = _GITHUB + (
        "linuxgurugamer/ClickThroughBlocker/releases/download/2.1.10.23/"
        "ClickThroughBlocker-1.12.5-2.1.10.23.zip"
    )
    sha256 = "83bf6c3556a7d2c46634260100a535f1ccda99f83fc999c846b45a93c6369e3e"
    folders = ("000_ClickThroughBlocker",)
    requires = ("Harmony",)


class ToolbarControl(Mod):
    name = "ToolbarControl"
    source = _GITHUB + (
        "linuxgurugamer/ToolbarControl/releases/download/0.1.9.14/"
        "ToolbarControl-1.12.0-0.1.9.14.zip"
    )
    sha256 = "0f44f5500f8e914346cb2e6dd5c5e5f26df9f26caa6ce8179738a3842aac2a5b"
    folders = ("001_ToolbarControl",)
    requires = ("ClickThroughBlocker",)


class SpaceTuxLibrary(Mod):
    """LinuxGuruGamer's shared library, whose KSP_Log AGExt references."""

    name = "SpaceTuxLibrary"
    source = _GITHUB + (
        "linuxgurugamer/SpaceTuxLibrary/releases/download/0.0.9/"
        "SpaceTuxLibrary-1.12.0-0.0.9.zip"
    )
    sha256 = "e916b6907e8310e08baa49532d04f92068864cb38b93afd664bf712b190197f1"
    folders = ("SpaceTuxLibrary",)


_FAR_SOURCE = _GITHUB + (
    "KSPModStewards/Ferram-Aerospace-Research/releases/download/v0.16.1.2_Marangoni/"
    "FAR_0_16_1_2_Marangoni.zip"
)
_FAR_SHA256 = "422c6b404e7d050844f9b41218205d0bb85aed1a6e66941019d55b0ed0a30e8e"


class KSPTextureLoader(Mod):
    """Loads the body textures SCANsat's visual maps are drawn from."""

    name = "KSPTextureLoader"
    source = _GITHUB + (
        "Phantomical/KSPTextureLoader/releases/download/v1.0.41/KSPTextureLoader.zip"
    )
    sha256 = "8d34d60c69e2c13e331d267c6eb14362c5dc9319eb013acbd67f59d6deabe382"
    folders = ("KSPTextureLoader",)


class ModularFlightIntegrator(Mod):
    """Lets several mods hook the flight integrator. KSP does not load FAR without it. It
    has no release of its own, so it comes from the FAR archive."""

    name = "ModularFlightIntegrator"
    source = _FAR_SOURCE
    sha256 = _FAR_SHA256
    folders = ("ModularFlightIntegrator",)


class KSPBurst(Mod):
    """The Unity job-system assemblies FAR's voxelizer is built against, which KSP does not
    ship. This is KSPBurst-Lite, the assemblies without the Burst compiler."""

    name = "KSPBurst"
    source = _GITHUB + (
        "KSPModdingLibs/KSPBurst/releases/download/v1.7.4.11/KSPBurst_1.7.4.11.zip"
    )
    sha256 = "6295c300c1a11b520b2037370f192a4fabe919ee1afb93bdb594e4499b298c45"
    folders = ("000_KSPBurst",)
    requires = ("Harmony",)


class RealFuelsStock(Mod):
    """RealFuels' engine configs for stock parts. Without them no stock engine becomes a
    ModuleEnginesRF. Frozen in CKAN."""

    name = "RealFuels-Stock"
    source = _GITHUB + (
        "ValentinBischof/RealFuels-Stock/releases/download/v5.1.0/"
        "RealFuels-Stock-v5.1.0.zip"
    )
    sha256 = "5ad5dde73fa84dcd1681e03385d9a4f9c6e65ea62248329c29e1038102412fac"
    folders = ("RealFuels-Stock",)


class SolverEngines(Mod):
    """The base class of RealFuels' engine module."""

    name = "SolverEngines"
    source = _GITHUB + (
        "KSP-RO/SolverEngines/releases/download/v3.14.0/SolverEngines_v3.14.0.zip"
    )
    sha256 = "360ab293f3de1974ec7ce99f5fd8e4a3e348b5b9a40877c0cab67c5ec45e83a3"
    folders = ("SolverEngines",)


class ROUtils(Mod):
    """The utility library of RealFuels and Procedural Fairings."""

    name = "ROUtils"
    source = _GITHUB + "KSP-RO/ROUtils/releases/download/v1.2.0.0/ROUtils-v1.2.0.0.zip"
    sha256 = "3118abbe9cbf61cbaa6f395d3bf0786ed402a8eaa54833799869dd57fdc99d39"
    folders = ("ROUtils",)
    requires = ("KSPCommunityFixes", "SerializationFix")


class SerializationFix(Mod):
    """Serializes ROUtils' curves."""

    name = "SerializationFix"
    source = _GITHUB + (
        "Phantomical/KSPSerializationFix/releases/download/v1.0.1/KSPSerializationFix.zip"
    )
    sha256 = "8232add723a443545c729b5d48e36806ed90d70576030e29b2f7436dac2c9445"
    folders = ("000_SerializationFix",)


class KSPCommunityFixes(Mod):
    name = "KSPCommunityFixes"
    source = _GITHUB + (
        "KSPModdingLibs/KSPCommunityFixes/releases/download/1.41.1/"
        "KSPCommunityFixes_1.41.1.zip"
    )
    sha256 = "3b63c37d7da9ec0c93063374df1c7a365b02fa66ad6b033c7a2b6ae6ff18215e"
    folders = ("KSPCommunityFixes",)
    requires = ("Harmony",)


class CommunityResourcePack(Mod):
    """The resource definitions RealFuels' configs are written against."""

    name = "CommunityResourcePack"
    source = _GITHUB + (
        "UmbraSpaceIndustries/CommunityResourcePack/releases/download/v112.0.1/"
        "CommunityResourcePack_112.0.1.zip"
    )
    sha256 = "e6c264137c391bd96b2cdd996b163ec4d7f200eccee0b2555edaf8bae19db2ed"
    folders = ("CommunityResourcePack",)


# The mods kRPC wraps


class RemoteTech(_ServiceMod):
    name = "RemoteTech"
    source = _GITHUB + (
        "RemoteTechnologiesGroup/RemoteTech/releases/download/1.9.12/RemoteTech-1.9.12.zip"
    )
    sha256 = "cd425571cedd4256444aaa23dcbd5f2bf581ca61f132ec2f04643cefe92f65f4"
    # The archive also bundles an older ModuleManager, which is left out
    folders = ("RemoteTech",)
    service = "remote_tech"


class InfernalRobotics(_ServiceMod):
    name = "InfernalRobotics"
    source = _GITHUB + (
        "meirumeiru/InfernalRobotics/releases/download/v3.1.22/InfernalRobotics_v3.1.22.zip"
    )
    sha256 = "7e5819e06909c86e7cc117fb89582727ee9b8dfe169882e12835fa1929c59052"
    folders = ("MagicSmokeIndustries",)
    service = "infernal_robotics"


class KerbalAlarmClock(_ServiceMod):
    """The maintained fork."""

    name = "KerbalAlarmClock"
    source = _GITHUB + (
        "linuxgurugamer/KerbalAlarmClock/releases/download/3.14.2.7/"
        "KerbalAlarmClock-1.12.5-3.14.2.7.zip"
    )
    sha256 = "cdc06631ce478de4656d309ed04df08a2ebeb19e1d3b7fe79b221e38c1e04410"
    folders = ("TriggerTech",)
    requires = ("ToolbarControl",)
    service = "kerbal_alarm_clock"


class DockingCamera(_ServiceMod):
    """Docking Camera (KURS)."""

    name = "DockingCamera"
    source = _GITHUB + (
        "linuxgurugamer/DockingCam/releases/download/1.3.9/DockingCamera-1.12.0-1.3.9.zip"
    )
    sha256 = "b2f6d153edc65d0e39049c2d1da8ad818d51705d66e1c1413f9a3f0520974413"
    folders = ("DockingCamKURS",)
    requires = ("ToolbarControl",)
    service = "docking_camera"


class MechJeb(Mod):
    """MechJeb, wrapped by the MechJeb service. The service is for the flight scene only, so
    we find the mod by MechJebCore, the part module it adds to every command part."""

    name = "MechJeb"
    source = _GITHUB + (
        "MuMech/MechJeb2/releases/download/2.15.4.0/MechJeb2-2.15.4.0.zip"
    )
    sha256 = "658c30132b00f4df972b1d39ecd3e046749ebb7544a619a15620becaf5aa1c86"
    folders = ("MechJeb2",)

    def present(self, game, conn):
        return conn.testing_tools.part_module_available("MechJebCore")


class KerbalEngineerRedux(_ServiceMod):
    name = "KerbalEngineerRedux"
    source = _GITHUB + (
        "jrbudda/KerbalEngineer/releases/download/1.1.9.5/KerbalEngineer-1.1.9.5.zip"
    )
    sha256 = "821e5b8a4af1c4af87b1e6aadd02ee9662be78ab3343844f9711c324aa3c2a3a"
    folders = ("KerbalEngineer",)
    service = "kerbal_engineer_redux"


class ScanSat(_ServiceMod):
    name = "SCANsat"
    source = _GITHUB + (
        "KSPModStewards/SCANsat/releases/download/v22.1/SCANsat-22.1.zip"
    )
    sha256 = "34cabb87618d61934fcc4dd6332294f62cf183ba3828e092c07d67608821bd4a"
    folders = ("SCANsat",)
    requires = ("KSPTextureLoader",)
    service = "scan_sat"


class RealChute(_PartMod):
    """Wrapped by the SpaceCenter Parachute class. Its part modules do not register without
    ClickThroughBlocker and ToolbarControl."""

    name = "RealChute"
    source = _GITHUB + (
        "ChrisViral/RealChute/releases/download/v1.4.9.5/RealChute_v1.4.9.5.zip"
    )
    sha256 = "3b135ae03d2214d4721a655e2d63e7c93c9a69558d8eae2473fced2bbf92bdf0"
    folders = ("RealChute",)
    requires = ("ToolbarControl",)
    part = "RC.stack"


class DMagic(_PartMod):
    """DMagic's science part module, wrapped by the SpaceCenter Experiment class. The
    archive is the part module alone, and the overlay adds a part that uses it."""

    name = "DMagic"
    source = _GITHUB + (
        "DMagic1/DMModuleScienceAnimateGeneric/releases/download/v0.23/"
        "DMModuleScienceAnimateGeneric_v0.23.zip"
    )
    sha256 = "a4821a1d76ddf0757b57a383bd4c76c153851159d533f7bcf31f1b74401d9b60"
    folders = ("DMagicScienceAnimate",)
    part = "dmagicSensorTest"


class AGExt(Mod):
    """Action Groups Extended, wrapped by the SpaceCenter Control class. It adds no part,
    and patches a ModuleAGX module onto every part."""

    name = "AGExt"
    source = _GITHUB + (
        "linuxgurugamer/AGExt/releases/download/2.4.1.4/AGExt-1.12.0-2.4.1.4.zip"
    )
    sha256 = "fa5f56288165321775c96674a1b64e49c353ccfec57fd4d7979afb1c1005f7aa"
    folders = ("Diazo",)
    requires = ("ToolbarControl", "SpaceTuxLibrary")

    def present(self, game, conn):
        return conn.testing_tools.part_module_available("ModuleAGX")


class FAR(Mod):
    """Ferram Aerospace Research, which replaces the stock aerodynamics. A SpaceCenter
    property gives whether it is active."""

    name = "FAR"
    source = _FAR_SOURCE
    sha256 = _FAR_SHA256
    folders = ("FerramAerospaceResearch",)
    requires = ("ModularFlightIntegrator", "KSPBurst", "Harmony")

    def present(self, game, conn):
        return conn.space_center.far_available


class RealFuels(Mod):
    """Real propellants, limited ignitions, ullage and pressure feeding. A SpaceCenter
    property gives whether it is active."""

    name = "RealFuels"
    source = _GITHUB + (
        "KSP-RO/RealFuels/releases/download/v15.16.1.0/RealFuels_v15.16.1.0.zip"
    )
    sha256 = "5e14f5bd87111380025a3bc93e4e3f84cbe9c9d19c1dd8c0d3212c12ab11cc1e"
    folders = ("RealFuels",)
    requires = (
        "RealFuels-Stock",
        "SolverEngines",
        "ROUtils",
        "CommunityResourcePack",
    )

    def present(self, game, conn):
        return conn.space_center.real_fuels_available


class RealAntennas(Mod):
    """RealAntennas, which replaces CommNet's range model with a link budget. A
    SpaceCenter property gives whether it is active."""

    name = "RealAntennas"
    source = _GITHUB + (
        "KSP-RO/RealAntennas/releases/download/v2.12.0.0/RealAntennas-v2.12.0.0.zip"
    )
    sha256 = "2630574a64131c9990d0613aac75d9458b5e2172d3e4c502287841f90e358e51"
    folders = ("RealAntennas",)
    requires = ("ToolbarControl", "KSPCommunityFixes", "KSPBurst")

    def present(self, game, conn):
        return conn.space_center.real_antennas_available


class B9PartSwitch(_PartMod):
    """Switches part subtypes, which changes their resources and mass. The archive is the
    part module alone, and the overlay adds a part that uses it."""

    name = "B9PartSwitch"
    source = _GITHUB + (
        "blowfishpro/B9PartSwitch/releases/download/v2.20.0/B9PartSwitch_v2.20.0.zip"
    )
    sha256 = "ebf4c01f37a92f3ec0d74a9c653fae707298cf22ab0484c18a0e5670783a2121"
    folders = ("B9PartSwitch",)
    part = "b9PartSwitchTest"


class NearFutureSolar(_PartMod):
    """Near Future Solar, whose ModuleCurvedSolarPanel the SpaceCenter SolarPanel class
    wraps."""

    name = "NearFutureSolar"
    source = _GITHUB + (
        "post-kerbin-mining-corporation/NearFutureSolar/releases/download/1.3.3/"
        "NearFutureSolar_1_3_3.zip"
    )
    sha256 = "4dab8ed0b69bad969a75e89668a95ddc912b7618c7de5f6e87fe570adba6c4df"
    # The archive also bundles B9PartSwitch and ModuleManager, installed on their own
    folders = ("NearFutureSolar",)
    requires = ("B9PartSwitch",)
    part = "nfs-panel-deploying-curved-25-1"


class ProceduralFairings(Mod):
    """Procedural Fairings, whose fairing sides the SpaceCenter Fairing class wraps. Its
    parts load without its plugin, so the check looks for the plugin's module."""

    name = "ProceduralFairings"
    source = _GITHUB + (
        "KSP-RO/ProceduralFairings/releases/download/v6.8.0.0/"
        "ProceduralFairings-v6.8.0.0.zip"
    )
    sha256 = "bd574414e01ebecb6fd056bb7c62a7de7cdae4160c1550c98931128890918400"
    folders = ("ProceduralFairings",)
    requires = ("ROUtils", "KSPCommunityFixes")

    def present(self, game, conn):
        return conn.testing_tools.part_module_available("ProceduralFairingDecoupler")


class Firespitter(_PartMod):
    """Firespitter, whose FSswitchEngineThrustTransform module is one of the engine thrust
    reversers the SpaceCenter Engine class drives."""

    name = "Firespitter"
    source = _GITHUB + "snjo/Firespitter/releases/download/v7.17/Firespitter_7.17.zip"
    sha256 = "ce19d8abc5ef3343752146644f4f8baa25daae4f38124541399590d4156e87fc"
    folders = ("Firespitter",)
    part = "FSnoseEngine"


class KerbalActuators(_PartMod):
    """KerbalActuators, whose WBIPropSpinner module is another engine thrust reverser. The
    archive is the part module alone, and the overlay adds the parts that use it."""

    name = "KerbalActuators"
    source = _GITHUB + (
        "Angel-125/KerbalActuators/releases/download/v1.9.0/KerbalActuators.zip"
    )
    sha256 = "ff5ab1d099158440a5259da56b8e31d778a977c5469be3f3defca2439a6b7ff7"
    # The archive also bundles ModuleManager, installed on its own
    folders = ("WildBlueIndustries",)
    part = "kerbalActuatorsReverserTest"


class _AssemblyMod(Mod):
    """A mod told apart by the major.minor version of its loaded assembly."""

    assembly = ""
    version = ""

    def present(self, game, conn):
        return conn.testing_tools.assembly_version(self.assembly) == self.version


class KSPCommunityPartModules(_AssemblyMod):
    """Provides ModuleNameTag, which SpaceCenter's Part.Tag uses when kOS is not
    installed."""

    name = "KSPCommunityPartModules"
    source = _GITHUB + (
        "KSPModdingLibs/KSPCommunityPartModules/releases/download/v0.5.1/"
        "KSPCommunityPartModules-v0.5.1.zip"
    )
    sha256 = "609fc54396af08399a37f9126f3839e898e5cfed688a49e0cdb3c5ee52dba294"
    folders = ("KSPCommunityPartModules",)
    requires = ("Harmony",)
    assembly = "KSPCommunityPartModules"
    version = "0.5"


class KSPCommunityPartModules04(KSPCommunityPartModules):
    """0.4, which predates ModuleNameTag, to test that kRPC still loads with it."""

    name = "KSPCommunityPartModules-0.4"
    source = _GITHUB + (
        "KSPModdingLibs/KSPCommunityPartModules/releases/download/v0.4.0/"
        "KSPCommunityPartModules-v0.4.0.zip"
    )
    sha256 = "bb1f98cdbc4e641bc21fa37b7859ddae76d93b1b14e700766a29ca6e7de3c809"
    version = "0.4"


class KOS(_PartMod):
    """kOS, whose KOSNameTag part module Part.Tag shares."""

    name = "kOS"
    source = _GITHUB + "KSP-KOS/KOS/releases/download/1.6.0.1/KOS-1.6.0.1.zip"
    sha256 = "d61eb36652ff94e793e280d5afb49b22abe70451638ed0ac6d2850483d8a3574"
    folders = ("kOS",)
    part = "kOSMachine1m"


class Trajectories(Mod):
    """Trajectories, trajectory predictions accounting for atmospheric drag and lift."""

    name = "Trajectories"
    source = (
        _GITHUB
        + "linuxgurugamer/KSPTrajectories/releases/download/2.4.5.5/Trajectories-1.12.5-2.4.5.5.zip"
    )
    sha256 = "92d9670da884e40608193200a876bafcf70ea59869b5bc1b3fbf31d541313b16"
    folders = ("Trajectories",)
    requires = ("ClickThroughBlocker", "ToolbarControl", "SpaceTuxLibrary")


class TweakScaleRescaled(_AssemblyMod):
    """TweakScale Rescaled, which scales parts and the stats of their modules."""

    name = "TweakScaleRescaled"
    source = _GITHUB + (
        "JonnyOThan/TweakScale/releases/download/3.3.2.1/TweakScale-3.3.2.1.zip"
    )
    sha256 = "669199d6ec88eab5335ee96fc0066c6ac035edc332e079c627c636078847d38d"
    folders = ("TweakScale",)
    files = ("999_Scale_Redist.dll",)
    requires = ("Harmony",)
    assembly = "Scale"
    version = "3.3"


MODS = [
    Harmony,
    ClickThroughBlocker,
    ToolbarControl,
    SpaceTuxLibrary,
    KSPTextureLoader,
    ModularFlightIntegrator,
    KSPBurst,
    RealFuelsStock,
    SolverEngines,
    ROUtils,
    SerializationFix,
    KSPCommunityFixes,
    CommunityResourcePack,
    RemoteTech,
    InfernalRobotics,
    KerbalAlarmClock,
    DockingCamera,
    MechJeb,
    KerbalEngineerRedux,
    ScanSat,
    RealChute,
    DMagic,
    AGExt,
    FAR,
    RealFuels,
    RealAntennas,
    B9PartSwitch,
    NearFutureSolar,
    ProceduralFairings,
    Firespitter,
    KerbalActuators,
    KSPCommunityPartModules,
    KSPCommunityPartModules04,
    KOS,
    TweakScaleRescaled,
    Trajectories,
]
