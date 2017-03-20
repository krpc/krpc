_MCS = 'mcs'
_MCS_FLAGS = ['-noconfig', '-nostdlib']

def _ref_impl(ctx):
    input = ctx.file.file
    output = ctx.outputs.lib

    ctx.action(
        mnemonic = 'CSharpReference',
        inputs = [input],
        outputs = [output],
        command = 'cp "%s" "%s"' % (input.path, output.path)
    )

    return struct(
        name = ctx.label.name,
        target_type = ctx.attr._target_type,
        lib = output,
        out = output
    )

def _csc_args(srcs, deps, exe=None, lib=None, doc=None, optimize=True, warn=4, nowarn=[], define=[], warnaserror=True):
    if exe: target_type = 'exe'
    if lib: target_type = 'library'
    args = ['-target:%s' % target_type, '-debug']
    if optimize:
        args.extend(['-optimize+'])
    else:
        args.extend(['-optimize-'])
    args.extend(_MCS_FLAGS)
    if exe: args.append('-out:%s' % exe.path)
    if lib: args.append('-out:%s' % lib.path)
    if doc: args.append('-doc:%s' % doc.path)
    args.append('-warn:%d' % warn)
    if warnaserror: args.append('-warnaserror+')
    if len(nowarn) > 0: args.append('-nowarn:%s' % ','.join(nowarn))
    if len(define) > 0: args.append('-define:%s' % ','.join(define))
    args.extend([x.path for x in srcs])
    args.extend(['-reference:%s' % x.path for x in deps])
    return args

def _lib_impl(ctx):
    lib_output = ctx.outputs.lib
    doc_output = ctx.outputs.doc
    mdb_output = ctx.outputs.mdb
    outputs = [lib_output, doc_output, mdb_output]
    srcs = ctx.files.srcs
    deps = [dep.lib for dep in ctx.attr.deps]
    if ctx.attr.nunit_test:
        deps += ctx.files._nunit_exe_libs + ctx.files._nunit_framework
    inputs = srcs + deps


    cmd = ctx.attr.csc
    args = _csc_args(
        srcs, deps, lib=lib_output, doc=doc_output, optimize=ctx.attr.optimize,
        warn=ctx.attr.warn, nowarn=ctx.attr.nowarn, warnaserror=ctx.attr.warnaserror, define=ctx.attr.define)

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
        doc = doc_output,
        mdb = mdb_output,
        out = outputs
    )

def _bin_impl(ctx):
    bin_output = ctx.outputs.bin
    doc_output = ctx.outputs.doc
    mdb_output = ctx.outputs.mdb
    outputs = [bin_output, doc_output, mdb_output]
    srcs = ctx.files.srcs
    deps = [dep.lib for dep in ctx.attr.deps]
    inputs = srcs + deps

    cmd = ctx.attr.csc
    args = _csc_args(
        srcs, deps, exe=bin_output, doc=doc_output, optimize=ctx.attr.optimize,
        warn=ctx.attr.warn, nowarn=ctx.attr.nowarn, warnaserror=ctx.attr.warnaserror, define=ctx.attr.define)

    ctx.action(
        mnemonic = 'CSharpCompile',
        inputs = inputs,
        outputs = outputs,
        command = '%s %s' % (cmd, ' '.join(args))
    )

    runfiles = outputs + ctx.files.deps
    runfile_dir = '$0.runfiles/krpc'
    tmp_dir = '$$.run'
    sub_commands = ['mkdir -p %s/%s' % (runfile_dir, tmp_dir)]
    for dep in runfiles:
        sub_commands.append('ln -f -s ../%s %s/%s/%s' % (dep.short_path, runfile_dir, tmp_dir, dep.basename))
    sub_commands.append('/usr/bin/mono %s/%s/%s "$@" %s' % (runfile_dir, tmp_dir, bin_output.basename, ' '.join(ctx.attr.runargs)))
    ctx.file_action(
        ctx.outputs.executable,
        ' && \\\n'.join(sub_commands)+'\n'
    )
    runfiles = ctx.runfiles(files = runfiles)

    return struct(
        name = ctx.label.name,
        target_type = ctx.attr._target_type,
        bin = bin_output,
        doc = doc_output,
        mdb = mdb_output,
        out = outputs,
        runfiles = runfiles
    )

