#!/usr/bin/env bash

set -euo pipefail

repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

dotnet restore Salvo.slnx --locked-mode
dotnet build Salvo.slnx --configuration Release --no-restore
dotnet test Salvo.slnx --configuration Release --no-build --no-restore
npm run check --prefix frontend
npm run build --prefix frontend
