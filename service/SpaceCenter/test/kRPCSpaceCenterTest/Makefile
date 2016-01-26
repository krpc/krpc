.PHONY: all env2 env3 test test2 test3 clean

all: test

env2: env2/bin/activate
env2/bin/activate:
	test -d env2 || virtualenv -p python2 env2
	bazel build //client/python
	env2/bin/pip install ../../../../bazel-bin/client/python/krpc-*.zip
	touch env2/bin/activate

env3: env3/bin/activate
env3/bin/activate:
	test -d env3 || virtualenv -p python3 env3
	bazel build //client/python
	env3/bin/pip install ../../../../bazel-bin/client/python/krpc-*.zip
	touch env3/bin/activate

test: test2 test3

test2: env2
	env2/bin/python test.py

test3: env3
	env3/bin/python test.py

clean:
	find . -name "*.pyc" -exec rm -rf {} \;
	rm -rf env2 env3 __pycache__
