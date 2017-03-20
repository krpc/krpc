.PHONY: build deploy

VERSION=1.0.0

build:
	docker build \
		--build-arg ksp_version=`(cd ../..; tools/ksp-version.sh)` \
		-t krpc/buildenv .
	docker tag krpc/buildenv krpc/buildenv:$(VERSION)

deploy:
	docker push krpc/buildenv
	docker push krpc/buildenv:$(VERSION)
