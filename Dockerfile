FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["Digiprise.PMS.API/Digiprise.PMS.API.csproj", "Digiprise.PMS.API/"]
COPY ["Digiprise.PMS.Application/Digiprise.PMS.Application.csproj", "Digiprise.PMS.Application/"]
COPY ["Digiprise.PMS.Domain/Digiprise.PMS.Domain.csproj", "Digiprise.PMS.Domain/"]
COPY ["Digiprise.PMS.Infrastructure/Digiprise.PMS.Infrastructure.csproj", "Digiprise.PMS.Infrastructure/"]
COPY ["Digiprise.PMS.Contracts/Digiprise.PMS.Contracts.csproj", "Digiprise.PMS.Contracts/"]

RUN dotnet restore "Digiprise.PMS.API/Digiprise.PMS.API.csproj" --no-cache

COPY . .
WORKDIR "/src/Digiprise.PMS.API"

RUN dotnet build "Digiprise.PMS.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Digiprise.PMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Digiprise.PMS.API.dll"]
