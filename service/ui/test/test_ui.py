import unittest
import krpctest


class TestUI(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.ui = cls.connect().ui
        cls.message_position = cls.ui.MessagePosition

    def test_message(self):
        self.ui.message('One')
        self.ui.message('Two', 5)
        self.ui.message('Three', 1, self.message_position.top_right)


if __name__ == '__main__':
    unittest.main()
