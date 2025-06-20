#!/usr/bin/env bash
set -e

# 1. поднять docker-compose (если не поднят)
docker compose up -d

# 2. подождать Oracle (XE грузится ~30-40s)
echo "⏳ Waiting Oracle to come up…"
until docker compose exec -T oracle bash -c "echo 'select 1 from dual;' | sqlplus -S app/app@XEPDB1" >/dev/null 2>&1; do
  sleep 5
done

# 3. создать и мигрировать
dotnet run --project src/Migrator.Cli -- create all   -c examples/config.yaml
dotnet run --project src/Migrator.Cli -- migrate all  -c examples/config.yaml
