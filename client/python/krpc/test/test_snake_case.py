import unittest
from krpc.utils import snake_case as t


class TestSnakeCase(unittest.TestCase):
    def test_examples(self):
        # Simple cases
        self.assertEqual('server', t('Server'))
        self.assertEqual('my_server', t('MyServer'))

        # With numbers
        self.assertEqual('int32_to_string', t('Int32ToString'))
        self.assertEqual('32_to_string', t('32ToString'))
        self.assertEqual('to_int32', t('ToInt32'))

        # With multiple capitals
        self.assertEqual('https', t('HTTPS'))
        self.assertEqual('http_server', t('HTTPServer'))
        self.assertEqual('my_http_server', t('MyHTTPServer'))
        self.assertEqual('http_server_ssl', t('HTTPServerSSL'))

        # With underscores
        self.assertEqual('_http_server', t('_HTTPServer'))
        self.assertEqual('http__server', t('HTTP_Server'))

    def test_non_camel_case_examples(self):
        self.assertEqual('foobar', t('foobar'))
        self.assertEqual('foo__bar', t('foo_bar'))
        self.assertEqual('_foobar', t('_foobar'))

if __name__ == '__main__':
    unittest.main()
