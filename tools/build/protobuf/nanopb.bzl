def _create_py_env(out, install):
    tmp = out+'.tmp-create-py-env'
    cmds = [
        'rm -rf %s' % tmp,
        'virtualenv %s --python python3 --quiet --never-download --no-site-packages' % tmp
    ]
    for lib in install:
        cmds.append(
            '%s/bin/python %s/bin/pip install --quiet --no-deps --no-cache-dir file:`pwd`/%s'
            % (tmp, tmp, lib.path))
    cmds.extend([
        '(CWD=`pwd`; cd %s; tar -c -f $CWD/%s *)' % (tmp, out)
    ])
    return cmds

def _extract_py_env(env, path):
    return [
        'rm -rf %s' % path,
        'mkdir -p %s' % path,
        '(CWD=`pwd`; cd %s; tar -xf $CWD/%s)' % (path, env)
    ]

def _impl(ctx):
    input = ctx.file.src
    options = ctx.file.options
    header = ctx.outputs.header
    source = ctx.outputs.source
    include = ctx.attr.include
    protoc = ctx.file._protoc
    protoc_nanopb_env = ctx.file._protoc_nanopb_env
    protoc_nanopb = ctx.files._protoc_nanopb
    protoc_nanopb_dir = protoc_nanopb[0].dirname+'/generator'
    protoc_nanopb_opts = '-Q \\"#include \\\\\\"'+include+'/%s\\\\\\"\\" ' + \
                         '-L \\"#include <'+include+'/%s>\\"'

    protoc_input = source.path + '.src-proto-nanopb'
    protoc_output = source.path + '.tmp-proto-nanopb'
    pyenv = source.path + '.tmp-proto-nanopb-env'

    sub_commands = _extract_py_env(protoc_nanopb_env.path, pyenv)
    sub_commands.extend([
        'rm -rf %s %s' % (protoc_input, protoc_output),
        'mkdir -p %s' % protoc_input,
        'mkdir -p %s' % protoc_output,
        'cp %s %s' % (input.path, protoc_input),
        'cp %s %s' % (options.path, protoc_input),
        'PATH=%s/bin:%s:$PATH %s "--nanopb_out=%s:%s" %s/%s' % (pyenv, protoc_nanopb_dir, protoc.path, protoc_nanopb_opts, protoc_output, protoc_input, input.basename),
        'cp %s/%s/*.pb.h %s' % (protoc_output, protoc_input, header.path),
        'cp %s/%s/*.pb.c %s' % (protoc_output, protoc_input, source.path)
        #'sed -i \'s/#include ".\\+"/#include "%s"/g\' %s' % (include.replace('/', '\\/'), source.path)
    ])

    ctx.actions.run_shell(
        inputs = [input, options, protoc, protoc_nanopb_env] + protoc_nanopb,
        outputs = [header, source],
        mnemonic = 'ProtobufNanopb',
        command = ' && \\\n'.join(sub_commands)+'\n'
    )

protobuf_nanopb = rule(
    implementation = _impl,
    attrs = {
        'src': attr.label(allow_single_file=['.proto']),
        'options': attr.label(allow_single_file=['.options']),
        'header': attr.output(mandatory=True),
        'source': attr.output(mandatory=True),
        'include': attr.string(mandatory=True),
        '_protoc': attr.label(default=Label('//tools/build/protobuf:protoc'), allow_single_file=True),
        '_protoc_nanopb': attr.label(default=Label('@protoc_nanopb//:plugin'), allow_files=True),
        '_protoc_nanopb_env': attr.label(default=Label('//tools/build/protobuf:protoc-nanopb-env'), allow_single_file=True)
    },
    output_to_genfiles = True
)

def _env_impl(ctx):
    pylibs = [ctx.file._protobuf, ctx.file._six]
    setup = ctx.actions.declare_file('protoc-nanopb-setup')
    ctx.actions.write(
        output = setup,
        content = ' && \\\n'.join(_create_py_env(ctx.outputs.out.path, pylibs))+'\n',
        is_executable = True
    )
    ctx.actions.run(
        inputs = pylibs,
        outputs = [ctx.outputs.out],
        progress_message = 'Setting up protoc-nanopb python environment',
        executable = setup,
        use_default_shell_env = True
    )

protoc_nanopb_env = rule(
    implementation = _env_impl,
    attrs = {
        '_protobuf': attr.label(default=Label('@python_protobuf//file'), allow_single_file=True),
        '_six': attr.label(default=Label('@python_six//file'), allow_single_file=True)
    },
    outputs = {'out': '%{name}.tar'},
    output_to_genfiles = True
)
