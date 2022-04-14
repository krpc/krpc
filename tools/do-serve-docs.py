#!/usr/bin/env python2

""" Auto-build and auto-serve the docs """

import filecmp
import os
import pyinotify
import shutil
import subprocess
import sys

port = sys.argv[1]
src = sys.argv[2]
stage = sys.argv[3]
out = sys.argv[4]


def targets_to_paths(targets):
    """ Converts a list of bazel targets to a list of file paths """
    result = []
    for target in targets:
        if target.startswith('//'):
            path = target[2:].replace(':', '/')
            if path[0] == '/':
                path = path[1:]
            if os.path.exists(path):
                result.append(path)
    return result


# Get paths to source files
targets = [line.strip() for line in
           subprocess.check_output([
               'bazel', 'query', 'kind(file, deps(//doc:srcs))']).split('\n')
           if len(line) > 0]
dependencies = set(targets_to_paths(targets))


# Change handler that builds using bazel then updates the stage directory
class UpdateStagedFiles(pyinotify.ProcessEvent):
    def process_default(self, event):
        # Rebuild the docs
        subprocess.check_call(['bazel', 'build', '//doc:srcs'])
        if not os.path.exists(stage):
            os.makedirs(stage)

        # Remove files from stage directory that are not in src directory
        for basepath, dirnames, filenames in os.walk(stage):
            for filename in filenames:
                path = os.path.relpath(os.path.join(basepath, filename), stage)
                if not os.path.exists(os.path.join(src, path)):
                    print('Removing', path)
                    os.unlink(os.path.join(stage, path))

        # Update stale files in stage directory
        for basepath, dirnames, filenames in os.walk(src):
            for filename in filenames:
                path = os.path.relpath(os.path.join(basepath, filename), src)

                srcpath = os.path.join(src, path)
                stagepath = os.path.join(stage, path)
                if not os.path.exists(stagepath):
                    print('Staging new file', path)
                    if not os.path.exists(os.path.dirname(stagepath)):
                        os.makedirs(os.path.dirname(stagepath))
                    shutil.copy(srcpath, stagepath)
                elif not filecmp.cmp(srcpath, stagepath):
                    print('Updating file', path)
                    os.unlink(stagepath)
                    shutil.copy(srcpath, stagepath)


# Do an initial update of the stage directory
UpdateStagedFiles().process_default(None)

# Auto-serve the docs
p = subprocess.Popen(['sphinx-autobuild', '-W', '-n', '-T',
                      '-H', '0.0.0.0', '-p', port, stage, out])

# Auto-update the stage directory when a dependency changes
watch_manager = pyinotify.WatchManager()
notifier = pyinotify.Notifier(
    watch_manager, default_proc_fun=UpdateStagedFiles())
for path in dependencies:
    watch_manager.add_watch(path, pyinotify.IN_MODIFY)
notifier.loop()
