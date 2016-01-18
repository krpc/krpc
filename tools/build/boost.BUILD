#TODO: these libraries include all headers, only include the ones that are needed
cc_library(
    name = 'thread',
    hdrs = glob(['boost/**/*']) + ['libs/thread/src/pthread/once_atomic.cpp'],
    includes = ['.', 'libs/thread/src/pthread'],
    srcs = [
        'libs/thread/src/pthread/thread.cpp',
        'libs/thread/src/pthread/once.cpp',
        'libs/thread/src/future.cpp'
    ],
    visibility = ['//visibility:public']
)

cc_library(
    name = 'system',
    hdrs = glob(['boost/**/*']),
    includes = ['.'],
    srcs = glob(['libs/system/src/**/*.cpp']),
    visibility = ['//visibility:public']
)

cc_library(
    name = 'asio',
    hdrs = glob(['boost/asio/**/*']),
    includes = ['.'],
    visibility = ['//visibility:public']
)
