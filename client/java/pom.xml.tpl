<?xml version="1.0" encoding="UTF-8"?>
<project xmlns="http://maven.apache.org/POM/4.0.0"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd">
    <modelVersion>4.0.0</modelVersion>

    <groupId>{groupId}</groupId>
    <artifactId>{artifactId}</artifactId>
    <version>{version}</version>
    <packaging>jar</packaging>

    <name>kRPC Java client</name>
    <description>Java client for kRPC, a Remote Procedure Call server for Kerbal Space Program</description>
    <url>https://krpc.github.io/krpc/</url>

    <licenses>
        <license>
            <name>GNU Lesser General Public License v3.0 or later</name>
            <url>https://www.gnu.org/licenses/lgpl-3.0.txt</url>
        </license>
    </licenses>

    <developers>
        <developer>
            <id>djungelorm</id>
            <name>djungelorm</name>
            <url>https://github.com/djungelorm</url>
        </developer>
    </developers>

    <scm>
        <connection>scm:git:https://github.com/krpc/krpc.git</connection>
        <developerConnection>scm:git:ssh://git@github.com/krpc/krpc.git</developerConnection>
        <url>https://github.com/krpc/krpc</url>
    </scm>

    <issueManagement>
        <system>GitHub</system>
        <url>https://github.com/krpc/krpc/issues</url>
    </issueManagement>

    <dependencies>
{dependencies}
    </dependencies>
</project>
