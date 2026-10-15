import unittest

import krpctest
from krpc.services.stdlib import ColorRGBA


class TestUI(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.ui = cls.connect().ui
        cls.message_position = cls.ui.MessagePosition

    def test_message(self):
        self.ui.message("One")
        self.ui.message("Two", 5)
        self.ui.message("Three", 1, self.message_position.top_right)
        self.ui.message(
            "Four", 1, self.message_position.top_center, ColorRGBA(1, 0, 0, 0.5)
        )


if __name__ == "__main__":
    unittest.main()
