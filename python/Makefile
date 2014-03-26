MARKDOWN = markdown
HTML2TEXT = html2text

.PHONY: all protobuf test dist release clean

all: dist

protobuf:
	make -C .. protobuf-python

test:
	(cd test; ./run.sh)

dist: protobuf
	$(MARKDOWN) README.md | $(HTML2TEXT) -rcfile ../tools/html2textrc > README.txt

	python setup.py sdist --formats=gztar,zip

release: protobuf
	python setup.py register
	python setup.py sdist --formats=gztar,zip upload

clean:
	make -C .. protobuf-python-clean
	find . -name "*.pyc" -exec rm -rf {} \;
	-rm -f test/server.log
	-rm -rf dist README.txt MANIFEST
