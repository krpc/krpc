load('/tools/build/python', 'py_script')

py_script(
  name = 'pylint',
  script = 'pylint',
  pkg = '@python_pylint//file',
  deps = [
      '@python_six//file',
      '@python_pylint//file',
      '@python_astroid//file',
      '@python_wrapt//file',
      '@python_lazy_object_proxy//file'
  ],
  visibility = ['//visibility:public']
)
