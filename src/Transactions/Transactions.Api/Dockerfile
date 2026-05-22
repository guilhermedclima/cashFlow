FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["src/Transactions/Transactions.Api/Transactions.Api.csproj", "src/Transactions/Transactions.Api/"]
COPY ["src/Transactions/Transactions.Application/Transactions.Application.csproj", "src/Transactions/Transactions.Application/"]
COPY ["src/Transactions/Transactions.Domain/Transactions.Domain.csproj", "src/Transactions/Transactions.Domain/"]
COPY ["src/Transactions/Transactions.Infrastructure/Transactions.Infrastructure.csproj", "src/Transactions/Transactions.Infrastructure/"]
COPY ["src/Shared/Shared.Contracts/Shared.Contracts.csproj", "src/Shared/Shared.Contracts/"]
COPY ["src/Shared/Shared.Observability/Shared.Observability.csproj", "src/Shared/Shared.Observability/"]
RUN dotnet restore "src/Transactions/Transactions.Api/Transactions.Api.csproj"
COPY . .
WORKDIR "/src/src/Transactions/Transactions.Api"
RUN dotnet publish "Transactions.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Transactions.Api.dll"]
