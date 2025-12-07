# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PromoStandards.Validator.Tests.StubServer.csproj", "./"]
RUN dotnet restore "PromoStandards.Validator.Tests.StubServer.csproj"
COPY . .
RUN dotnet publish "PromoStandards.Validator.Tests.StubServer.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PromoStandards.Validator.Tests.StubServer.dll"]
