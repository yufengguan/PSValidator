# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PromoStandards.Validator.Api.csproj", "./"]
RUN dotnet restore "PromoStandards.Validator.Api.csproj"
COPY . .
RUN dotnet publish "PromoStandards.Validator.Api.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PromoStandards.Validator.Api.dll"]
