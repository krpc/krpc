.PHONY: build deploy

VERSION=1.8.0

build:
	tar -cf ./krpc.tar ../.. --exclude="bazel-*" --exclude=.git*
	docker build \
		--build-arg ksp_version=`(cd ../..; tools/ksp-version.sh)` \
		-t krpc/buildenv .
	rm krpc.tar
	docker tag krpc/buildenv krpc/buildenv:$(VERSION)

test:
	docker run -t -i krpc/buildenv:$(VERSION)

deploy:
	docker push krpc/buildenv
	docker push krpc/buildenv:$(VERSION)
