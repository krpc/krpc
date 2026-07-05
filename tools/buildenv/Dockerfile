FROM ubuntu:24.04
LABEL maintainer="djungelorm <djungelorm@users.noreply.github.com>"

# Install essential packages. python3 + pip remain only for the system nanopb
# generator used by the C-nano CMake consumer-experience jobs (the Bazel build
# uses the hermetic rules_python toolchain); the -dev/-venv/is-python3 packages
# are no longer needed.
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    curl ca-certificates gnupg git wget gosu \
    python3 python3-pip && \
    apt-get clean

# Install Bazel
RUN wget https://github.com/bazelbuild/bazelisk/releases/download/v1.29.0/bazelisk-linux-amd64 -O /usr/local/bin/bazel && \
    chmod +x /usr/local/bin/bazel
COPY bazelrc /etc/bazel.bazelrc
RUN chmod 0644 /etc/bazel.bazelrc
ENV BAZELISK_HOME=/usr/local/share/bazelisk
RUN USE_BAZEL_VERSION=9.1.1 bazel version && chmod -R a+rwX /usr/local/share/bazelisk

# rules_dotnet downloads its own hermetic .NET SDK and runtime (resolved through
# the test runfiles), so no system dotnet is installed.

# Install mono. Still required by the build-csharp-sln CI job, which builds the
# generated Visual Studio solution (KRPC.sln) with mono's msbuild -- a
# consumer-experience check independent of the Bazel build (which is off mono).
RUN gpg --keyserver hkp://keyserver.ubuntu.com:80 \
        --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF && \
      gpg --export 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF \
        > /etc/apt/keyrings/mono.gpg && \
      echo "deb [signed-by=/etc/apt/keyrings/mono.gpg] https://download.mono-project.com/repo/ubuntu stable-focal main" | \
        tee /etc/apt/sources.list.d/mono-official-stable.list && \
      apt-get update && \
      apt-get install -y mono-complete && \
      apt-get clean

# Install latex
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    latexmk make texlive-latex-base texlive-latex-recommended texlive-fonts-recommended \
    texlive-latex-extra tex-gyre libenchant-2-2 && \
    apt-get clean

# Install other build tools. Dropped vs earlier images: cppcheck (replaced by
# the hermetic clang-tidy), maven and openjdk-11-jdk (rules_jvm_external
# downloads jars and Bazel uses remotejdk_11 + its own embedded JDK). gcc/g++/
# ccache/cmake/librsvg2-bin/luarocks/socat stay for the CMake consumer jobs and
# the Linux-only lua/image/serialio rules.
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    gcc g++ ccache librsvg2-bin cmake luarocks socat graphviz && \
    apt-get clean

# Install buildifier
RUN apt-get update && \
    apt-get install -y --no-install-recommends golang && \
    apt-get clean && \
    git clone https://github.com/bazelbuild/buildtools.git --depth 1 --branch v7.1.2 && \
    (cd buildtools/buildifier; GOBIN=/usr/local/bin go install)

# Install ASIO
RUN apt-get update && \
    apt-get install -y libasio-dev && \
    apt-get clean

# Install Protobuf
ARG PROTOBUF_VERSION=35.1
RUN mkdir /protobuf && \
    cd /protobuf && \
    wget -O protobuf.tar.gz \
      https://github.com/protocolbuffers/protobuf/releases/download/v${PROTOBUF_VERSION}/protobuf-${PROTOBUF_VERSION}.tar.gz && \
    tar xf protobuf.tar.gz && \
    cd protobuf-${PROTOBUF_VERSION} && \
    cmake -S . -B build \
      -DCMAKE_BUILD_TYPE=Release \
      -Dprotobuf_BUILD_TESTS=OFF \
      -DCMAKE_INSTALL_PREFIX=/usr/local && \
    cmake --build build -j$(nproc) && \
    cmake --install build && \
    ldconfig && \
    cd / && \
    rm -rf /protobuf*

# Install python-protobuf via pip, for the system nanopb generator used by the
# C-nano CMake consumer jobs (needs runtime_version, protobuf >= 4.25). The
# Bazel build's nanopb generator runs on the hermetic pip hub instead.
RUN pip3 install --break-system-packages protobuf

# Install Nanopb
ARG NANOPB_VERSION=0.4.9.1
RUN mkdir /nanopb && \
    cd /nanopb && \
    wget -O nanopb.tar.gz \
      https://github.com/nanopb/nanopb/archive/refs/tags/${NANOPB_VERSION}.tar.gz && \
    tar xf nanopb.tar.gz && \
    cd nanopb-${NANOPB_VERSION} && \
    cmake -S . -B build \
      -DCMAKE_BUILD_TYPE=Release \
      -Dnanopb_BUILD_GENERATOR=ON \
      -DCMAKE_INSTALL_PREFIX=/usr/local && \
    cmake --build build -j$(nproc) && \
    cmake --install build && \
    ldconfig && \
    cd / && \
    rm -rf /nanopb*

WORKDIR /build

# Add github actions user
ARG UID=1001
ARG GID=121
RUN groupadd -g $GID runner
RUN useradd -mr -d /home/runner -u $UID -g $GID runner
RUN usermod -aG sudo runner
