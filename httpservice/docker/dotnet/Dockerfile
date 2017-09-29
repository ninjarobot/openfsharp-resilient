FROM microsoft/dotnet
ADD ./bin/Release/netcoreapp2.0/publish /app
EXPOSE 8080
CMD ["/usr/bin/dotnet", "/app/httpservice.dll"]
HEALTHCHECK --interval=5s CMD ["curl", "http://localhost:8080/healthcheck"]