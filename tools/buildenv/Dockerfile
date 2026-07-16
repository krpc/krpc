FROM ubuntu:24.04
LABEL maintainer="djungelorm <djungelorm@users.noreply.github.com>"

# Install essential packages. python3 + pip are for the system nanopb generator
# used by the C-nano CMake consumer jobs; python-is-python3 provides a `python`
# executable for the build-setup action's version-stamping step. The Bazel build
# uses the hermetic rules_python toolchain, not these.
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    curl ca-certificates gnupg git wget gosu \
    python3 python3-pip python-is-python3 && \
    apt-get clean

# Install Bazel
RUN wget https://github.com/bazelbuild/bazelisk/releases/download/v1.29.0/bazelisk-linux-amd64 -O /usr/local/bin/bazel && \
    chmod +x /usr/local/bin/bazel
COPY bazelrc /etc/bazel.bazelrc
RUN chmod 0644 /etc/bazel.bazelrc

# Repository cache for Bazel's downloaded archives, referenced by
# /etc/bazel.bazelrc (--repository_cache). Created before the first bazel
# invocation below so the rc path always exists; world-writable because both root
# and the (later-created) runner user invoke bazel. CI persists the small
# content_addressable/ subdir via actions/cache (see .github/actions/bazel-fetch).
RUN mkdir -p /var/cache/bazel-repo && chmod -R a+rwX /var/cache/bazel-repo

ENV BAZELISK_HOME=/usr/local/share/bazelisk
# Warm up bazelisk (download + extract Bazel 9.1.1) and make it world-readable.
# --ignore_all_rc_files: this runs with no workspace present, so /etc/bazel.bazelrc's
# `--config=system-llvm` (defined in the repo-root .bazelrc, absent here) would
# otherwise fail to resolve. The warmup only needs to extract Bazel.
RUN USE_BAZEL_VERSION=9.1.1 bazel --ignore_all_rc_files version && \
    chmod -R a+rwX /usr/local/share/bazelisk

# Install the .NET SDK for the build-csharp-sln CI job, which builds the
# generated Visual Studio solution (KRPC.sln) with `dotnet build`. The Bazel
# build uses rules_dotnet's own hermetic SDK/runtime, resolved through the test
# runfiles, not this one.
RUN apt-get update && \
    apt-get install -y --no-install-recommends dotnet-sdk-8.0 && \
    apt-get clean

# Install latex
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    latexmk make texlive-latex-base texlive-latex-recommended texlive-fonts-recommended \
    texlive-latex-extra tex-gyre libenchant-2-2 && \
    apt-get clean

# Install other build tools. gcc/g++/ccache/cmake/librsvg2-bin are for the CMake
# consumer jobs; luarocks/socat for the Linux-only lua/image/serialio rules;
# graphviz for the documentation's dot graphs.
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    gcc g++ ccache librsvg2-bin cmake luarocks socat graphviz && \
    apt-get clean

# Install system LLVM (clang-20) from apt.llvm.org for CI's C/C++ client build and
# lint. CI builds with `--config=system-llvm` (see .bazelrc + tools/build/llvm),
# which uses this system toolchain via toolchains_llvm's path mode. Pinned to 20
# to match the default hermetic clang 20.x, so clang-format/clang-tidy do not skew
# between dev and CI. Installs a self-contained toolchain at /usr/lib/llvm-20.
RUN wget -qO- https://apt.llvm.org/llvm-snapshot.gpg.key | gpg --dearmor -o /usr/share/keyrings/llvm.gpg && \
    echo "deb [signed-by=/usr/share/keyrings/llvm.gpg] http://apt.llvm.org/noble/ llvm-toolchain-noble-20 main" \
      > /etc/apt/sources.list.d/llvm.list && \
    apt-get update && \
    apt-get install -y --no-install-recommends \
    clang-20 clang-tidy-20 clang-format-20 lld-20 llvm-20 \
    libc++-20-dev libc++abi-20-dev libclang-rt-20-dev libunwind-20-dev && \
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
# Bazel build's nanopb generator uses the hermetic pip hub.
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
