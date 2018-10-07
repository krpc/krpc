#!/bin/bash

# Run the Travis CI tests locally using Docker
ROOT="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd $ROOT/../..
bazel clean
rm -rf deploy
docker run -t -i \
  -v `pwd`:/build/krpc \
  -e local_user_id=`id -u ${USER}` \
  -e "TRAVIS_BRANCH=`git branch | grep \* | cut -d ' ' -f2-`" \
  -e "TRAVIS_PULL_REQUEST=false" \
  -e "TRAVIS_JOB_NUMBER=1" \
  krpc/buildenv:1.11.0 \
  /build/krpc/tools/travis-ci/script-docker.sh
