from .generator import Generator
from ..lang.csharp import CsharpLanguage


class CsharpGenerator(Generator):

    language = CsharpLanguage()

    @staticmethod
    def parse_documentation(documentation):
        documentation = documentation \
            .replace('<doc>', '').replace('</doc>', '').strip()
        if documentation == '':
            return ''
        lines = ['/// '+line for line in documentation.split('\n')]
        content = '\n'.join(line.rstrip() for line in lines)
        content = content.replace('  <param', '<param')
        content = content.replace('  <returns', '<returns')
        content = content.replace('  <remarks', '<remarks')
        return content

    def generate_context_parameters(self, procedure):
        parameters = super(CsharpGenerator, self) \
            .generate_context_parameters(procedure)
        for parameter in parameters:
            if 'default_value' not in parameter:
                parameter['name_value'] = parameter['name']
                continue
            typ = parameter['type']
            default_value = parameter['default_value']
            if typ.startswith('systemAlias::Tuple') or \
               typ.startswith('global::System.Collections.Generic.IList') or \
               typ.startswith('genericCollectionsAlias::ISet') or \
               typ.startswith('global::System.Collections' +
                              '.Generic.IDictionary'):
                parameter['name_value'] = '%s ?? %s' % \
                                          (parameter['name'], default_value)
                parameter['default_value'] = 'null'
            else:
                parameter['name_value'] = parameter['name']
        return parameters

    @staticmethod
    def parse_context(context):
        return context
