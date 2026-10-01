# Multi-stage build for the Roivo web app (Render staging/production).
#
# Build context is the repository root. `dotnet restore` needs the test
# projects too, because Roivo.slnx references them.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Roivo.slnx", "."]
COPY ["src/", "src/"]
COPY ["tests/", "tests/"]

RUN dotnet restore
RUN dotnet build -c Release --no-restore

# Publish only the web host. Publishing the solution would work, but it
# flattens every project into one folder -- test assemblies and their
# dependencies (xunit, Moq, SSH.NET) included -- so name the project instead.
RUN dotnet publish src/Roivo.Web/Roivo.Web.csproj \
    -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Program.cs schedules the recurring jobs with
# TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens"), which throws at startup
# without the zone database.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# The Enable Banking private key is deliberately NOT baked into the image:
# *.pem is gitignored, so it is absent from Render's build context, and an
# image layer is the wrong place for key material. Supply it at
# /app/enable-banking-key.pem as a Render Secret File (or a bind mount
# locally) and point EnableBanking__PrivateKeyPath at it.

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Roivo.Web.dll"]