def _nunit_impl(ctx):
    lib = ctx.attr.lib.lib
    nunit_files = [ctx.file._nunit_exe] + ctx.files._nunit_exe_libs + ctx.files._nunit_framework
    runfiles = nunit_files + [lib] + ctx.files.deps
    sub_commands = []
    for dep in runfiles:
        sub_commands.append('ln -f -s %s %s' % (dep.short_path, dep.basename))
    sub_commands.extend([
        '/usr/bin/mono --debug %s %s "$@" 2>stderr.txt' % (ctx.file._nunit_exe.basename, lib.basename),
        'RESULT=$?',
        'cat stderr.txt',
        '(if grep "FATAL UNHANDLED EXCEPTION" stderr.txt; then exit 1; fi)',
        'exit $RESULT'
    ])
    ctx.file_action(
        ctx.outputs.executable,
        ' ; \\\n'.join(sub_commands)+'\n'
    )

    return struct(
        name = ctx.label.name,
        runfiles = ctx.runfiles(files = runfiles)
    )

def _gendarme_impl(ctx):
    if ctx.attr.lib:
        src = ctx.attr.lib.lib
        mdb = ctx.attr.lib.mdb
    else:
        src = ctx.attr.exe.bin
        mdb = ctx.attr.exe.mdb
    runfiles = [src, mdb, ctx.file.config]
    if ctx.attr.ignores:
        runfiles.append(ctx.file.ignores)

    args = ['--severity=all', '--confidence=all']
    args.append('--config=%s' % ctx.file.config.short_path)
    args.append('--set=%s' % ctx.attr.ruleset)
    if ctx.attr.ignores:
        args.append('--ignore=%s' % ctx.file.ignores.short_path)
    args.append(src.short_path)

    ctx.file_action(
        ctx.outputs.executable,
        'MONO_OPTIONS="--debug" /usr/bin/gendarme %s\n' % ' '.join(args)
    )

    return struct(
        name = ctx.label.name,
        runfiles = ctx.runfiles(files = runfiles)
    )

def _assembly_info_impl(ctx):
    content = ['using System;', 'using System.Reflection;', 'using System.Runtime.InteropServices;']
    if len(ctx.attr.internals_visible_to) > 0:
        content.append('using System.Runtime.CompilerServices;')
    for x in ctx.attr.using:
        content.append('using %s;' % x)
    content.append('[assembly: AssemblyTitle ("%s")]' % ctx.attr.title)
    content.append('[assembly: AssemblyDescription ("%s")]' % ctx.attr.description)
    content.append('[assembly: AssemblyCopyright ("%s")]' % ctx.attr.copyright)
    content.append('[assembly: AssemblyVersion ("%s")]' % ctx.attr.version)

    for pkg in ctx.attr.internals_visible_to:
        content.append('[assembly: InternalsVisibleTo ("%s")]' % pkg)

    for k,v in ctx.attr.custom.items():
        content.append('[assembly: %s (%s)]' % (k,v))

    if ctx.attr.cls_compliant:
        cls_compliant = 'true'
    else:
        cls_compliant = 'false'
    content.append('[assembly: CLSCompliant (%s)]' % cls_compliant)

    if ctx.attr.com_visible:
        com_visible = 'true'
    else:
        com_visible = 'false'
    content.append('[assembly: ComVisible (%s)]' % com_visible)

    ctx.file_action(
        output = ctx.outputs.out,
        content = '\n'.join(content)+'\n'
    )

_COMMON_ATTRS = {
    'csc': attr.string(default=_MCS),
    'deps': attr.label_list(providers=['out', 'lib', 'target_type']),
    'srcs': attr.label_list(allow_files=FileType(['.cs'])),
    'optimize': attr.bool(default=True),
    'warn': attr.int(default=4),
    'nowarn': attr.string_list(),
    'warnaserror': attr.bool(default=True),
    'define': attr.string_list()
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
    attrs = _COMMON_ATTRS + {
        '_target_type': attr.string(default='lib'),
        'nunit_test': attr.bool(default=False),
        '_nunit_exe_libs': attr.label(default=Label('@csharp_nunit//:nunit_exe_libs'), allow_files=True),
        '_nunit_framework': attr.label(default=Label('@csharp_nunit//:nunit_framework'), allow_files=True)
},
    outputs = {'lib': '%{name}.dll', 'doc': '%{name}.xml', 'mdb': '%{name}.dll.mdb'}
)

csharp_binary = rule(
    implementation = _bin_impl,
    attrs = _COMMON_ATTRS + {'runargs': attr.string_list(), '_target_type': attr.string(default='bin')},
    outputs = {'bin': '%{name}.exe', 'doc': '%{name}.xml', 'mdb': '%{name}.exe.mdb'},
    executable = True
)

csharp_nunit_test = rule(
    implementation = _nunit_impl,
    attrs = {
        'lib': attr.label(mandatory=True, providers=['out', 'lib', 'target_type']),
        'deps': attr.label_list(providers=['out', 'lib', 'target_type']),
        '_nunit_exe': attr.label(default=Label('@csharp_nunit//:nunit_exe'), allow_files=True, single_file=True),
        '_nunit_exe_libs': attr.label(default=Label('@csharp_nunit//:nunit_exe_libs'), allow_files=True),
        '_nunit_framework': attr.label(default=Label('@csharp_nunit//:nunit_framework'), allow_files=True)
    },
    test = True
)

