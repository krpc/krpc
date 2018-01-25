import unittest
from krpctools.test.docgentest import DocGenTestCase
from krpctools.docgen.lua import LuaDomain


class TestDocGenLua(DocGenTestCase, unittest.TestCase):
    language = 'lua'
    domain = LuaDomain


if __name__ == '__main__':
    unittest.main()
