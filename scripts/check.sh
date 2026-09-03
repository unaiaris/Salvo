#!/usr/bin/env bash

set -euo pipefail

repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# The generated OpenAPI types are checked first: it is the cheapest step, it needs no API process
# listening anywhere, and drift between the captured contract and the committed types invalidates
# everything the frontend build would go on to verify.
npm run api:types:check --prefix frontend

dotnet restore Salvo.slnx --locked-mode
dotnet tool restore
dotnet build Salvo.slnx --configuration Release --no-restore
dotnet ef migrations has-pending-model-changes \
  --project backend/src/Salvo.Infrastructure/Salvo.Infrastructure.csproj \
  --startup-project backend/src/Salvo.Api/Salvo.Api.csproj \
  --configuration Release \
  --no-build
dotnet test Salvo.slnx --configuration Release --no-build --no-restore
npm run check --prefix frontend
npm run build --prefix frontend
