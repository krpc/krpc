package(default_visibility = ["//visibility:public"])

genrule(
    name = 'Google.Protobuf_net35',
    srcs = ['@csharp_protobuf_net35//file'],
    outs = ['net35/Google.Protobuf.dll'],
    cmd = 'cp $< $@'
)

genrule(
    name = 'Google.Protobuf_net45',
    srcs = ['@csharp_protobuf//:lib/net45/Google.Protobuf.dll'],
    outs = ['net45/Google.Protobuf.dll'],
    cmd = 'cp $< $@'
)

genrule(
    name = 'KRPC.IO.Ports',
    srcs = ['@csharp_krpc_io_ports//file'],
    outs = ['KRPC.IO.Ports.dll'],
    cmd = 'cp $< $@'
)

genrule(
    name = 'nunit.framework',
    srcs = ['@csharp_nunit//:nunit_framework'],
    outs = ['nunit.framework.dll'],
    cmd = 'cp $< $@'
)

genrule(
    name = 'Moq',
    srcs = ['@csharp_moq//:lib/net40/Moq.dll'],
    outs = ['net40/Moq.dll'],
    cmd = 'cp $< $@'
)

genrule(
    name = 'Newtonsoft.Json_net35',
    srcs = ['@csharp_json//:lib/net35/Newtonsoft.Json.dll'],
    outs = ['net35/Newtonsoft.Json.dll'],
    cmd = 'cp $< $@'
)

genrule(
    name = 'Newtonsoft.Json_net45',
    srcs = ['@csharp_json//:lib/net45/Newtonsoft.Json.dll'],
    outs = ['net45/Newtonsoft.Json.dll'],
    cmd = 'cp $< $@'
)

genrule(
    name = 'NDesk.Options',
    srcs = ['@csharp_options//:lib/NDesk.Options.dll'],
    outs = ['NDesk.Options.dll'],
    cmd = 'cp $< $@'
)

genrule(
    name = 'ModuleManager',
    srcs = ['@module_manager//file'],
    outs = ['ModuleManager.4.0.3.dll'],
    cmd = 'cp $< $@'
)
filegroup(
    name = 'cslibs',
    srcs = [
        ':Google.Protobuf_net35',
        ':Google.Protobuf_net45',
        ':KRPC.IO.Ports',
        ':nunit.framework',
        ':Moq',
        ':Newtonsoft.Json_net35',
        ':Newtonsoft.Json_net45',
        ':NDesk.Options',
        ':ModuleManager'
    ]
)
