# Use the .NET Core runtime templates
FROM microsoft/dotnet as source
RUN dotnet new console -lang F# --name hello

# But we will target net462
FROM fsharp:4.1.34 as builder
COPY --from=source hello/*.fs* /build/
RUN sed -i 's/netcoreapp2.0/net462/g' /build/hello.fsproj
WORKDIR /build
# With msbuild on mono, restore and build works with new fsproj
RUN msbuild /t:Restore && msbuild /t:Build /p:Configuration=Release

# Now bundle on Alpine.  This was tricky, but with mono 5.10 on Alpine, mkbundle --deps pulls dependencies properly.
FROM alpine:latest
COPY --from=builder /build/bin/Release/net462/* /hello-bin/
RUN apk add --virtual build-dependencies --repository http://dl-cdn.alpinelinux.org/alpine/edge/testing --no-cache mono mono-dev musl-dev binutils gcc && \
    cd /hello-bin && \
    mkbundle --deps --static --config /etc/mono/config -L /usr/lib/mono/4.5 -o ../hello hello.exe && \
    apk del build-dependencies && \
    rm -rf hello-bin/ && \
    apk add --no-cache libgcc
ENTRYPOINT ["/hello"]
