.PHONY: build test deploy

# The version of the image, as in: make build VERSION=3.18.0
ifndef VERSION
$(error set VERSION to the version of the image, as in: make build VERSION=3.18.0)
endif

build:
	docker build -t ghcr.io/krpc/buildenv:$(VERSION) .

test:
	docker run -t --user 1001:121 -i ghcr.io/krpc/buildenv:$(VERSION)

deploy:
	docker push ghcr.io/krpc/buildenv:$(VERSION)
	docker tag ghcr.io/krpc/buildenv:$(VERSION) ghcr.io/krpc/buildenv:latest
	docker push ghcr.io/krpc/buildenv:latest
