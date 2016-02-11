cc_library(
    name = 'main',
    srcs = glob(['gtest/src/*.cc', 'src/*.cc'], exclude = ['gtest/src/gtest-all.cc', 'src/gmock-all.cc']),
    hdrs = glob(['**/*.h', 'gtest/src/*.cc', 'src/*.cc']),
    includes = [
        './',
        'gtest',
        'gtest/include',
        'include'
    ],
    linkopts = ['-pthread'],
    visibility = ['//visibility:public'],
)
