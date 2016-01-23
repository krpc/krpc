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

def _impl(ctx):
    output = ctx.outputs.out
    inputs = ctx.files.files
    path_map = ctx.attr.path_map

    # Symlink all the files to a staging directory
    # to get the required directory structure in the archive
    staging_dir = output.basename + '.package-tmp'
    staging_dir_path = output.path.replace(
        ctx.configuration.bin_dir.path, ctx.configuration.genfiles_dir.path) + '.package-tmp'
    staging_inputs = []
    for input in inputs:
        staging_path = staging_dir + '/' + _apply_path_map(path_map, input.short_path)
        staging_file = ctx.new_file(ctx.configuration.genfiles_dir, staging_path)

        ctx.action(
            mnemonic = 'PackageFile',
            inputs = [input],
            outputs = [staging_file],
            command = 'ln -f -r -s %s %s' % (input.path, staging_file.path)
        )
        staging_inputs.append(staging_file)

    # Generate a zip file from the staging directory
    ctx.action(
        inputs = staging_inputs,
        outputs = [output],
        progress_message = 'Packaging files into %s' % output.short_path,
        command = '(CWD=`pwd` && cd %s && zip --quiet -r $CWD/%s ./)' % (staging_dir_path, output.path)
    )

package_archive = rule(
    implementation = _impl,
    attrs = {
        'files': attr.label_list(allow_files=True, mandatory=True, non_empty=True),
        'path_map': attr.string_dict(),
        'out': attr.output(mandatory=True)
    }
)
