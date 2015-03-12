project = 'kRPC'
version = '0.1'
release = '0.1.4'
copyright = '2015, djungelorm'

master_doc = 'index'
source_suffix = '.rst'
extensions = ['sphinx.ext.mathjax']
templates_path = ['_templates']
exclude_patterns = []

pygments_style = 'sphinx'
import sphinx_rtd_theme
html_theme = 'sphinx_rtd_theme'
html_theme_path = [sphinx_rtd_theme.get_html_theme_path()]
html_static_path = ['_static']
htmlhelp_basename = 'krpc-doc'
