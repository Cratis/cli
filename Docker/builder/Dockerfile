# The packaged Cratis builder: the Cratis CLI with the tools its levels need, so the builder Studio runs in the cloud runs
# anywhere Docker does. Start it with `cratis build run --docker`, which mounts the project at /workspace and passes the
# builder's connection through the environment.
#
#   docker build -f Docker/builder/Dockerfile --build-arg CLI_VERSION=<version> -t ghcr.io/cratis/builder:<version> .
ARG DOTNET_IMAGE=mcr.microsoft.com/dotnet/sdk:11.0
FROM ${DOTNET_IMAGE}

ARG CLI_VERSION
RUN test -n "${CLI_VERSION}" || (echo "Pass --build-arg CLI_VERSION=<version of the Cratis CLI to package>" >&2 && exit 1)

# Git for the commits the builder makes; Node.js for the React frontend the Cratis target renders and for agents that run on it;
# a JDK and Maven for the Axon target. Docker is not needed inside the image.
RUN apt-get update \
    && apt-get install -y --no-install-recommends git ca-certificates curl openjdk-21-jdk-headless maven nodejs npm \
    && rm -rf /var/lib/apt/lists/*

ENV DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_CLI_HOME=/tmp/dotnet \
    NUGET_PACKAGES=/tmp/nuget \
    PATH="${PATH}:/opt/cratis"

RUN dotnet tool install Cratis.Cli --version "${CLI_VERSION}" --tool-path /opt/cratis

WORKDIR /workspace
ENTRYPOINT ["cratis"]
CMD ["build", "run", "--project", "/workspace"]
