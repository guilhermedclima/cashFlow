FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["src/Reporting/Reporting.Api/Reporting.Api.csproj", "src/Reporting/Reporting.Api/"]
COPY ["src/Reporting/Reporting.Application/Reporting.Application.csproj", "src/Reporting/Reporting.Application/"]
COPY ["src/Reporting/Reporting.Domain/Reporting.Domain.csproj", "src/Reporting/Reporting.Domain/"]
COPY ["src/Reporting/Reporting.Infrastructure/Reporting.Infrastructure.csproj", "src/Reporting/Reporting.Infrastructure/"]
COPY ["src/Shared/Shared.Contracts/Shared.Contracts.csproj", "src/Shared/Shared.Contracts/"]
COPY ["src/Shared/Shared.Observability/Shared.Observability.csproj", "src/Shared/Shared.Observability/"]
RUN dotnet restore "src/Reporting/Reporting.Api/Reporting.Api.csproj"
COPY . .
WORKDIR "/src/src/Reporting/Reporting.Api"
RUN dotnet publish "Reporting.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Reporting.Api.dll"]
