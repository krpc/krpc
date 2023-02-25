class Language:
    def __init__(self):
        self.module = None

    def parse_name(self, name):
        if hasattr(self, 'keywords') and name in self.keywords:
            return '%s_' % name
        return name

    def parse_type(self, typ):  # pylint: disable=unused-argument
        raise NotImplementedError

    def parse_default_value(
            self, value, typ):  # pylint: disable=unused-argument
        return None
