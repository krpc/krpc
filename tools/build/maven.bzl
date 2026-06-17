" Maven Central repository rule "

def _impl(ctx):
    parts = ctx.attr.artifact.split(":")
    group_id = parts[0]
    artifact_id = parts[1]
    version = parts[2]
    group_path = group_id.replace(".", "/")
    jar_name = "%s-%s.jar" % (artifact_id, version)
    url = "https://repo1.maven.org/maven2/%s/%s/%s/%s" % (
        group_path,
        artifact_id,
        version,
        jar_name,
    )

    ctx.download(
        url = url,
        output = "jar/" + jar_name,
        sha256 = ctx.attr.sha256,
    )

    ctx.file("jar/BUILD.bazel", """
load("@rules_java//java:defs.bzl", "java_import")

java_import(
    name = "jar",
    jars = ["{jar_name}"],
    visibility = ["//visibility:public"],
)
""".format(jar_name = jar_name))

maven_jar = repository_rule(
    implementation = _impl,
    attrs = {
        "artifact": attr.string(mandatory = True),
        "sha256": attr.string(mandatory = True),
    },
)
