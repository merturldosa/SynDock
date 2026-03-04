#!/bin/bash
set -euo pipefail

# ─── SynDock Shop Production Deployment Script ───
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOCKER_DIR="$(dirname "$SCRIPT_DIR")"
COMPOSE_FILE="$DOCKER_DIR/docker-compose.prod.yml"
ENV_FILE="$DOCKER_DIR/.env"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info()  { echo -e "${GREEN}[INFO]${NC} $1"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# ─── Check prerequisites ────────────────────
if ! command -v docker &> /dev/null; then
    log_error "Docker is not installed"
    exit 1
fi

if ! docker compose version &> /dev/null; then
    log_error "Docker Compose V2 is not installed"
    exit 1
fi

# ─── Check .env file ────────────────────────
if [ ! -f "$ENV_FILE" ]; then
    log_error ".env file not found at $ENV_FILE"
    log_info "Copy .env.example to .env and fill in your values:"
    log_info "  cp $DOCKER_DIR/.env.example $ENV_FILE"
    exit 1
fi

# ─── Validate required variables ─────────────
REQUIRED_VARS=("POSTGRES_PASSWORD" "JWT_SECRET" "ENCRYPTION_KEY")
source "$ENV_FILE"

for var in "${REQUIRED_VARS[@]}"; do
    if [ -z "${!var:-}" ]; then
        log_error "Required variable $var is not set in .env"
        exit 1
    fi
    # Check for default/placeholder values
    if [[ "${!var}" == *"ChangeMe"* ]]; then
        log_warn "$var still has a placeholder value — change it before production use!"
    fi
done

# ─── Check SSL certificates ─────────────────
SSL_DIR="$DOCKER_DIR/nginx/ssl"
if [ ! -f "$SSL_DIR/fullchain.pem" ] || [ ! -f "$SSL_DIR/privkey.pem" ]; then
    log_warn "SSL certificates not found in $SSL_DIR"
    log_warn "Run init-ssl.sh first for HTTPS support, or place your certificates manually."
    log_warn "Continuing with deployment (HTTP-only mode may apply)..."
fi

# ─── Pull latest images ─────────────────────
log_info "Pulling latest base images..."
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" pull --ignore-pull-failures 2>/dev/null || true

# ─── Build application images ────────────────
log_info "Building application images..."
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" build --parallel

# ─── Stop existing services ──────────────────
log_info "Stopping existing services..."
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" down --timeout 30

# ─── Start services ──────────────────────────
log_info "Starting services..."
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d

# ─── Wait for health checks ─────────────────
log_info "Waiting for services to become healthy (15s)..."
sleep 15

# ─── Health check ────────────────────────────
HEALTH_URL="http://localhost/health/live"
log_info "Checking API health at $HEALTH_URL..."

RETRIES=5
for i in $(seq 1 $RETRIES); do
    HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$HEALTH_URL" 2>/dev/null || echo "000")
    if [ "$HTTP_CODE" = "200" ]; then
        log_info "Health check passed (HTTP $HTTP_CODE)"
        break
    fi
    if [ "$i" -eq "$RETRIES" ]; then
        log_error "Health check failed after $RETRIES attempts (HTTP $HTTP_CODE)"
        log_warn "Check logs with: docker compose -f $COMPOSE_FILE logs"
        exit 1
    fi
    log_warn "Health check attempt $i/$RETRIES failed (HTTP $HTTP_CODE), retrying in 5s..."
    sleep 5
done

# ─── Show service status ────────────────────
log_info "Service status:"
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" ps

# ─── Clean up old images ────────────────────
log_info "Cleaning up dangling images..."
docker image prune -f > /dev/null 2>&1 || true

log_info "Deployment complete!"
echo ""
echo "  Shop Web:    https://catholia.com / https://mohyun.com"
echo "  Portal:      https://admin.syndock.co.kr"
echo "  HomePage:    https://syndock.co.kr"
echo "  Grafana:     http://localhost:3001"
echo "  Prometheus:  http://localhost:9090"
echo ""
