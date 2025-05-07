FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine

RUN apk add --no-cache gcompat

ARG TARGETPLATFORM
COPY native/ /native/

RUN case "$TARGETPLATFORM" in \
        "linux/amd64") cp /native/linux-x64/azddns /bin/azddns ;; \
        "linux/arm64") cp /native/linux-arm64/azddns /bin/azddns ;; \
        *) echo "Unsupported: $TARGETPLATFORM" && exit 1 ;; \
    esac

# remove binaries we don't need
RUN rm -rf /native

ENTRYPOINT ["/bin/azddns"]
