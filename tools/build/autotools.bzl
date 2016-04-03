def _apply_path_map(path_map, path):
    """ Apply the path mappings to a path.
        Replaces the longest prefix match from the mapping. """
    matchlen = 0
    match = path
    for x,y in path_map.items():
        if path.startswith(x):
            if len(x) > matchlen:
                match = y + path[len(x):]
                matchlen = len(x)
    return match

def _get_mode(mode_map, path):
    """ Get the mode for a file using the mode mapping. """
    matchlen = 0
    match = '0644'
    for x,y in mode_map.items():
        if path.startswith(x):
            if len(x) > matchlen:
                match = y
                matchlen = len(x)
    return match

def _impl(ctx):
    output = ctx.outputs.out
    inputs = ctx.files.files
    macros = ctx.files.macros
    source_dir = ctx.attr.source_dir
    path_map = ctx.attr.path_map
    mode_map = ctx.attr.mode_map

    sub_commands = []

    # Copy and chmod all the files to a staging directory
    # to get the required directory structure and permissions
    # (Note: can't use symlinking as we need to set permissions)
    staging_dir = output.basename + '.package-tmp'
    for input in inputs:
        staging_path = staging_dir + '/' + _apply_path_map(path_map, input.short_path)
        mode = _get_mode(mode_map, input.short_path)
        sub_commands.extend([
            'mkdir -p `dirname "%s"`' % staging_path,
            'cp %s %s' % (input.path, staging_path),
            'chmod %s %s' % (mode, staging_path)
        ])

    sub_commands.extend([
        'CWD=`pwd`',
        'cd %s' % staging_dir,
        '(cd %s; autoreconf --force --install --warnings=error 2>/dev/null 1>&2)' % source_dir,
        'rm -rf %s/autom4te.cache' % source_dir,
        'zip --quiet -r $CWD/%s ./' % output.path
    ])

    ctx.action(
        inputs = inputs + macros,
        outputs = [output],
        progress_message = 'Running autotools and creating package %s' % output.short_path,
        command = ' &&\\\n'.join(sub_commands)
    )

autotools_dist = rule(
    implementation = _impl,
    attrs = {
        'files': attr.label_list(allow_files=True, mandatory=True, non_empty=True),
        'macros': attr.label_list(allow_files=True),
        'source_dir': attr.string(mandatory=True),
        'path_map': attr.string_dict(),
        'mode_map': attr.string_dict(),
        'out': attr.output(mandatory=True)
    }
)
