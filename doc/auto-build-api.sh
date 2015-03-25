#!/bin/bash

# Watch the api directory and run 'make python-api' when a file changes

function block_for_change {
  inotifywait -r -e modify,move,create,delete api
}
function build {
  make python-api
}

build
while block_for_change; do
  build
done
