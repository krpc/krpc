from directives import *
import krpc.types
from utils import snake_case

class DefaultDomain(object):

    def __init__(self, env):
        self.env = env

    def parse_value(self, x, typ=None):
        if isinstance(typ, krpc.types.ClassType):
            result = 'null'
        else:
            result = str(x)
            if type(x) == float and result.endswith('.0'):
                result = result[:-2]
        if result in self.value_map:
            return self.value_map[result]
        return result

    def parse_type(self, typ):
        if typ in self.type_map:
            return '%s' % self.type_map[typ]
        if typ.startswith('Class('):
            return ':class:`%s`' % self.env.shorten_ref(typ[6:-1])
        if typ.startswith('Enum('):
            return ':class:`%s`' % self.env.shorten_ref(typ[5:-1])
        if typ.startswith('Tuple('):
            subtyps = [self.parse_type(x) for x in typ[6:-1].split(',')]
            return 'tuple of (%s)' % ', '.join(subtyps)
        if typ.startswith('Dictionary('):
            param = typ[11:-1]
            key,value = param.split(',')
            return 'dict from %s to %s' % (self.parse_type(key), self.parse_type(value))
        if typ.startswith('List('):
            return 'list of %s' % self.parse_type(typ[5:-1])
        return typ

    def parse_type_ref(self, typ):
        if typ in self.type_map:
            return '%s' % self.type_map[typ]
        if typ.startswith('Class('):
            return '%s' % self.env.shorten_ref(typ[6:-1])
        if typ.startswith('Enum('):
            return '%s' % self.env.shorten_ref(typ[5:-1])
        if typ.startswith('Tuple('):
            return 'tuple'
        if typ.startswith('Dictionary('):
            param = typ[11:-1]
            key,value = param.split(',')
            return 'dict'
        if typ.startswith('List('):
            return 'list'
        return typ

    def parse_ref(self, name):
        obj = self.env.get_ref(name)

        if isinstance(obj, Service) or isinstance(obj, Class) or isinstance(obj, Enumeration):
            prefix = 'class'
        elif isinstance(obj, StaticMethod) or isinstance(obj, ClassMethod) or isinstance(obj, ClassStaticMethod):
            prefix = 'meth'
        elif isinstance(obj, Property) or isinstance(obj, ClassProperty) or isinstance(obj, EnumerationValue):
            prefix = 'attr'
        elif obj == None:
            prefix = 'ref'
        else:
            raise RuntimeError('Unknown type for ref %s' % str(type(obj)))

        typ,_,name = name.partition(':')
        name,_,_ = name.partition('(')
        name = self.env.shorten_ref(name).split('.')
        if typ == 'M':
            name[-1] = snake_case(name[-1])
        ref = '.'.join(name)

        return ':%s:`%s`' % (prefix, ref)

    def parse_param(self, name):
        return snake_case(name)

    def parse_member(self, name):
        return snake_case(name)
