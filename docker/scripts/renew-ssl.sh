#!/bin/bash
set -euo pipefail

# ─── SynDock Shop SSL Certificate Auto-Renewal ───
# Add to cron: 0 3 * * * /path/to/renew-ssl.sh >> /var/log/ssl-renew.log 2>&1

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOCKER_DIR="$(dirname "$SCRIPT_DIR")"
SSL_DIR="$DOCKER_DIR/nginx/ssl"
CERTBOT_DIR="$DOCKER_DIR/nginx/certbot"
COMPOSE_FILE="$DOCKER_DIR/docker-compose.prod.yml"
ENV_FILE="$DOCKER_DIR/.env"

echo "[$(date)] Starting SSL certificate renewal check..."

# Run certbot renewal
docker run --rm \
    -v "$SSL_DIR:/etc/letsencrypt/live/syndock" \
    -v "$CERTBOT_DIR:/var/www/certbot" \
    certbot/certbot renew \
    --webroot \
    --webroot-path=/var/www/certbot \
    --quiet

# Reload Nginx to pick up renewed certificate
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" exec -T shop-nginx nginx -s reload

echo "[$(date)] SSL renewal check complete."
