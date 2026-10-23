"""The LaTeX subset used by the <math> and <unit> tags of service documentation.

to_latex renders the markup for the API reference, and rejects anything outside the subset.
to_text renders it as Unicode text for client documentation, and never fails: markup outside
the subset comes back as its source, with the backslashes dropped from command names.

A unit is set upright in LaTeX, and a '.' in a unit joins two factors, as in 'N.m'. To extend
the subset, add a symbol to SYMBOLS, or a command taking arguments to _ARGUMENTS and to both
renderers.
"""

from __future__ import annotations

import re

# The commands that stand for one symbol, each with its LaTeX and its Unicode text
SYMBOLS: dict[str, tuple[str, str]] = {
    "cdot": ("\\cdot", "·"),
    "times": ("\\times", "×"),
    "pm": ("\\pm", "±"),
    "le": ("\\le", "≤"),
    "ge": ("\\ge", "≥"),
    "approx": ("\\approx", "≈"),
    "infty": ("\\infty", "∞"),
    "degree": ("{}^{\\circ}", "°"),
}

# Greek letters, lower case then the capitals LaTeX names
_GREEK = {
    "alpha": "α",
    "beta": "β",
    "gamma": "γ",
    "delta": "δ",
    "epsilon": "ϵ",
    "varepsilon": "ε",
    "zeta": "ζ",
    "eta": "η",
    "theta": "θ",
    "vartheta": "ϑ",
    "iota": "ι",
    "kappa": "κ",
    "lambda": "λ",
    "mu": "μ",
    "nu": "ν",
    "xi": "ξ",
    "pi": "π",
    "rho": "ρ",
    "sigma": "σ",
    "tau": "τ",
    "upsilon": "υ",
    "phi": "ϕ",
    "varphi": "φ",
    "chi": "χ",
    "psi": "ψ",
    "omega": "ω",
    "Gamma": "Γ",
    "Delta": "Δ",
    "Theta": "Θ",
    "Lambda": "Λ",
    "Xi": "Ξ",
    "Pi": "Π",
    "Sigma": "Σ",
    "Upsilon": "Υ",
    "Phi": "Φ",
    "Psi": "Ψ",
    "Omega": "Ω",
}
SYMBOLS.update((name, ("\\" + name, char)) for name, char in _GREEK.items())

# The commands that take braced arguments, and how many
_ARGUMENTS = {"frac": 2, "sqrt": 1, "text": 1}

_SUPERSCRIPTS = dict(
    zip(
        "0123456789+-=()abcdefghijklmnoprstuvwxyz",
        "⁰¹²³⁴⁵⁶⁷⁸⁹⁺⁻⁼⁽⁾ᵃᵇᶜᵈᵉᶠᵍʰⁱʲᵏˡᵐⁿᵒᵖʳˢᵗᵘᵛʷˣʸᶻ",
        strict=True,
    )
)

_SUBSCRIPTS = dict(
    zip(
        "0123456789+-=()aehijklmnoprstuvx",
        "₀₁₂₃₄₅₆₇₈₉₊₋₌₍₎ₐₑₕᵢⱼₖₗₘₙₒₚᵣₛₜᵤᵥₓ",
        strict=True,
    )
)

_FRACTIONS = {
    ("1", "2"): "½",
    ("1", "3"): "⅓",
    ("2", "3"): "⅔",
    ("1", "4"): "¼",
    ("3", "4"): "¾",
    ("1", "8"): "⅛",
}

# The characters allowed outside a command, besides letters and digits
_PLAIN = set(" +-=(),/.'|!:;*[]<>")

_TOKEN = re.compile(r"\\([A-Za-z]+)|\\(.?)|([{}^_])|([^\\{}^_]+)")


class Command:
    def __init__(self, name: str, args: list[list[Node]]) -> None:
        self.name = name
        self.args = args


class Script:
    """A superscript, marked '^', or a subscript, marked '_'."""

    def __init__(self, mark: str, body: list[Node]) -> None:
        self.mark = mark
        self.body = body


Node = str | Command | Script


def to_latex(source: str, unit: bool = False) -> str:
    """The markup as LaTeX. Throws ValueError if it is outside the subset."""
    nodes = _Parser(source).parse()
    # Markup that renders for the reference must render for clients too
    try:
        _render_text(nodes, unit)
    except ValueError as error:
        raise ValueError("%s in '%s'" % (error, source)) from error
    latex = _render_latex(nodes, unit).strip()
    return "\\mathrm{%s}" % latex if unit else latex


def problem(source: str, unit: bool = False) -> str | None:
    """Why the markup is outside the subset, or None if it is in it."""
    try:
        to_latex(source, unit)
    except ValueError as error:
        return str(error)
    return None


