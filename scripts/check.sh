#!/usr/bin/env bash

set -euo pipefail

repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

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
