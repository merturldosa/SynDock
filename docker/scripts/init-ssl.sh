#!/bin/bash
set -euo pipefail

# ─── SynDock Shop SSL Certificate Setup (Let's Encrypt) ───
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOCKER_DIR="$(dirname "$SCRIPT_DIR")"
SSL_DIR="$DOCKER_DIR/nginx/ssl"
CERTBOT_DIR="$DOCKER_DIR/nginx/certbot"
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

# ─── Configuration ───────────────────────────
DOMAINS="${1:-}"
EMAIL="${2:-}"

if [ -z "$DOMAINS" ] || [ -z "$EMAIL" ]; then
    echo "Usage: $0 <domains> <email>"
    echo ""
    echo "Example:"
    echo "  $0 'syndock.co.kr,www.syndock.co.kr,admin.syndock.co.kr,catholia.com,mohyun.com' admin@syndock.co.kr"
    exit 1
fi

# ─── Create directories ─────────────────────
mkdir -p "$SSL_DIR" "$CERTBOT_DIR"

# ─── Step 1: Generate temporary self-signed certificate ──
log_info "Generating temporary self-signed certificate..."
openssl req -x509 -nodes -newkey rsa:2048 \
    -days 1 \
    -keyout "$SSL_DIR/privkey.pem" \
    -out "$SSL_DIR/fullchain.pem" \
    -subj "/CN=localhost" \
    2>/dev/null

log_info "Temporary certificate created."

# ─── Step 2: Start Nginx with temporary cert ──
log_info "Starting Nginx with temporary certificate..."
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d shop-nginx

# Wait for Nginx
sleep 3

# Verify Nginx is running
if ! docker compose -f "$COMPOSE_FILE" ps shop-nginx | grep -q "Up"; then
    log_error "Nginx failed to start. Check logs:"
    log_error "  docker compose -f $COMPOSE_FILE logs shop-nginx"
    exit 1
fi

log_info "Nginx is running."

# ─── Step 3: Request real certificate via Certbot ──
log_info "Requesting Let's Encrypt certificate..."

# Build certbot domain arguments
DOMAIN_ARGS=""
IFS=',' read -ra DOMAIN_ARRAY <<< "$DOMAINS"
for domain in "${DOMAIN_ARRAY[@]}"; do
    DOMAIN_ARGS="$DOMAIN_ARGS -d $(echo $domain | xargs)"
done

docker run --rm \
    -v "$SSL_DIR:/etc/letsencrypt/live/syndock" \
    -v "$CERTBOT_DIR:/var/www/certbot" \
    certbot/certbot certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    --email "$EMAIL" \
    --agree-tos \
    --no-eff-email \
    --force-renewal \
    $DOMAIN_ARGS

# ─── Step 4: Copy real certificates ─────────
log_info "Certificate obtained successfully!"

# Certbot stores certs in /etc/letsencrypt/live/<domain>/
# We already mounted to the correct location
FIRST_DOMAIN=$(echo "$DOMAINS" | cut -d',' -f1 | xargs)

# If certbot created in standard location, copy
if [ -f "/etc/letsencrypt/live/$FIRST_DOMAIN/fullchain.pem" ]; then
    cp "/etc/letsencrypt/live/$FIRST_DOMAIN/fullchain.pem" "$SSL_DIR/fullchain.pem"
    cp "/etc/letsencrypt/live/$FIRST_DOMAIN/privkey.pem" "$SSL_DIR/privkey.pem"
fi

# ─── Step 5: Reload Nginx with real certificate ──
log_info "Reloading Nginx with real certificate..."
docker compose -f "$COMPOSE_FILE" exec shop-nginx nginx -s reload

log_info "SSL setup complete!"
echo ""
echo "Certificates stored in: $SSL_DIR"
echo "Domains: $DOMAINS"
echo ""
echo "Set up auto-renewal with:"
echo "  crontab -e"
echo "  0 3 * * * $(realpath "$SCRIPT_DIR/renew-ssl.sh") >> /var/log/ssl-renew.log 2>&1"
echo ""
