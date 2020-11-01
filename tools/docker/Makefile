.PHONY: build test deploy

VERSION=`cat VERSION.txt`

build:
	tar --exclude="bazel-*" --exclude=".git*" --exclude="tools/docker/krpc.tar*" \
	    -cf ./krpc.tar ../..
	docker build \
	  --build-arg ksp_version=`(cd ../..; tools/ksp-version.sh)` \
          --build-arg bazel_version=`(cd ../..; tools/bazel-version.sh)` \
	  -t krpc/buildenv .
	rm krpc.tar
	docker tag krpc/buildenv krpc/buildenv:$(VERSION)

test:
	docker run -t -i krpc/buildenv:$(VERSION)

deploy:
	docker push krpc/buildenv
	docker push krpc/buildenv:$(VERSION)
