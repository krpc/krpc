# Sphinx configuration for the kRPC for KSA documentation. docs.sh copies this
# directory into bin/doc-src, and generates the API pages beside it.
import os

project = "kRPC for KSA"
copyright = "2015-2026 kRPC Org"
version = os.environ.get("KSA_DOC_VERSION", "")
release = version

extensions = [
    "sphinx_tabs.tabs",
    "sphinxcontrib.luadomain",
    "sphinx_csharp",
    "javasphinx",
]

exclude_patterns = ["api/api.tmpl", "order.txt"]
html_theme = "pydata_sphinx_theme"
html_title = "kRPC for KSA"
html_theme_options = {"navigation_with_keys": False}
html_sidebars = {"**": []}
