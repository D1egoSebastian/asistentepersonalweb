# Build stage: the SDK publishes the API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish AsistentePersonal/AsistentePersonal.csproj -c Release -o /app/publish

# Runtime stage: only the ASP.NET Core runtime and the published files, without the SDK.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Render gives the port in PORT, and ASP.NET Core reads ASPNETCORE_HTTP_PORTS, so the value is mapped here.
ENTRYPOINT ["sh", "-c", "export ASPNETCORE_HTTP_PORTS=\"${PORT:-8080}\"; exec dotnet AsistentePersonal.dll"]
