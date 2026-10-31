FROM ubuntu:24.04
LABEL maintainer="djungelorm <djungelorm@users.noreply.github.com>"

# Install essential packages. python-is-python3 provides a `python` executable for
# the CI workflow steps. The Bazel build uses the hermetic rules_python toolchain.
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    curl ca-certificates git python3 python-is-python3 && \
    apt-get clean

# Install Bazelisk, which fetches the Bazel version in the repository's .bazelversion
RUN curl -fsSL https://github.com/bazelbuild/bazelisk/releases/download/v1.29.0/bazelisk-linux-amd64 -o /usr/local/bin/bazel && \
    chmod +x /usr/local/bin/bazel

# Install latex
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    latexmk make texlive-latex-base texlive-latex-recommended texlive-fonts-recommended \
    texlive-latex-extra tex-gyre libenchant-2-2 && \
    apt-get clean

# Install other build tools: socat for the serial transport tests, graphviz for
# the documentation's dot graphs and unzip for the CI steps that unpack an archive
RUN apt-get update && \
    apt-get install -y --no-install-recommends socat graphviz unzip && \
    apt-get clean

# Install system LLVM (clang-23) from apt.llvm.org for CI's C/C++ client build and
# lint. CI builds with `--config=system-llvm` (see .bazelrc + tools/build/llvm),
# which uses this system toolchain via toolchains_llvm's path mode. Pinned to 23
# to match the default hermetic clang 23.x, so clang-format/clang-tidy do not skew
# between dev and CI. Installs a self-contained toolchain at /usr/lib/llvm-23.
# toolchains_llvm links clangd into the toolchain, and a Bazel disk cache needs
# every file the toolchain links to exist.
RUN curl -fsSL https://apt.llvm.org/llvm-snapshot.gpg.key -o /usr/share/keyrings/llvm.asc && \
    echo "deb [signed-by=/usr/share/keyrings/llvm.asc] http://apt.llvm.org/noble/ llvm-toolchain-noble-23 main" \
      > /etc/apt/sources.list.d/llvm.list && \
    apt-get update && \
    apt-get install -y --no-install-recommends \
    clang-23 clangd-23 clang-tidy-23 clang-format-23 lld-23 llvm-23 \
    libc++-23-dev libc++abi-23-dev libclang-rt-23-dev libunwind-23-dev && \
    apt-get clean

WORKDIR /build

# Add github actions user
ARG UID=1001
ARG GID=121
RUN groupadd -g $GID runner
RUN useradd -mr -d /home/runner -u $UID -g $GID runner
