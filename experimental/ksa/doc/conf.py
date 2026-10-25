# Sphinx configuration for the kRPC for KSA documentation. docs.sh copies this
# directory into bin/doc-src, and generates the API pages beside it. The theme settings
# follow the kRPC documentation's.

import os

project = "kRPC"
copyright = "2015-2026 kRPC Org"
version = os.environ.get("KSA_DOC_VERSION", "")
release = version
html_title = f"kRPC v{release} documentation"

extensions = [
    "sphinx_design",
    "sphinx_tabs.tabs",
    "sphinxcontrib.luadomain",
    "sphinx_csharp",
    "javasphinx",
]
suppress_warnings = ["design.fa-build"]

exclude_patterns = ["api/api.tmpl", "order.txt"]

html_theme = "pydata_sphinx_theme"
html_static_path = ["_static"]
html_css_files = ["custom.css"]
html_js_files = ["external-links.js"]
html_theme_options = {
    "logo": {"text": "kRPC"},
    "header_links_before_dropdown": 6,
    "navbar_end": ["theme-switcher", "navbar-icon-links"],
    "icon_links": [
        {
            "name": "GitHub",
            "url": "https://github.com/krpc/krpc/tree/main/experimental/ksa",
            "icon": "fa-brands fa-github",
        },
    ],
}

toc_object_entries = False
add_module_names = False
sphinx_tabs_disable_tab_closing = True
