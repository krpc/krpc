load('/tools/build/csharp', 'csharp_binary')
load('/tools/build/csharp', 'csharp_library')

def csharp_binary_multiple(name, srcs, deps):
    names = []
    for src in srcs:
        subname = name + '/' + src
        names.append(subname)
        csharp_binary(
            name = subname,
            srcs = [src],
            deps = deps
        )
    native.filegroup(name=name, srcs=names)

def csharp_library_multiple(name, srcs, deps):
    names = []
    for src in srcs:
        subname = name + '/' + src
        names.append(subname)
        csharp_library(
            name = subname,
            srcs = [src],
            deps = deps
        )
    native.filegroup(name=name, srcs=names)

def cc_binary_multiple(name, srcs, deps):
    names = []
    for src in srcs:
        subname = name + '/' + src
        names.append(subname)
        native.cc_binary(
            name = subname,
            srcs = [src],
            deps = deps
        )
    native.filegroup(name=name, srcs=names)

def java_binary_multiple(name, srcs, deps):
    names = []
    for src in srcs:
        subname = name + '/' + src
        names.append(subname)
        native.java_binary(
            name = subname,
            main_class = src.rpartition('/')[2].rpartition('.')[0],
            srcs = [src],
            deps = deps
        )
    native.filegroup(name=name, srcs=names)
