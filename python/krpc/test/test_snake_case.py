#!/usr/bin/env python2

import unittest
from krpc.service import _to_snake_case as t

class TestSnakeCase(unittest.TestCase):

    def test_examples(self):
        # Simple cases
        self.assertEquals('server', t('Server'))
        self.assertEquals('my_server', t('MyServer'))

        # With numbers
        self.assertEquals('int32_to_string', t('Int32ToString'))
        self.assertEquals('32_to_string', t('32ToString'))
        self.assertEquals('to_int32', t('ToInt32'))

        # With multiple capitals
        self.assertEquals('https', t('HTTPS'))
        self.assertEquals('http_server', t('HTTPServer'))
        self.assertEquals('my_http_server', t('MyHTTPServer'))
        self.assertEquals('http_server_ssl', t('HTTPServerSSL'))

        # With underscores
        self.assertEquals('_http_server', t('_HTTPServer'))
        self.assertEquals('http__server', t('HTTP_Server'))


    def test_non_camel_case_examples(self):
        self.assertEquals('foobar', t('foobar'))
        self.assertEquals('foo__bar', t('foo_bar'))
        self.assertEquals('_foobar', t('_foobar'))


if __name__ == '__main__':
    unittest.main()
