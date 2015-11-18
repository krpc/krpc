_MCS = 'mcs'
_MCS_FLAGS = ['-noconfig', '-nostdlib']

def _ref_impl(ctx):
    input = ctx.file.file
    output = ctx.outputs.lib

    ctx.action(
        mnemonic = 'CSharpReference',
        inputs = [input],
        outputs = [output],
        command = 'cp %s %s' % (input.path, output.path) # TODO: use symlinks
    )

    return struct(
        name = ctx.label.name,
        target_type = ctx.attr._target_type,
        lib = output,
        out = output
    )

def _csc_args(srcs, deps, exe=None, lib=None, doc=None, warn=4):
    if exe: target_type = 'exe'
    if lib: target_type = 'library'
    args = ['-target:%s' % target_type]
    args.extend(_MCS_FLAGS)
    if exe: args.append('-out:%s' % exe.path)
    if lib: args.append('-out:%s' % lib.path)
    if doc: args.append('-doc:%s' % doc.path)
    args.append('-warn:%d' % warn)
    args.extend([x.path for x in srcs])
    args.extend(['-reference:%s' % x.path for x in deps])
    return args

def _lib_impl(ctx):
    lib_output = ctx.outputs.lib
    doc_output = ctx.outputs.doc
    outputs = [lib_output, doc_output]
    srcs = ctx.files.srcs
    deps = [dep.lib for dep in ctx.attr.deps]
    inputs = srcs + deps

    cmd = ctx.attr.csc
    args = _csc_args(srcs, deps, lib=lib_output, doc=doc_output, warn=ctx.attr.warn)

    ctx.action(
        mnemonic = 'CSharpCompile',
        inputs = inputs,
        outputs = outputs,
        command = '%s %s' % (cmd, ' '.join(args))
    )

    return struct(
        name = ctx.label.name,
        target_type = ctx.attr._target_type,
        lib = lib_output,
        out = outputs
    )

def _bin_impl(ctx):
    bin_output = ctx.outputs.bin
    outputs = [bin_output]
    srcs = ctx.files.srcs
    deps = [dep.lib for dep in ctx.attr.deps]
    inputs = srcs + deps

    cmd = ctx.attr.csc
    args = _csc_args(srcs, deps, exe=bin_output, warn=ctx.attr.warn)

    ctx.action(
        mnemonic = 'CSharpCompile',
        inputs = inputs,
        outputs = outputs,
        command = '%s %s' % (cmd, ' '.join(args))
    )

    content = ''
    for dep in ctx.files.deps:
        content += 'ln -s %s $0.runfiles/%s\n' % (dep.short_path, dep.basename)
    content += 'ln -s ../%s $0.runfiles/%s\n' % (bin_output.basename, bin_output.basename)
    content += '/usr/bin/mono $0.runfiles/%s "$@" 1>/dev/null\n' % bin_output.basename
    ctx.file_action(
        ctx.outputs.executable,
        content
    )
    runfiles = ctx.runfiles(files = ctx.files.deps)

    return struct(
        name = ctx.label.name,
        target_type = ctx.attr._target_type,
        out = outputs,
        runfiles = runfiles
    )

_COMMON_ATTRS = {
    'csc': attr.string(default=_MCS),
    'deps': attr.label_list(providers=['out', 'lib', 'target_type']),
    'srcs': attr.label_list(allow_files=FileType(['.cs'])),
    'warn': attr.int(default=4),
}

csharp_reference = rule(
    implementation = _ref_impl,
    attrs = {
        'file': attr.label(mandatory=True, allow_files=True, single_file=True),
        '_target_type': attr.string(default='ref')
    },
    outputs = {'lib': '%{name}.dll'}
)

csharp_library = rule(
    implementation = _lib_impl,
    attrs = _COMMON_ATTRS + {'_target_type': attr.string(default='lib')},
    outputs = {'lib': '%{name}.dll', 'doc': '%{name}.xml'}
)

csharp_binary = rule(
    implementation = _bin_impl,
    attrs = _COMMON_ATTRS + {'_target_type': attr.string(default='bin')},
    outputs = {'bin': '%{name}.exe'},
    executable = True
)
