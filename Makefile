# Makefile with common tasks for the CashFlow project.
# Usage: make <target>

.PHONY: help up down logs restart clean build restore test test-unit test-integration loadtest migrations format check

help: ## List available targets
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-20s\033[0m %s\n", $$1, $$2}'

# ----------------- Docker / Stack -----------------

up: ## Bring up the full stack (infra + apps + observability)
	docker compose up -d --build

up-infra: ## Bring up only infra (run apps locally via dotnet run)
	docker compose -f docker-compose.infra.yml up -d

down: ## Tear down the stack keeping volumes
	docker compose down

clean: ## Tear down the stack and remove volumes (DESTRUCTIVE)
	docker compose down -v
	docker compose -f docker-compose.infra.yml down -v 2>/dev/null || true

logs: ## Tail logs from the apps
	docker compose logs -f transactions-api transactions-worker reporting-api reporting-worker

restart: down up ## Full restart

# ----------------- .NET -----------------

restore: ## dotnet restore
	dotnet restore CashFlow.sln

build: restore ## dotnet build (Release)
	dotnet build CashFlow.sln -c Release --no-restore

test: build ## Run ALL tests
	dotnet test CashFlow.sln -c Release --no-build

test-unit: build ## Unit tests only (fast, no Docker required)
	dotnet test CashFlow.sln -c Release --no-build --filter "Category=Unit"

test-integration: build ## Integration tests (requires Docker - Testcontainers)
	dotnet test CashFlow.sln -c Release --no-build --filter "Category=Integration"

format: ## dotnet format (applies .editorconfig)
	dotnet format CashFlow.sln

check: ## dotnet format --verify-no-changes (CI)
	dotnet format CashFlow.sln --verify-no-changes

# ----------------- Migrations -----------------

migrations: ## Generate initial EF Core migrations
	./scripts/generate-migrations.sh

# ----------------- Load testing -----------------

loadtest: ## Run the NBomber load test (Reporting.Api must be up)
	dotnet run --project tests/CashFlow.LoadTests -c Release

smoke: ## End-to-end smoke test via curl (stack must be up)
	./scripts/smoke-test.sh

demo: ## End-to-end demo: generates synthetic traffic for 60s
	./scripts/demo.sh

demo-watch: ## Demo with real-time tail of balances
	./scripts/demo.sh --watch

# ----------------- Diagrams -----------------

diagrams: ## Export Mermaid diagrams (.mmd) to PNG and SVG
	./scripts/export-diagrams.sh
