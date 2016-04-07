#!/bin/bash
# pre-commit.sh

# Install using:
#   ln -s ../../tools/pre-commit.sh .git/hooks/pre-commit

exec 1>&2

result=`git diff --cached | grep $'\xEF\xBB\xBF'`

if [ "$result" ]; then
  echo "error: UTF-16 byte order mark(s) found in commit."
  echo "hint: Remove them using 'tools/strip-bom.sh' and try again."
  exit 1
fi

exit 0
