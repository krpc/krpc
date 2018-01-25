class Language(object):

    def __init__(self):
        self.module = None

    def parse_name(self, name):
        if hasattr(self, 'keywords') and name in self.keywords:
            return '%s_' % name
        return name

    def parse_type(  # pylint: disable=no-self-use
            self, typ):  # pylint: disable=unused-argument
        raise NotImplementedError

    def parse_default_value(  # pylint: disable=no-self-use
            self, value, typ):  # pylint: disable=unused-argument
        return None
