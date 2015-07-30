class Option(object):
    def __init__(self, env, option, value):
        self.env = env
        self.option = option
        self.value = value

    def __call__(self, indent=0):
        value = ' '.join(self.value.split('\n'))
        return (' '*indent) + (':%s: %s' % (self.option, value)).rstrip()

class Param(Option):
    def __init__(self, env, name, desc, pos, info, attrs):
        name = env.domain.parse_param(name)
        option = 'param %s %s' % (env.parse_parameter_type(pos, info['type'], attrs), name)
        super(Param, self).__init__(env, option, desc)

class Returns(Option):
    def __init__(self, env, desc):
        super(Returns, self).__init__(env, 'returns', desc)

class ReturnType(Option):
    def __init__(self, env, typ, attrs):
        super(ReturnType, self).__init__(env, 'rtype', env.parse_return_type(typ, attrs))

class ReadOnlyProperty(Option):
    def __init__(self, env):
        super(ReadOnlyProperty, self).__init__(env, 'Property', 'Read-only, cannot be set')

class WriteOnlyProperty(Option):
    def __init__(self, env):
        super(WriteOnlyProperty, self).__init__(env, 'Property', 'Write-only, cannot be read')

class ReadWriteProperty(Option):
    def __init__(self, env):
        super(ReadWriteProperty, self).__init__(env, 'Property', 'Can be read or written')
