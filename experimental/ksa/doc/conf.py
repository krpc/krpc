# Sphinx configuration for the kRPC for KSA documentation. The Bazel build fills in the
# version and generates the API pages beside it. The theme settings follow the kRPC
# documentation's.

import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "_ext"))

project = "kRPC"
copyright = "2015-2026 kRPC Org"
version = "@VERSION@"
release = version
html_title = f"kRPC v{release} documentation"

extensions = [
    "sphinx.ext.extlinks",
    "sphinx_design",
    "sphinx_tabs.tabs",
    "sphinx_lua_ls",
    "sphinx_csharp",
    "sphinx_java",
    "grpcdomain",
]
suppress_warnings = ["design.fa-build"]

# The spellchecker of the kRPC documentation, for the spelling builder (sphinx-build -t
# spelling)
if tags.has("spelling"):  # noqa: F821  # pyright: ignore[reportUndefinedVariable]
    import spelling_hunspell  # pyright: ignore[reportMissingImports]

    spelling_hunspell.enable(extensions)

# The kRPC documentation's words, with the KSA ones. The changelog page is dense with API
# identifiers, so it is left out.
spelling_word_list_filename = "dictionary.txt"
spelling_ignore_contributor_names = False
spelling_exclude_patterns = ["changelog.rst"]

# The Lua API is written with the domain's directives, so sphinx-lua-ls needs no language
# server to analyze Lua sources
lua_ls_backend = "disable"

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

javadoc_url_map = {
    "org.javatuples": (
        "https://javadoc.io/static/org.javatuples/javatuples/1.2/",
        "javadoc",
    ),
}

# The C-nano references, as in the kRPC documentation. The C domain scopes a function to the
# type it is documented under, so a reference from another type's page finds no target.
nitpick_ignore_regex = [
    ("c:func", ".+"),
    ("c:macro", ".+"),
]

# The fixed-width integer types, and the client and core service types the generated API
# pages name. The kRPC documentation covers the clients, KRPC and StdLib.
nitpick_ignore = [
    ("c:identifier", "int32_t"),
    ("c:identifier", "krpc_connection_t"),
    ("c:identifier", "krpc_error_t"),
    ("c:identifier", "krpc_dictionary_string_object_t"),
    ("c:identifier", "krpc_list_object_t"),
    ("c:identifier", "krpc_list_string_t"),
    ("c:identifier", "krpc_list_tuple_double_double_double_t"),
    ("c:identifier", "krpc_tuple_double_double_double_t"),
    ("c:identifier", "krpc_tuple_double_double_double_double_t"),
    ("c:identifier", "krpc_tuple_float_float_t"),
    ("c:identifier", "krpc_StdLib_ColorRGB_t"),
    ("cpp:identifier", "int32_t"),
    ("cpp:identifier", "krpc"),
    ("cpp:identifier", "krpc::Connection"),
    ("cpp:identifier", "krpc::RemoteService"),
    ("cpp:identifier", "StdLib"),
    ("cpp:identifier", "StdLib::ColorRGB"),
    ("cs:type", "IConnection"),
    ("cs:type", "StdLib.ColorRGB"),
    ("java:type", "Connection"),
    ("java:type", "StdLib"),
    ("java:type", "StdLib.ColorRGB"),
    ("js:class", "StdLib.ColorRGB"),
    ("py:class", "StdLib.ColorRGB"),
]

toc_object_entries = False
add_module_names = False
sphinx_tabs_disable_tab_closing = True


def setup(app):
    # sphinx-lua-ls warns that apidoc needs a language server whenever the backend is
    # disabled, even with no apidoc roots configured
    import logging

    logging.getLogger("sphinx.sphinx_lua_ls").addFilter(
        lambda record: (
            not record.getMessage().startswith(
                "apidoc requested, but the language server backend is disabled"
            )
        )
    )
