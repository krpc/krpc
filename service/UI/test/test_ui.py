import unittest
import krpctest

class TestUI(krpctest.TestCase):

    @classmethod
    def setUp(cls):
        krpctest.new_save()
        cls.conn = krpctest.connect(cls)
        cls.ui = cls.conn.ui
        cls.message_position = cls.conn.ui.MessagePosition

    @classmethod
    def tearDown(cls):
        cls.conn.close()

    def test_message(self):
        self.ui.message("One")
        self.ui.message("Two", 5)
        self.ui.message("Three", 1, self.message_position.top_right)

if __name__ == '__main__':
    unittest.main()
