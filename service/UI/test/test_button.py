import unittest
import krpc
import krpctest


class TestButton(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.canvas = cls.connect().ui.stock_canvas

    def test_button(self):
        button = self.canvas.add_button('Foo')
        self.assertIsNotNone(button.rect_transform)
        self.assertTrue(button.visible)
        self.assertIsNotNone(button.text)
        self.assertEqual('Foo', button.text.content)
        self.assertFalse(button.clicked)
        self.wait()
        button.remove()
        self.assertRaises(krpc.client.RPCError, button.remove)


if __name__ == '__main__':
    unittest.main()
