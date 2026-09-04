# Base dotnet image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# Add curl to template.
# CDP PLATFORM HEALTHCHECK REQUIREMENT
RUN apt update && \
    apt upgrade -y && \
    apt install curl -y && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Build stage image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore tools
COPY .config/dotnet-tools.json .config/dotnet-tools.json
COPY .csharpierrc .csharpierrc
COPY .csharpierignore .csharpierignore

RUN dotnet tool restore

# Copy solution and project files for restore

COPY src/TradeImportsQuantityMgmt/TradeImportsQuantityMgmt.csproj src/TradeImportsQuantityMgmt/TradeImportsQuantityMgmt.csproj
COPY src/TradeImportsQuantityMgmt.Contract/TradeImportsQuantityMgmt.Contract.csproj src/TradeImportsQuantityMgmt.Contract/TradeImportsQuantityMgmt.Contract.csproj
COPY src/TradeImportsQuantityMgmt.Client/TradeImportsQuantityMgmt.Client.csproj src/TradeImportsQuantityMgmt.Client/TradeImportsQuantityMgmt.Client.csproj
COPY tests/TradeImportsQuantityMgmt.Tests/TradeImportsQuantityMgmt.Tests.csproj tests/TradeImportsQuantityMgmt.Tests/TradeImportsQuantityMgmt.Tests.csproj
COPY tests/TradeImportsQuantityMgmt.IntegrationTests/*.csproj tests/TradeImportsQuantityMgmt.IntegrationTests/

COPY TradeImportsQuantityMgmt.sln TradeImportsQuantityMgmt.sln
COPY Directory.Build.props Directory.Build.props
COPY global.json global.json

COPY NuGet.config NuGet.config
ARG DEFRA_NUGET_PAT

RUN dotnet restore

# Copy source code
COPY src/TradeImportsQuantityMgmt src/TradeImportsQuantityMgmt
COPY src/TradeImportsQuantityMgmt.Contract src/TradeImportsQuantityMgmt.Contract
COPY src/TradeImportsQuantityMgmt.Client src/TradeImportsQuantityMgmt.Client
COPY tests/TradeImportsQuantityMgmt.Tests tests/TradeImportsQuantityMgmt.Tests
COPY tests/TradeImportsQuantityMgmt.IntegrationTests tests/TradeImportsQuantityMgmt.IntegrationTests

# Check code formatting
RUN dotnet csharpier check .

# unit test and code coverage (exclude integration tests)
# RUN dotnet test --no-restore --filter "Category!=IntegrationTest"
RUN dotnet test --project tests/TradeImportsQuantityMgmt.Tests/TradeImportsQuantityMgmt.Tests.csproj --no-restore

FROM build AS publish
RUN dotnet publish src/TradeImportsQuantityMgmt -c Release -o /app/publish /p:UseAppHost=false


ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Final production image
FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 8085
USER app
ENTRYPOINT ["dotnet", "TradeImportsQuantityMgmt.dll"]



















