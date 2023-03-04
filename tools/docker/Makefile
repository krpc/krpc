.PHONY: build test deploy

VERSION=`cat VERSION.txt`

build:
	tar --exclude="bazel-*" --exclude=".git*" --exclude="tools/docker/krpc.tar*" \
	    -cf ./krpc.tar ../..
	docker build \
	  --build-arg ksp_version=`(cd ../..; tools/ksp-version.sh)` \
          --build-arg bazel_version=`(cd ../..; tools/bazel-version.sh)` \
	  -t ghcr.io/krpc/buildenv:$(VERSION) .
	rm krpc.tar

test:
	docker run -t -i ghcr.io/krpc/buildenv:$(VERSION)

deploy:
	docker push ghcr.io/krpc/buildenv:$(VERSION)
