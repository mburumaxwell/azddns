FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
COPY . .

ARG TARGETPLATFORM
ARG VERSION=0.1.0-dev

# Use TARGETPLATFORM to determine the correct runtime ID, validate, then map to RuntimeIdentifier (RID)
RUN echo "TARGETPLATFORM=${TARGETPLATFORM}" && \
    case "${TARGETPLATFORM}" in \
        "linux/amd64") export RID=linux-x64 && export LINKER="" ;; \
        "linux/arm64") export RID=linux-arm64 && export LINKER="/usr/bin/aarch64-linux-gnu-ld" ;; \
        *) echo "Unsupported TARGETPLATFORM: ${TARGETPLATFORM}" && exit 1 ;; \
    esac && \
    echo "Resolved RID=$RID and LINKER=$LINKER" && \
    echo $RID > /RID.txt && echo $LINKER > /LINKER.txt

# NativeAOT does not support full cross-compilation out of the box.
# When targeting linux-arm64 from a linux-amd64 GitHub Actions runner:
# - The compiler generates ARM64 native object files
# - But the host's default linker (ld.bfd or lld) cannot link ARM64 binaries
# - We explicitly install and use aarch64-linux-gnu-ld, the cross-linker
#
# For linux-x64, we skip this override and let the SDK pick the default linker (lld)
# This setup allows multi-arch AoT builds in one Dockerfile via docker buildx
# Some guidance from https://github.com/dotnet/dotnet-docker/blob/main/src/sdk/10.0/trixie-slim-aot/amd64/Dockerfile
# but the rest came from scouring the web!

# Install only what's required for AoT per Microsoft patterns + cross linker
RUN apt-get update \
    &&  apt-get install -y --no-install-recommends \
            clang llvm zlib1g-dev \
            binutils-aarch64-linux-gnu \
            gcc-aarch64-linux-gnu \
            libc6-dev-arm64-cross \
    && rm -rf /var/lib/apt/lists/*

# Publish NativeAOT binary with conditional linker
RUN export RID=$(cat /RID.txt) && \
    export LINKER=$(cat /LINKER.txt) && \
    dotnet publish AzureDDNS/AzureDDNS.csproj \
        --runtime $RID \
        --configuration Release \
        -p:VersionPrefix=$VERSION \
        -p:PackageVersion=$VERSION \
        -p:PublishAot=true \
        ${LINKER:+-p:IlcPath=$LINKER} \
        --output /app/publish

# Minimal Runtime Image
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine AS runtime

RUN apk add --no-cache gcompat
COPY --from=build /app/publish/azddns /bin/azddns
ENTRYPOINT ["/bin/azddns"]
