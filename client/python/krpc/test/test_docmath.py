import unittest

from krpc.docmath import problem, to_latex, to_text


class TestDocMath(unittest.TestCase):
    def test_units(self) -> None:
        cases = [
            ("m/s^2", "\\mathrm{m/s^{2}}", "m/s\u00b2"),
            ("kg/m^3", "\\mathrm{kg/m^{3}}", "kg/m\u00b3"),
            ("N.m", "\\mathrm{N \\cdot m}", "N\u00b7m"),
            ("kg.m^2", "\\mathrm{kg \\cdot m^{2}}", "kg\u00b7m\u00b2"),
            (
                "m^3.s^{-2}",
                "\\mathrm{m^{3} \\cdot s^{-2}}",
                "m\u00b3\u00b7s\u207b\u00b2",
            ),
            ("N(m/kg)^2", "\\mathrm{N(m/kg)^{2}}", "N(m/kg)\u00b2"),
            ("\\degree/s", "\\mathrm{{}^{\\circ}/s}", "\u00b0/s"),
            ("kg", "\\mathrm{kg}", "kg"),
        ]
        for source, latex, text in cases:
            self.assertEqual(latex, to_latex(source, unit=True))
            self.assertEqual(text, to_text(source, unit=True))

    def test_math(self) -> None:
        cases = [
            ("2\\pi", "2\\pi", "2\u03c0"),
            ("Q", "Q", "Q"),
            ("1.5", "1.5", "1.5"),
            (
                "\\frac{1}{2} \\rho v^2",
                "\\frac{1}{2} \\rho v^{2}",
                "\u00bd \u03c1 v\u00b2",
            ),
            ("\\frac{a + b}{c}", "\\frac{a + b}{c}", "(a + b)/c"),
            ("\\sqrt{x}", "\\sqrt{x}", "\u221ax"),
            ("\\sqrt{x + y}", "\\sqrt{x + y}", "\u221a(x + y)"),
            ("\\omega_{max}", "\\omega_{max}", "\u03c9\u2098\u2090\u2093"),
            ("\\mu = GM", "\\mu = GM", "\u03bc = GM"),
            ("\\text{air density}", "\\text{air density}", "air density"),
            ("x \\le y", "x \\le y", "x \u2264 y"),
        ]
        for source, latex, text in cases:
            self.assertEqual(latex, to_latex(source))
            self.assertEqual(text, to_text(source))

    def test_invalid(self) -> None:
        cases = [
            ("\\foo", "foo"),
            ("x^q", "x^q"),
            ("{a", "{a"),
            ("a}", "a}"),
            ("\\frac{1}", "frac{1}"),
            ("\\text{\\pi}", "text{pi}"),
            ("a & b", "a & b"),
        ]
        for source, text in cases:
            with self.assertRaises(ValueError):
                to_latex(source)
            self.assertIsNotNone(problem(source))
            self.assertEqual(text, to_text(source))

    def test_no_problem(self) -> None:
        self.assertIsNone(problem("m/s^2", unit=True))
        self.assertIsNone(problem("\\frac{1}{2} \\rho v^2"))


if __name__ == "__main__":
    unittest.main()
