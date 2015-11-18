protobuf:
	make -C .. protobuf-python

test: protobuf bin
	$(PYTHON2) setup.py test
	$(PYTHON3) setup.py test

dist: protobuf bin
	$(PYTHON2) setup.py sdist --formats=gztar,zip

release: protobuf bin
	$(PYTHON2) setup.py sdist --formats=gztar,zip register upload
