#!/usr/bin/env bash
set -euo pipefail

# SQLite CLI for inspecting the local database.
sudo apt-get update
sudo apt-get install -y --no-install-recommends sqlite3
sudo rm -rf /var/lib/apt/lists/*

# EF Core CLI for generating migrations.
dotnet tool update --global dotnet-ef --version '9.*'

# Trust the ASP.NET Core dev certificate for the HTTPS endpoint.
dotnet dev-certs https || true

dotnet restore src/PurdueIo.sln
