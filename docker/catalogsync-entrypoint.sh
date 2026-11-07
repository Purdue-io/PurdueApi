#!/bin/bash
set -e

# Default values
SYNC_SCHEDULE="${SYNC_SCHEDULE:-0 2 * * *}"
DB_PROVIDER="${DB_PROVIDER:-Npgsql}"
DB_CONNECTION_STRING="${DB_CONNECTION_STRING:-Host=postgres;Port=5432;Database=purdueio;Username=purdueio;Password=purdueio}"
SYNC_ALL_TERMS="${SYNC_ALL_TERMS:-false}"

# Build the command as an array so values containing special characters
# (e.g. shell metacharacters in a connection string password) are passed
# through verbatim and never re-parsed by the shell.
CMD=(./CatalogSync -d "$DB_PROVIDER" -c "$DB_CONNECTION_STRING")

if [ "${SYNC_ALL_TERMS}" = "true" ]; then
    CMD+=(-a)
fi

if [ -n "${SYNC_TERMS}" ]; then
    CMD+=(-t "${SYNC_TERMS}")
fi

if [ -n "${SYNC_SUBJECTS}" ]; then
    CMD+=(-s "${SYNC_SUBJECTS}")
fi

# One-off sync mode (for testing/initialization)
if [ "${RUN_ONCE}" = "true" ]; then
    echo "Running one-time sync..."
    echo "Command: ${CMD[*]}"
    exec "${CMD[@]}"
fi

# Cron mode: Generate crontab and run supercronic.
# printf %q shell-quotes each argument so the crontab's shell reproduces
# the argument boundaries exactly.
CRON_CMD=$(printf '%q ' "${CMD[@]}")
echo "Setting up scheduled sync with cron expression: ${SYNC_SCHEDULE}"
echo "CatalogSync will run with arguments: ${CRON_CMD}"
echo "${SYNC_SCHEDULE} cd /app && ${CRON_CMD}" > /app/crontab

echo "Starting supercronic..."
exec supercronic /app/crontab
