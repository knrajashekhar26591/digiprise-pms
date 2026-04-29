FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Digiprise.PMS.API/Digiprise.PMS.API.csproj", "Digiprise.PMS.API/"]
COPY ["Digiprise.PMS.Application/Digiprise.PMS.Application.csproj", "Digiprise.PMS.Application/"]
COPY ["Digiprise.PMS.Domain/Digiprise.PMS.Domain.csproj", "Digiprise.PMS.Domain/"]
COPY ["Digiprise.PMS.Infrastructure/Digiprise.PMS.Infrastructure.csproj", "Digiprise.PMS.Infrastructure/"]
COPY ["Digiprise.PMS.Contracts/Digiprise.PMS.Contracts.csproj", "Digiprise.PMS.Contracts/"]

RUN dotnet restore "Digiprise.PMS.API/Digiprise.PMS.API.csproj"

COPY . .
WORKDIR "/src/Digiprise.PMS.API"
RUN dotnet publish "Digiprise.PMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Railway uses $PORT — must listen on 0.0.0.0
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Digiprise.PMS.API.dll"]
