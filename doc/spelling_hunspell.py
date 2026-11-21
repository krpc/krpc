"""The spellchecker of the documentation sites' spelling builds.

sphinxcontrib-spelling checks words through pyenchant, which loads the Enchant
library at import. We register the enchant package without running its
__init__, so only pyenchant's tokenizer loads, which is pure Python. Words are
checked with spylls, a Python port of Hunspell, against the dictionary the
build fetches.
"""

import importlib.util
import os
import sys
import types


class HunspellDictWithPWL:
    """The part of enchant.DictWithPWL that sphinxcontrib-spelling uses."""

    def __init__(self, lang, pwl):
        from spylls.hunspell import Dictionary, readers
        from spylls.hunspell.readers.file_reader import FileReader

        aff, context = readers.read_aff(FileReader(os.environ["KRPC_SPELLING_AFF"]))
        dic = readers.read_dic(
            FileReader(os.environ["KRPC_SPELLING_DIC"], encoding=context.encoding),
            aff=aff,
            context=context,
        )
        self.dictionary = Dictionary(aff, dic)
        with open(pwl, encoding="utf-8") as words:
            self.words = {word.strip() for word in words if word.strip()}
        self.checked = {}

    def check(self, word):
        if word not in self.checked:
            self.checked[word] = self.in_words(word) or self.dictionary.lookup(word)
        return self.checked[word]

    # Enchant's casing rule for a personal word list: an entry also matches its
    # capitalized and upper case forms
    def in_words(self, word):
        if word in self.words:
            return True
        if word.istitle() and word.lower() in self.words:
            return True
        return word.isupper() and (
            word.lower() in self.words or word.capitalize() in self.words
        )

    def suggest(self, word):
        return list(self.dictionary.suggest(word))


def enable(extensions):
    """Add sphinxcontrib-spelling to the extensions, checking words with spylls."""
    spec = importlib.util.find_spec("enchant")
    assert spec is not None and spec.submodule_search_locations is not None
    enchant = types.ModuleType("enchant")
    enchant.__path__ = spec.submodule_search_locations
    sys.modules["enchant"] = enchant
    enchant.DictWithPWL = HunspellDictWithPWL  # pyright: ignore[reportAttributeAccessIssue]
    extensions.append("sphinxcontrib.spelling")
