FROM mono:6

RUN apt-get update && \
    apt-get install -y --no-install-recommends unzip && \
    rm -rf /var/lib/apt/lists/*

COPY TestServer.zip ./
RUN unzip TestServer.zip && rm TestServer.zip

EXPOSE 50000
EXPOSE 50001

ENTRYPOINT [ \
  "mono", "./TestServer.exe", \
  "--bind", "0.0.0.0", \
  "--rpc-port", "50000", \
  "--stream-port", "50001"]
CMD []
