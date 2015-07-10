project = 'kRPC'
version = '0.1'
release = '0.1.10'
copyright = '2015, djungelorm'

master_doc = 'index'
source_suffix = '.rst'
exclude_patterns = ['**/.#*']
extensions = ['sphinx.ext.mathjax', 'sphinxcontrib.spelling', 'sphinx.ext.todo']
templates_path = ['_templates']

pygments_style = 'sphinx'
import sphinx_rtd_theme
html_theme = 'sphinx_rtd_theme'
html_theme_path = [sphinx_rtd_theme.get_html_theme_path()]
htmlhelp_basename = 'krpc-doc'
html_static_path = ['crafts','scripts','_static']
html_context = { 'css_files': ['_static/custom.css'] }

todo_include_todos = True

spelling_word_list_filename = 'dictionary.txt'
