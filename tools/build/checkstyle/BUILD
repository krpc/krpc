java_binary(
  name = 'checkstyle',
  main_class = 'com.puppycrawl.tools.checkstyle.Main',
    runtime_deps = [
        ':checkstyle_jar',
        ':antlr2',
        ':antlr4_runtime',
        ':guava',
        '@java_apache_commons_beanutils//:jar',
        '@java_apache_commons_cli//:jar',
        '@java_apache_commons_collections//:jar',
        '@java_apache_commons_logging//:jar'
    ],
  visibility = ['//visibility:public']
)

exports_files(['google_checks.xml', 'default.properties'])

java_import(
  name = 'checkstyle_jar',
  jars = ['@java_checkstyle//file']
)

java_import(
  name = 'antlr2',
  jars = ['@java_antlr2//file']
)

java_import(
  name = 'antlr4_runtime',
  jars = ['@java_antlr4_runtime//file']
)

java_import(
  name = 'guava',
  jars = ['@java_guava//file']
)
