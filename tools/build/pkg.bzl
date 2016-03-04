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

def _stage_files_impl(ctx):
    outs = []
    for src in ctx.files.srcs:
        path = ctx.label.name + '/' + _apply_path_map(ctx.attr.path_map, src.short_path)
        out = ctx.new_file(ctx.configuration.genfiles_dir, path)

        sub_commands = ['ln -f -r -s %s %s' % (src.path, out.path)]

        ctx.action(
            mnemonic = 'StageFile',
            inputs = [src],
            outputs = [out],
            command = ' && '.join(sub_commands)
        )
        outs.append(out)

    return struct(files = set(outs))

stage_files = rule(
    implementation = _stage_files_impl,
    attrs = {
        'srcs': attr.label_list(allow_files=True),
        'path_map': attr.string_dict()
    }
)
