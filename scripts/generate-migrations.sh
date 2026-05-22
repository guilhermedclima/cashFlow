#!/usr/bin/env bash
# Generates the initial EF Core migrations for both services.
#
# Prerequisite: dotnet ef installed globally.
#   $ dotnet tool install --global dotnet-ef
#
# In CI/dev, the SQL scripts under infra/postgres/init-*.sql already create
# the schema on the first Postgres boot. This script is useful for teams that
# prefer the code-versioned migrations workflow (recommended for production).

set -euo pipefail

cd "$(dirname "$0")/.."

if ! command -v dotnet-ef &>/dev/null && ! dotnet ef --version &>/dev/null; then
    echo "ERROR: dotnet-ef not found. Install it with:"
    echo "  dotnet tool install --global dotnet-ef"
    exit 1
fi

echo "▶ Generating initial migration for Transactions..."
dotnet ef migrations add Initial \
    --project src/Transactions/Transactions.Infrastructure \
    --startup-project src/Transactions/Transactions.Api \
    --output-dir Migrations \
    --context TransactionsDbContext

echo "▶ Generating initial migration for Reporting..."
dotnet ef migrations add Initial \
    --project src/Reporting/Reporting.Infrastructure \
    --startup-project src/Reporting/Reporting.Api \
    --output-dir Migrations \
    --context ReportingDbContext

echo ""
echo "✔ Migrations generated. To apply:"
echo "  dotnet ef database update --project src/Transactions/Transactions.Infrastructure --startup-project src/Transactions/Transactions.Api"
echo "  dotnet ef database update --project src/Reporting/Reporting.Infrastructure --startup-project src/Reporting/Reporting.Api"