def to_text(source: str, unit: bool = False) -> str:
    """The markup as Unicode text, or its source with plain command names if it is outside
    the subset."""
    try:
        return _render_text(_Parser(source).parse(), unit)
    except ValueError:
        return re.sub(r"\\([A-Za-z]+)", r"\1", source)


class _Parser:
    def __init__(self, source: str) -> None:
        self.source = source
        self.tokens: list[tuple[str, str]] = []
        for match in _TOKEN.finditer(source):
            command, escape, special, plain = match.groups()
            if command:
                self.tokens.append(("command", command))
            elif escape is not None:
                self.error("unsupported command '\\%s'" % escape)
            elif special:
                self.tokens.append((special, special))
            else:
                self.tokens.append(("plain", plain))
        self.position = 0

    def error(self, message: str) -> None:
        raise ValueError("%s in '%s'" % (message, self.source))

    def parse(self) -> list[Node]:
        return self.group(closing=False)

    def group(self, closing: bool) -> list[Node]:
        nodes: list[Node] = []
        while self.position < len(self.tokens):
            kind, value = self.tokens[self.position]
            self.position += 1
            if kind == "}":
                if not closing:
                    self.error("unbalanced '}'")
                return nodes
            if kind == "{":
                nodes.extend(self.group(closing=True))
            elif kind in ("^", "_"):
                nodes.append(Script(kind, self.argument()))
            elif kind == "command":
                nodes.append(self.command(value))
            else:
                for char in value:
                    if not (char.isalnum() or char in _PLAIN):
                        self.error("unsupported character '%s'" % char)
                nodes.append(value)
        if closing:
            self.error("unbalanced '{'")
        return nodes

    def argument(self) -> list[Node]:
        """A braced group, or else the next character or command."""
        while self.position < len(self.tokens):
            kind, value = self.tokens[self.position]
            if kind == "{":
                self.position += 1
                return self.group(closing=True)
            if kind == "command":
                self.position += 1
                return [self.command(value)]
            if kind != "plain":
                break
            value = value.lstrip()
            if not value:
                self.position += 1
                continue
            if len(value) == 1:
                self.position += 1
            else:
                self.tokens[self.position] = ("plain", value[1:])
            return [value[0]]
        self.error("missing argument")
        return []

    def command(self, name: str) -> Node:
        if name in _ARGUMENTS:
            args = [self.argument() for _ in range(_ARGUMENTS[name])]
            if name == "text" and not all(isinstance(node, str) for node in args[0]):
                self.error("\\text holding markup")
            return Command(name, args)
        if name not in SYMBOLS:
            self.error("unsupported command '\\%s'" % name)
        return Command(name, [])


def _render_latex(nodes: list[Node], unit: bool) -> str:
    result = ""
    for node in nodes:
        if isinstance(node, str):
            if unit:
                node = node.replace(".", " \\cdot ")
            # A letter straight after a command name would extend the name
            if node[:1].isalpha() and re.search(r"\\[A-Za-z]+$", result):
                result += " "
            result += node
        elif isinstance(node, Script):
            result += "%s{%s}" % (node.mark, _render_latex(node.body, unit).strip())
        elif node.name in SYMBOLS:
            result += SYMBOLS[node.name][0]
        elif node.name == "text":
            result += "\\text{%s}" % "".join(str(part) for part in node.args[0])
        else:
            result += "\\" + node.name
            result += "".join(
                "{%s}" % _render_latex(arg, unit).strip() for arg in node.args
            )
    return result


def _render_text(nodes: list[Node], unit: bool) -> str:
    result = ""
    for node in nodes:
        if isinstance(node, str):
            result += node.replace(".", "·") if unit else node
        elif isinstance(node, Script):
            body = _render_text(node.body, unit)
            table = _SUPERSCRIPTS if node.mark == "^" else _SUBSCRIPTS
            for char in body:
                if char not in table:
                    kind = "superscript" if node.mark == "^" else "subscript"
                    raise ValueError("'%s' has no Unicode %s form" % (char, kind))
                result += table[char]
        elif node.name in SYMBOLS:
            result += SYMBOLS[node.name][1]
        elif node.name == "text":
            result += "".join(str(part) for part in node.args[0])
        elif node.name == "frac":
            numerator, denominator = (_render_text(arg, unit) for arg in node.args)
            fraction = _FRACTIONS.get((numerator, denominator))
            result += fraction or "%s/%s" % (_operand(numerator), _operand(denominator))
        else:
            result += "√" + _operand(_render_text(node.args[0], unit))
    return result


def _operand(text: str) -> str:
    """The text, parenthesized unless it is a single term."""
    return text if re.fullmatch(r"\w+", text) else "(%s)" % text
