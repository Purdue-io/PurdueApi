#!/bin/bash
set -e

# Default values
SYNC_SCHEDULE="${SYNC_SCHEDULE:-0 2 * * *}"
DB_PROVIDER="${DB_PROVIDER:-Npgsql}"
DB_CONNECTION_STRING="${DB_CONNECTION_STRING:-Host=postgres;Port=5432;Database=purdueio;Username=purdueio;Password=purdueio}"
SYNC_ALL_TERMS="${SYNC_ALL_TERMS:-false}"

# Build command-line arguments
ARGS="-d ${DB_PROVIDER} -c \"${DB_CONNECTION_STRING}\""

if [ "${SYNC_ALL_TERMS}" = "true" ]; then
    ARGS="${ARGS} -a"
fi

if [ -n "${SYNC_TERMS}" ]; then
    ARGS="${ARGS} -t ${SYNC_TERMS}"
fi

if [ -n "${SYNC_SUBJECTS}" ]; then
    ARGS="${ARGS} -s ${SYNC_SUBJECTS}"
fi

# One-off sync mode (for testing/initialization)
if [ "${RUN_ONCE}" = "true" ]; then
    echo "Running one-time sync..."
    echo "Command: ./CatalogSync ${ARGS}"
    eval "./CatalogSync ${ARGS}"
    exit $?
fi

# Cron mode: Generate crontab and run supercronic
echo "Setting up scheduled sync with cron expression: ${SYNC_SCHEDULE}"
echo "CatalogSync will run with arguments: ${ARGS}"
echo "${SYNC_SCHEDULE} cd /app && ./CatalogSync ${ARGS}" > /app/crontab

echo "Starting supercronic..."
exec supercronic /app/crontab
