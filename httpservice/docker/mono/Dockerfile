FROM fsharp:4.1.25
ADD ./bin/Release/net462/publish /app
EXPOSE 8080
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
CMD ["/usr/bin/mono", "/app/httpservice.exe"]
HEALTHCHECK --interval=5s CMD ["curl", "http://localhost:8080/healthcheck"]