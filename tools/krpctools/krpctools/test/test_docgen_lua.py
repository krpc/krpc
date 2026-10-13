import unittest

from krpctools.docgen.lua import LuaDomain
from krpctools.test.docgentest import DocGenTestCase


class TestDocGenLua(DocGenTestCase, unittest.TestCase):
    language = "lua"
    domain = LuaDomain
    maxDiff = None


if __name__ == "__main__":
    unittest.main()
