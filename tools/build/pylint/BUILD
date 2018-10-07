load('//tools/build:python.bzl', 'py_script')

py_script(
  name = 'pylint',
  script = 'pylint',
  pkg = '@python_pylint//file',
  deps = [
      '@python_configparser//file',
      '@python_backports_functools_lru_cache//file',
      '@python_six//file',
      '@python_isort//file',
      '@python_mccabe//file',
      '@python_enum34//file',
      '@python_singledispatch//file',
      '@python_astroid//file',
      '@python_wrapt//file',
      '@python_lazy_object_proxy//file',
      '@python_futures//file'
  ],
  visibility = ['//visibility:public']
)
