import json
import tempfile
from pkg_resources import resource_string, resource_filename
from ..docgen.nodes import Service
from ..docgen import process_file


class DocGenTestCase(object):
    def run_test(self, service_name, name):
        defs = json.loads(resource_string(
            'krpctools.test', name+'.json').decode('utf-8'))

        def sort(member):
            return member.fullname

        def parse_service_info(info):
            del info['id']
            keys = ('procedures', 'classes', 'enumerations',
                    'exceptions', 'documentation')
            for key in keys:
                if key not in info:
                    if key == 'documentation':
                        value = ''
                    else:
                        value = {}
                    info[key] = value
            return info

        services = {name: Service(name, sort=sort, **parse_service_info(info))
                    for name, info in defs.iteritems()}

        rst_content = [
            '.. default-domain:: {{ domain.sphinxname }}',
            '.. highlight:: {{ domain.highlight }}',
            '',
            '{{ domain.currentmodule(\'%s\') }}' % service_name,
            '{% import domain.macros as macros with context %}',
            '',
            '{{ macros.service(services[\'%s\']) }}' % service_name
        ]
        for cls in defs[service_name]['classes'].keys():
            rst_content.append(
                "{{ macros.class(services['%s'].classes['%s']) }}"
                % (service_name, cls))
        for enm in defs[service_name]['enumerations'].keys():
            rst_content.append(
                "{{ macros.enumeration(services['%s'].enumerations['%s']) }}"
                % (service_name, enm))
        for exn in defs[service_name]['exceptions'].keys():
            rst_content.append(
                "{{ macros.exception(services['%s'].exceptions['%s']) }}"
                % (service_name, exn))

        path = tempfile.mktemp()
        with open(path, 'w') as fp:
            fp.write('\n'.join(rst_content))

        macros = resource_filename(
            'krpctools.docgen', '%s.tmpl' % self.language)
        domain = self.domain(macros)

        actual, _ = process_file(domain, services, path)

        # with open('/home/alex/workspaces/ksp/krpc/' +
        #           'tools/krpctools/krpctools/test/' +
        #           'docgen-'+name+'-'+self.language+'.rst', 'w') as f:
        #     f.write(actual)

        expected = resource_string(
            'krpctools.test',
            'docgen-'+name+'-'+self.language+'.rst').decode('utf-8')
        self.assertEqual(expected, actual)

    def test_empty(self):
        self.run_test('EmptyService', 'Empty')

    def test_test_service(self):
        self.run_test('TestService', 'TestService')
