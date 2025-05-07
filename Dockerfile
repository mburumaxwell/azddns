FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine

RUN apk add --no-cache gcompat

ARG TARGETPLATFORM
COPY binaries/ /binaries/

RUN case "$TARGETPLATFORM" in \
        "linux/amd64") cp /binaries/linux-x64/azddns /bin/azddns ;; \
        "linux/arm64") cp /binaries/linux-arm64/azddns /bin/azddns ;; \
        *) echo "Unsupported: $TARGETPLATFORM" && exit 1 ;; \
    esac

# remove binaries we don't need
RUN rm -rf /binaries

# set executable permissions (needed because we are copying from  outside the container)
RUN chmod +x /bin/azddns

ENTRYPOINT ["/bin/azddns"]
