# Sphinx configuration for the kRPC for KSA documentation. docs.sh copies this
# directory into bin/doc-src, and generates the API pages beside it. The theme settings
# follow the kRPC documentation's.

import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "_ext"))

project = "kRPC"
copyright = "2015-2026 kRPC Org"
version = os.environ.get("KSA_DOC_VERSION", "")
release = version
html_title = f"kRPC v{release} documentation"

extensions = [
    "sphinx.ext.extlinks",
    "sphinx_design",
    "sphinx_tabs.tabs",
    "sphinxcontrib.luadomain",
    "sphinx_csharp",
    "javasphinx",
    "grpcdomain",
]
suppress_warnings = ["design.fa-build"]

extlinks = {"issue": ("https://github.com/krpc/krpc/issues/%s", "#%s")}

exclude_patterns = ["api", "order.txt"]

html_theme = "pydata_sphinx_theme"
html_static_path = ["_static"]
html_css_files = ["custom.css"]
html_js_files = ["external-links.js"]
html_favicon = "_static/logo-mark.svg"
html_theme_options = {
    # The logo in the header, with the lettering for each color mode
    "logo": {
        "image_light": "_static/logo-on-light.svg",
        "image_dark": "_static/logo-on-dark.svg",
        "alt_text": "kRPC",
    },
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