csharp_gendarme_test = rule(
    implementation = _gendarme_impl,
    attrs = {
        'lib': attr.label(allow_files=True),
        'exe': attr.label(allow_files=True),
        'config': attr.label(default=Label('//tools/build:csharp_gendarme_rules.xml'),
                             allow_files=True, single_file=True),
        'ruleset': attr.string(default='default'),
        'ignores': attr.label(allow_files=True, single_file=True)
    },
    test = True
)

csharp_assembly_info = rule(
    implementation = _assembly_info_impl,
    attrs= {
        'title': attr.string(mandatory=True),
        'description': attr.string(),
        'copyright': attr.string(mandatory=True),
        'version': attr.string(mandatory=True),
        'using': attr.string_list(),
        'custom': attr.string_dict(),
        'internals_visible_to': attr.string_list(),
        'cls_compliant': attr.bool(default=True),
        'com_visible': attr.bool(default=False)
    },
    outputs = {'out': '%{name}.cs'}
)

def _nuget_package_out(id, version):
    return {'out': '%s.%s.nupkg' % (id, version)}

def _nuget_package_impl(ctx):
    assembly = ctx.attr.assembly.lib
    doc = ctx.attr.assembly.doc
    nuspec = ctx.new_file(assembly, assembly.basename.replace('.dll','.nuspec'))

    nuspec_contents = [
        '<?xml version="1.0"?>',
        '<package>',
        '<metadata>',
        '  <id>%s</id>' % ctx.attr.id,
        '  <version>%s</version>' % ctx.attr.version,
        '  <authors>%s</authors>' % ctx.attr.author,
        '  <description>%s</description>' % ctx.attr.description
    ]
    if ctx.attr.project_url:
        nuspec_contents.append('  <projectUrl>%s</projectUrl>' % ctx.attr.project_url)
    if ctx.attr.project_url:
        nuspec_contents.append('  <licenseUrl>%s</licenseUrl>' % ctx.attr.license_url)
    nuspec_contents.extend([
        '  <frameworkAssemblies>'
    ])
    nuspec_contents.extend(['    <frameworkAssembly assemblyName="%s" targetFramework="net40"/>' % x for x in ctx.attr.framework_deps])
    nuspec_contents.extend([
        '  </frameworkAssemblies>',
        '  <dependencies>'
    ])
    nuspec_contents.extend(['    <dependency id="%s" version="%s"/>' % x for x in ctx.attr.deps.items()])
    nuspec_contents.extend([
        '  </dependencies>',
        '</metadata>',
        '<files>',
        '  <file src="%s" target="lib/net40" />' % assembly.basename,
        '  <file src="%s" target="lib/net40" />' % doc.basename,
        '</files>',
        '</package>'
    ])

    ctx.file_action(
        output = nuspec,
        content = '\n'.join(nuspec_contents)
    )

    tmpdir = '%s.nuget-package-tmp' % assembly.basename
    sub_commands = [
        'mkdir -p %s' % tmpdir,
        'cp %s %s' % (nuspec.path, tmpdir),
        'cp %s %s' % (assembly.path, tmpdir),
        'cp %s %s' % (doc.path, tmpdir),
        'mono %s pack %s -BasePath %s -Verbosity quiet' % (ctx.file._nuget_exe.path, tmpdir+'/'+nuspec.basename, tmpdir),
        'cp %s %s' % (ctx.outputs.out.basename, ctx.outputs.out.path)
    ]

    ctx.action(
        mnemonic = 'NuGetPackage',
        inputs = [ctx.file._nuget_exe, assembly, doc, nuspec],
        outputs = [ctx.outputs.out],
        command = ' && '.join(sub_commands),
        execution_requirements = {'local': '1'} # FIXME: nuget.exe does not work with the sandbox
    )

nuget_package = rule(
    implementation = _nuget_package_impl,
    attrs = {
        'id': attr.string(mandatory=True),
        'assembly': attr.label(providers=['out', 'lib', 'doc', 'target_type']),
        'version': attr.string(mandatory=True),
        'author': attr.string(mandatory=True),
        'project_url': attr.string(),
        'license_url': attr.string(),
        'description': attr.string(mandatory=True),
        'framework_deps': attr.string_list(),
        'deps': attr.string_dict(),
        '_nuget_exe': attr.label(default=Label('@csharp_nuget//file'), allow_files=True, single_file=True)
    },
    outputs = _nuget_package_out
)
