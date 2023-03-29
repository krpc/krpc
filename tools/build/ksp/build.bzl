" KSP libraries "

ksp_unity_libs = [
    "//tools/build/ksp:Assembly-CSharp",
    "//tools/build/ksp:Assembly-CSharp-firstpass",
    "//tools/build/ksp:UnityEngine",
    "//tools/build/ksp:UnityEngine.AnimationModule",
    "//tools/build/ksp:UnityEngine.AssetBundleModule",
    "//tools/build/ksp:UnityEngine.CoreModule",
    "//tools/build/ksp:UnityEngine.IMGUIModule",
    "//tools/build/ksp:UnityEngine.InputLegacyModule",
    "//tools/build/ksp:UnityEngine.PhysicsModule",
    "//tools/build/ksp:UnityEngine.ScreenCaptureModule",
    "//tools/build/ksp:UnityEngine.SharedInternalsModule",
    "//tools/build/ksp:UnityEngine.TextRenderingModule",
    "//tools/build/ksp:UnityEngine.UI",
    "//tools/build/ksp:UnityEngine.UIModule",
    "//tools/build/ksp:UnityEngine.UnityWebRequestWWWModule",
]

ksp_net_libs = [
    "//tools/build/ksp:mscorlib",
    "//tools/build/ksp:System",
    "//tools/build/ksp:System.Core",
    "//tools/build/ksp:System.Xml",
]

ksp_libs = ksp_unity_libs + ksp_net_libs
