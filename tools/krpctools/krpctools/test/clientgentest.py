import json
from pkg_resources import resource_string


class ClientGenTestCase(object):
    def run_test(self, service_name, name):
        macro_template = resource_string(
            'krpctools.clientgen', self.language+'.tmpl').decode('utf-8')
        defs = json.loads(resource_string(
            'krpctools.test', name+'.json').decode('utf-8'))
        g = self.generator(
            macro_template, service_name, defs[service_name])
        actual = g.generate()
        expected = resource_string(
            'krpctools.test',
            'clientgen-'+name+'-'+self.language+'.txt').decode('utf-8')
        self.assertEqual(expected, actual)

    def test_empty(self):
        self.run_test('EmptyService', 'Empty')

    def test_test_service(self):
        self.run_test('TestService', 'TestService')
