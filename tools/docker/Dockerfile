FROM ubuntu:16.04
MAINTAINER djungelorm <djungelorm@users.noreply.github.com>

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    autoconf \
    automake \
    ca-certificates \
    cmake \
    cppcheck \
    curl \
    g++ \
    git \
    inotify-tools \
    libenchant1c2a \
    liblua5.1-0-dev \
    librsvg2-bin \
    libtool \
    libxml2-dev \
    libxslt1-dev \
    lua5.1 \
    luarocks \
    make \
    pkg-config \
    python \
    python-dev \
    python-pip \
    python-setuptools \
    python-virtualenv \
    socat \
    texlive-fonts-recommended \
    texlive-latex-base \
    texlive-latex-extra \
    texlive-latex-recommended \
    tzdata \
    unzip \
    virtualenv \
    wget && \
    rm -rf /var/lib/apt/lists/*

RUN apt-key adv \
      --keyserver hkp://keyserver.ubuntu.com:80 \
      --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF && \
    echo "deb http://download.mono-project.com/repo/ubuntu xenial main" | \
      tee /etc/apt/sources.list.d/mono-official.list && \
    apt-get update && apt-get install -y mono-devel && \
    rm -rf /var/lib/apt/lists/* && \
    wget http://download.mono-project.com/repo/ubuntu/pool/main/m/mono-tools/gendarme_4.2-2xamarin4+ubuntu1604b1_all.deb && \
    dpkg -i gendarme_4.2-2xamarin4+ubuntu1604b1_all.deb && \
    rm gendarme_4.2-2xamarin4+ubuntu1604b1_all.deb

ENV gosu_version 1.9
RUN apt-get update && \
    apt-get install -y --no-install-recommends sudo && \
    rm -rf /var/lib/apt/lists/* && \
    dpkgArch="$(dpkg --print-architecture | awk -F- '{ print $NF }')" && \
    wget -O /usr/local/bin/gosu \
      "https://github.com/tianon/gosu/releases/download/$gosu_version/gosu-$dpkgArch" && \
    wget -O /usr/local/bin/gosu.asc \
      "https://github.com/tianon/gosu/releases/download/$gosu_version/gosu-$dpkgArch.asc" && \
    export GNUPGHOME="$(mktemp -d)" && \
    gpg --keyserver ha.pool.sks-keyservers.net \
        --recv-keys B42F6819007F00F88E364FD4036A9C25BF357DD4 && \
    gpg --batch --verify /usr/local/bin/gosu.asc /usr/local/bin/gosu && \
    rm -r "$GNUPGHOME" /usr/local/bin/gosu.asc && \
    chmod +x /usr/local/bin/gosu && \
    gosu nobody true

ARG bazel_version=0.18.0
RUN echo "deb [arch=amd64] http://storage.googleapis.com/bazel-apt stable jdk1.8" | \
      tee /etc/apt/sources.list.d/bazel.list && \
    curl https://bazel.build/bazel-release.pub.gpg | sudo apt-key add - && \
    apt-get update && \
    apt-get install -y --no-install-recommends openjdk-8-jdk && \
    apt-get install -y --no-install-recommends bazel=${bazel_version} && \
    rm -rf /var/lib/apt/lists/*
COPY bazelrc /etc/bazel.bazelrc
RUN chmod 0644 /etc/bazel.bazelrc

ARG ksp_version
WORKDIR /usr/local/lib/ksp-${ksp_version}
RUN wget https://s3.amazonaws.com/krpc/lib/ksp-${ksp_version}.tar.gz && \
    tar -xf ksp-${ksp_version}.tar.gz && \
    ln -s /usr/local/lib/ksp-${ksp_version} /usr/local/lib/ksp && \
    rm /usr/local/lib/ksp-${ksp_version}/ksp-${ksp_version}.tar.gz

RUN user_id=1000 && \
    addgroup -q --gid $user_id build && \
    adduser -q --system --uid $user_id --home /build --disabled-password --ingroup build build
USER build
WORKDIR /build

COPY krpc.tar /build/krpc.tar
RUN mkdir krpc && \
    cd krpc && \
    tar -xf ../krpc.tar && \
    rm ../krpc.tar && \
    bazel fetch //... && \
    bazel build \
      @com_google_protobuf//:protobuf \
      @cpp_googletest//:gtest \
      @cpp_googletest//:gmock && \
    cd .. && \
    rm -rf krpc
USER root
RUN deluser build

WORKDIR /
COPY entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
