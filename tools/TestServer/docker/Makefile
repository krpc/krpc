.PHONY: build test deploy

VERSION=`(cd ../../..; ./tools/krpc-version.sh)`

build:
	bazel build //tools/TestServer:archive
	rm -f TestServer.zip
	cp ../../../bazel-bin/tools/TestServer/TestServer-$(VERSION).zip ./TestServer.zip
	docker build -t ghcr.io/krpc/testserver:$(VERSION) .
	docker tag ghcr.io/krpc/testserver:$(VERSION) ghcr.io/krpc/testserver:latest
	rm -f TestServer.zip

test:
	docker run -t -i -p 50000:50000 -p 50001:50001 ghcr.io/krpc/testserver:$(VERSION)

deploy:
	docker push ghcr.io/krpc/testserver:$(VERSION)
	docker push ghcr.io/krpc/testserver:latest
