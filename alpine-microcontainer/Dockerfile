FROM microsoft/dotnet as source
RUN dotnet new console -lang F# --name hello

FROM fsharp:4.1.25 as builder
COPY --from=source hello/*.fs* /build/
WORKDIR /build
# Problem with the following since new embedded FSharp SDK
# RUN msbuild /t:Restore && msbuild /t:Build /p:EnableDefaultCompileItems=false /p:Configuration=Release
RUN fsharpc Program.fs

FROM alpine:latest
COPY --from=builder /build/Program.exe /usr/lib/mono/fsharp/FSharp.Core.dll /
RUN apk add  --virtual build-dependencies --repository http://dl-cdn.alpinelinux.org/alpine/edge/testing --no-cache mono mono-dev musl-dev binutils gcc && \
    mkbundle --deps --static --config /etc/mono/config -L /usr/lib/mono/4.5 -o program Program.exe FSharp.Core.dll /usr/lib/mono/4.5/mscorlib.dll /usr/lib/mono/4.5/System.dll && \
    apk del build-dependencies && \
    rm Program.exe FSharp.Core.dll && \
    apk add --no-cache libgcc
ENTRYPOINT ["/program"]
