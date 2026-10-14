# Multi-stage build for the Roivo web app (Render staging/production).
#
# Build context is the repository root. `dotnet restore` resolves the whole
# solution, so every project Roivo.slnx references has to be in the image --
# the test projects and tools/seeder included, even though neither ships. A
# project named in the solution but missing from the context fails restore with
# MSB3202 before any compilation starts, which is what broke the deploys that
# followed the seeder landing in the solution.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Roivo.slnx", "."]
COPY ["src/", "src/"]
COPY ["tests/", "tests/"]
COPY ["tools/", "tools/"]

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
