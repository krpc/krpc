load('//tools/build:python.bzl', 'py_script')

py_script(
  name = 'pylint',
  script = 'pylint',
  pkg = '@python_pylint//file',
  deps = [
      '@python_astroid//file',
      '@python_dill//file',
      '@python_isort//file',
      '@python_lazy_object_proxy//file',
      '@python_mccabe//file',
      '@python_platformdirs//file',
      '@python_tomli//file',
      '@python_tomlkit//file',
      '@python_typing_extensions//file',
      '@python_wrapt//file'
  ],
  visibility = ['//visibility:public']
)
