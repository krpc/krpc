#!/bin/bash
set -ev

ROOT="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
docker run -t -i \
  -v `pwd`:/build/krpc \
  -e local_user_id=`id -u ${USER}` \
  -e "TRAVIS_BRANCH=${TRAVIS_BRANCH}" \
  -e "TRAVIS_PULL_REQUEST=${TRAVIS_PULL_REQUEST}" \
  -e "TRAVIS_JOB_NUMBER=${TRAVIS_JOB_NUMBER}" \
  krpc/buildenv:`cat $ROOT/../docker/VERSION.txt` \
  /build/krpc/tools/travis-ci/script-docker.sh
