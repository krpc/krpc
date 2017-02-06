#!/usr/bin/env python2

""" Auto-build and auto-serve the docs """

import pyinotify
import os
import subprocess
import sys

port = sys.argv[1]
src = sys.argv[2]
out = sys.argv[3]

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

targets = [line.strip() for line in
           subprocess.check_output(['bazel', 'query', 'kind(file, deps(//doc:srcs))']).split('\n')
           if len(line) > 0]
paths = targets_to_paths(targets)

# Auto-serve the docs
p = subprocess.Popen(['sphinx-autobuild', '-W', '-n', '-T', '-H', '0.0.0.0', '-p', sys.argv[1], sys.argv[2], sys.argv[3]])

# Auto-build the docs when a file changes
class ChangeHandler(pyinotify.ProcessEvent):
    def process_default(self, event):
        subprocess.check_call(['bazel', 'build', '//doc:srcs'])
watch_manager = pyinotify.WatchManager()
notifier = pyinotify.Notifier(watch_manager, default_proc_fun=ChangeHandler())
for path in paths:
    watch_manager.add_watch(path, pyinotify.IN_MODIFY)
notifier.loop()
