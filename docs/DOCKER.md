# Docker Deployment Guide for Purdue.io

This guide provides comprehensive instructions for deploying Purdue.io using Docker and Docker Compose.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [Production Deployment](#production-deployment)
- [Troubleshooting](#troubleshooting)
- [Maintenance](#maintenance)

## Overview

The Purdue.io Docker deployment consists of three main services:

1. **PostgreSQL** - Database for storing course catalog data
2. **API** - ASP.NET Core OData web service
3. **CatalogSync** - Scheduled sync process using supercronic

All services are orchestrated using Docker Compose and communicate over a private network.

## Prerequisites

- **Docker** 20.10+ ([Install Docker](https://docs.docker.com/get-docker/))
- **Docker Compose** 2.0+ ([Install Docker Compose](https://docs.docker.com/compose/install/))
- At least 2GB of available disk space
- At least 1GB of available RAM

## Quick Start

```bash
# Clone the repository (if not already done)
git clone https://github.com/Purdue-io/PurdueApi.git
cd PurdueApi

# Copy the example environment file
cp .env.example .env

# (Optional) Edit .env to customize configuration
nano .env

# Build and start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Stop and remove volumes (deletes database data)
docker-compose down -v
```

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                       Host Machine                          │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │              Docker Network (purdueio-net)           │  │
│  │                                                      │  │
│  │  ┌──────────────┐   ┌──────────────┐               │  │
│  │  │  PostgreSQL  │   │     API      │               │  │
│  │  │    :5432     │◄──┤    :8080     │               │  │
│  │  └──────┬───────┘   └──────────────┘               │  │
│  │         │                                           │  │
│  │         │           ┌──────────────┐               │  │
│  │         └───────────► CatalogSync  │               │  │
│  │                     │ (supercronic)│               │  │
│  │                     └──────────────┘               │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  Port Mapping:   localhost:8080 → api:8080                 │
└─────────────────────────────────────────────────────────────┘
```

### Service Details

#### PostgreSQL
- **Image**: `postgres:16-alpine`
- **Purpose**: Persistent database storage
- **Volume**: `postgres-data` (persists data across container restarts)
- **Health Check**: Runs `pg_isready` every 10 seconds

#### API
- **Build**: Custom Dockerfile (Dockerfile.api)
- **Framework**: ASP.NET Core 9.0
- **Port**: Exposed on host port 8080 (configurable via API_PORT)
- **Dependencies**: Waits for PostgreSQL to be healthy
- **Auto-migrations**: Runs EF Core migrations on startup

#### CatalogSync
- **Build**: Custom Dockerfile (Dockerfile.catalogsync)
- **Scheduler**: Supercronic (Docker-friendly cron)
- **Default Schedule**: Daily at 2:00 AM
- **Dependencies**: Waits for PostgreSQL to be healthy

## Configuration

All configuration is done through environment variables in the `.env` file.

### Environment Variables Reference

#### PostgreSQL Configuration

| Variable | Default | Description |
|----------|---------|-------------|
| `POSTGRES_DB` | `purdueio` | Database name |
| `POSTGRES_USER` | `purdueio` | Database username |
| `POSTGRES_PASSWORD` | `purdueio` | Database password |

**Security Note**: Change the default password in production!

#### API Configuration

| Variable | Default | Description |
|----------|---------|-------------|
| `API_PORT` | `8080` | Host port to expose the API on |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Environment (Development/Production) |

#### CatalogSync Configuration

| Variable | Default | Description |
|----------|---------|-------------|
| `SYNC_SCHEDULE` | `0 2 * * *` | Cron expression for sync schedule |
| `SYNC_TERMS` | _(empty)_ | Comma-separated term codes to sync |
| `SYNC_SUBJECTS` | _(empty)_ | Comma-separated subject codes to sync |
| `SYNC_ALL_TERMS` | `false` | Sync all terms vs current/future only |
| `RUN_ONCE` | `false` | Run once and exit (for testing) |

### Cron Schedule Examples

The `SYNC_SCHEDULE` variable uses standard cron syntax:

```
┌───────────── minute (0 - 59)
│ ┌───────────── hour (0 - 23)
│ │ ┌───────────── day of month (1 - 31)
│ │ │ ┌───────────── month (1 - 12)
│ │ │ │ ┌───────────── day of week (0 - 6) (Sunday = 0)
│ │ │ │ │
│ │ │ │ │
* * * * *
```

Common examples:
- `0 2 * * *` - Daily at 2:00 AM (DEFAULT)
- `0 */6 * * *` - Every 6 hours
- `0 */2 * * *` - Every 2 hours
- `*/30 * * * *` - Every 30 minutes
- `0 0 * * 0` - Weekly on Sunday at midnight
- `0 3 1 * *` - Monthly on the 1st at 3:00 AM

## Usage Examples

### Example 1: Development Setup

For development with frequent syncs:

```bash
# .env configuration
POSTGRES_PASSWORD=devpassword
API_PORT=8080
SYNC_SCHEDULE=0 */2 * * *  # Every 2 hours
SYNC_ALL_TERMS=false

# Start services
docker-compose up -d
```

### Example 2: Production Setup with Specific Terms

For production syncing only specific terms and subjects:

```bash
# .env configuration
POSTGRES_DB=purdueio_prod
POSTGRES_USER=purdueio_prod
POSTGRES_PASSWORD=<strong-random-password>
API_PORT=8080
SYNC_SCHEDULE=0 2 * * *  # Daily at 2 AM
SYNC_TERMS=202510,202520
SYNC_SUBJECTS=CS,MA,ECE,PHYS
SYNC_ALL_TERMS=false

# Start services
docker-compose up -d
```

### Example 3: Initial Database Population

Run a one-time sync to populate the database:

```bash
# First, start PostgreSQL and API
docker-compose up -d postgres api

# Wait for services to be healthy
docker-compose ps

# Run one-time sync
docker-compose run --rm -e RUN_ONCE=true catalogsync

# If successful, start the scheduled sync
docker-compose up -d catalogsync
```

### Example 4: API-Only Deployment

If you want to sync externally and only run the API:

```bash
# Start only PostgreSQL and API
docker-compose up -d postgres api

# Verify API is running
curl http://localhost:8080/odata/Terms
```

## Production Deployment

### Security Hardening

1. **Use Strong Passwords**
   ```bash
   # Generate a strong password
   openssl rand -base64 32

   # Update .env
   POSTGRES_PASSWORD=<generated-password>
   ```

2. **Use Docker Secrets** (Docker Swarm)
   ```yaml
   # docker-compose.yml modifications
   secrets:
     postgres_password:
       external: true

   services:
     postgres:
       secrets:
         - postgres_password
       environment:
         POSTGRES_PASSWORD_FILE: /run/secrets/postgres_password
   ```

3. **Run Behind a Reverse Proxy**

   Use nginx or Traefik to handle HTTPS/TLS termination:

   ```nginx
   server {
       listen 443 ssl;
       server_name api.purdue.io;

       ssl_certificate /path/to/cert.pem;
       ssl_certificate_key /path/to/key.pem;

       location / {
           proxy_pass http://localhost:8080;
           proxy_set_header Host $host;
           proxy_set_header X-Real-IP $remote_addr;
       }
   }
   ```

4. **Network Segmentation**

   Keep the database on an internal network, not exposed to the internet.

### Resource Limits

Add resource limits to prevent services from consuming excessive resources:

```yaml
# docker-compose.yml modifications
services:
  postgres:
    deploy:
      resources:
        limits:
          cpus: '1'
          memory: 1G
        reservations:
          cpus: '0.5'
          memory: 512M

  api:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 512M
        reservations:
          cpus: '1'
          memory: 256M
```

### Scaling the API

Scale the API service horizontally for high availability:

```bash
# Start 3 API instances
docker-compose up -d --scale api=3

# Use a load balancer (nginx, HAProxy, traefik) to distribute traffic
```

**Important**: Only run ONE CatalogSync instance to avoid concurrent sync conflicts.

### Monitoring

1. **Health Checks**
   ```bash
   # Check service health
   docker-compose ps

   # Inspect health check logs
   docker inspect purdueio-api | grep -A 10 Health
   ```

2. **Application Logs**
   ```bash
   # View all logs
   docker-compose logs

   # Follow API logs
   docker-compose logs -f api

   # View last 100 lines
   docker-compose logs --tail=100 catalogsync
   ```

3. **Resource Usage**
   ```bash
   # Monitor resource usage
   docker stats
   ```

4. **Centralized Logging**

   Configure Docker logging drivers for centralized log management:
   ```yaml
   # docker-compose.yml
   x-logging: &default-logging
     driver: json-file
     options:
       max-size: "10m"
       max-file: "3"

   services:
     api:
       logging: *default-logging
   ```

## Troubleshooting

### Services Won't Start

**Symptom**: `docker-compose up` fails or services are unhealthy

**Solutions**:
```bash
# Check service status
docker-compose ps

# View logs for errors
docker-compose logs

# Check disk space
df -h

# Check if ports are already in use
netstat -tuln | grep 8080
netstat -tuln | grep 5432
```

### API Cannot Connect to Database

**Symptom**: API logs show database connection errors

**Solutions**:
```bash
# Verify PostgreSQL is healthy
docker-compose ps postgres

# Check PostgreSQL logs
docker-compose logs postgres

# Verify network connectivity
docker-compose exec api ping postgres

# Ensure connection string matches .env settings
docker-compose config | grep DbConnectionString
```

### Migrations Fail

**Symptom**: API fails to start due to migration errors

**Solutions**:
```bash
# View API logs
docker-compose logs api

# Reset database (WARNING: Deletes all data)
docker-compose down -v
docker-compose up -d

# Manual migration (if needed)
docker-compose exec api dotnet ef database update
```

### CatalogSync Not Running

**Symptom**: Sync doesn't execute on schedule

**Solutions**:
```bash
# Check CatalogSync logs
docker-compose logs catalogsync

# Verify cron schedule is valid
docker-compose exec catalogsync cat /app/crontab

# Test one-time sync
docker-compose run --rm -e RUN_ONCE=true catalogsync

# Verify supercronic is running
docker-compose exec catalogsync ps aux | grep supercronic
```

### Invalid Cron Expression

**Symptom**: CatalogSync exits immediately

**Solutions**:
```bash
# Check logs for validation errors
docker-compose logs catalogsync

# Verify cron syntax using https://crontab.guru/
# Fix SYNC_SCHEDULE in .env and restart
docker-compose restart catalogsync
```

### Out of Disk Space

**Symptom**: Services fail with disk space errors

**Solutions**:
```bash
# Check disk usage
docker system df

# Remove unused images
docker image prune -a

# Remove unused volumes
docker volume prune

# Remove unused networks
docker network prune
```

### Performance Issues

**Symptom**: API is slow or unresponsive

**Solutions**:
```bash
# Check resource usage
docker stats

# Check PostgreSQL query performance
docker-compose exec postgres psql -U purdueio -c "SELECT * FROM pg_stat_activity;"

# Add database indexes (if needed)
# Check API logs for slow queries

# Scale API horizontally
docker-compose up -d --scale api=3
```

## Maintenance

### Backing Up the Database

```bash
# Backup database to file
docker-compose exec postgres pg_dump -U purdueio purdueio > backup-$(date +%Y%m%d).sql

# Automated daily backups (crontab)
0 3 * * * cd /path/to/PurdueApi && docker-compose exec -T postgres pg_dump -U purdueio purdueio | gzip > /backups/purdueio-$(date +\%Y\%m\%d).sql.gz
```

### Restoring from Backup

```bash
# Stop API and CatalogSync
docker-compose stop api catalogsync

# Restore database
cat backup-20240115.sql | docker-compose exec -T postgres psql -U purdueio purdueio

# Restart services
docker-compose start api catalogsync
```

### Updating Images

```bash
# Pull latest images
docker-compose pull

# Rebuild custom images
docker-compose build --no-cache

# Restart with new images
docker-compose up -d
```

### Viewing Database Data

```bash
# Connect to PostgreSQL
docker-compose exec postgres psql -U purdueio

# Useful queries
\dt                              # List tables
SELECT COUNT(*) FROM "Courses";  # Count courses
SELECT COUNT(*) FROM "Sections"; # Count sections
SELECT * FROM "Terms";           # List terms
\q                               # Quit
```

### Cleanup

```bash
# Stop and remove containers
docker-compose down

# Stop and remove containers + volumes (deletes database data)
docker-compose down -v

# Remove all images
docker-compose down --rmi all

# Full cleanup (containers, volumes, images)
docker-compose down -v --rmi all --remove-orphans
```

## Additional Resources

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [PostgreSQL Docker Image](https://hub.docker.com/_/postgres)
- [ASP.NET Core Docker Documentation](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)
- [Supercronic Documentation](https://github.com/aptible/supercronic)
- [Purdue.io Wiki](https://github.com/Purdue-io/PurdueApi/wiki/)

## Support

For issues, questions, or contributions:
- [GitHub Issues](https://github.com/Purdue-io/PurdueApi/issues)
- [Contributing Guide](https://github.com/Purdue-io/PurdueApi/wiki/Contributing)
