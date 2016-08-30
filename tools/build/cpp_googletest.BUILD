cc_library(
    name = 'gtest',
    srcs = glob(['googletest/src/*.cc'], exclude = ['googletest/src/gtest-all.cc']),
    hdrs = glob(['**/*.h', 'googletest/src/*.cc']),
    includes = [
        './',
        'googletest',
        'googletest/include',
        'include'
    ],
    linkopts = ['-pthread'],
    visibility = ['//visibility:public'],
)

cc_library(
    name = 'gmock',
    srcs = glob(['googlemock/src/*.cc'], exclude = ['googlemock/src/gmock-all.cc']),
    hdrs = glob(['**/*.h', 'googlemock/src/*.cc']),
    includes = [
        './',
        'googlemock',
        'googlemock/include',
        'include'
    ],
    deps = [':gtest'],
    linkopts = ['-pthread'],
    visibility = ['//visibility:public'],
)
