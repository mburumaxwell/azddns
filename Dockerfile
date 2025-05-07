FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine

RUN apk add --no-cache gcompat

ARG TARGETPLATFORM
COPY drop/ /drop/

RUN case "$TARGETPLATFORM" in \
        "linux/amd64") cp /drop/linux-x64/azddns /bin/azddns ;; \
        "linux/arm64") cp /drop/linux-arm64/azddns /bin/azddns ;; \
        *) echo "Unsupported: $TARGETPLATFORM" && exit 1 ;; \
    esac

# remove binaries we don't need
RUN rm -rf /drop

ENTRYPOINT ["/bin/azddns"]
