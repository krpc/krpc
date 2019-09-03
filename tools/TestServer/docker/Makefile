.PHONY: build test deploy

VERSION=`(cd ../../..; ./tools/krpc-version.sh)`

build:
	bazel build //tools/TestServer:archive
	rm -f TestServer.zip
	cp ../../../bazel-bin/tools/TestServer/TestServer-$(VERSION).zip ./TestServer.zip
	docker build -t krpc/testserver .
	rm -f TestServer.zip
	docker tag krpc/testserver krpc/testserver:$(VERSION)

test:
	docker run -t -i -p 50000:50000 -p 50001:50001 krpc/testserver:$(VERSION)

deploy:
	docker push krpc/testserver
	docker push krpc/testserver:$(VERSION)
