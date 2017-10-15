java_binary(
  name = 'checkstyle',
  main_class = 'com.puppycrawl.tools.checkstyle.Main',
    runtime_deps = [
        '@java_checkstyle//jar',
        '@java_antlr2//jar',
        '@java_antlr4_runtime//jar',
        '@java_guava//jar',
        '@java_apache_commons_beanutils//jar',
        '@java_apache_commons_cli//jar',
        '@java_apache_commons_collections//jar',
        '@java_apache_commons_logging//jar'
    ],
  visibility = ['//visibility:public']
)

exports_files(['google_checks.xml', 'default.properties'])
