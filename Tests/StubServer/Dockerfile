# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file from correct path (context is root)
COPY ["Tests/StubServer/PromoStandards.Validator.Tests.StubServer.csproj", "Tests/StubServer/"]

# Copy dependent file (optional/conditional in csproj now, but good to have if present)
COPY ["Docs/PSServiceList.json", "Docs/"]

# Restore
RUN dotnet restore "Tests/StubServer/PromoStandards.Validator.Tests.StubServer.csproj"

# Copy source code
COPY ["Tests/StubServer/", "Tests/StubServer/"]

# Publish
RUN dotnet publish "Tests/StubServer/PromoStandards.Validator.Tests.StubServer.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PromoStandards.Validator.Tests.StubServer.dll"]
