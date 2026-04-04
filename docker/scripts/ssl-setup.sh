#!/bin/bash
# SynDock SSL Setup Script
# Generates wildcard SSL certificate using Let's Encrypt + DNS validation
#
# Prerequisites:
#   - Oracle Cloud DNS or Cloudflare DNS configured
#   - certbot installed
#   - Domain: syndock.com + *.syndock.com
#
# Usage:
#   ./ssl-setup.sh [domain] [email]
#   ./ssl-setup.sh syndock.com admin@syndock.com

set -euo pipefail

DOMAIN="${1:-syndock.com}"
EMAIL="${2:-admin@${DOMAIN}}"
CERT_DIR="/etc/letsencrypt/live/${DOMAIN}"

echo "=== SynDock SSL Certificate Setup ==="
echo "Domain: ${DOMAIN} (+ *.${DOMAIN})"
echo "Email: ${EMAIL}"
echo ""

# Check if certbot is installed
if ! command -v certbot &> /dev/null; then
    echo "Installing certbot..."
    sudo apt-get update && sudo apt-get install -y certbot
fi

# Check for existing cert
if [ -d "${CERT_DIR}" ]; then
    echo "Certificate already exists at ${CERT_DIR}"
    echo "Expiry:"
    sudo openssl x509 -dates -noout -in "${CERT_DIR}/fullchain.pem" | grep "notAfter"
    echo ""
    read -p "Renew? (y/N): " RENEW
    if [ "${RENEW}" != "y" ]; then
        echo "Keeping existing certificate."
        exit 0
    fi
fi

echo ""
echo "=== Requesting Wildcard Certificate ==="
echo "This will use DNS-01 challenge."
echo "You'll need to create a TXT record for _acme-challenge.${DOMAIN}"
echo ""

sudo certbot certonly \
    --manual \
    --preferred-challenges dns-01 \
    --email "${EMAIL}" \
    --agree-tos \
    --no-eff-email \
    -d "${DOMAIN}" \
    -d "*.${DOMAIN}"

echo ""
echo "=== Certificate Generated ==="
echo "Files:"
echo "  Certificate: ${CERT_DIR}/fullchain.pem"
echo "  Key:         ${CERT_DIR}/privkey.pem"
echo ""

# Copy to docker nginx ssl directory
NGINX_SSL_DIR="$(dirname "$0")/../nginx/ssl"
mkdir -p "${NGINX_SSL_DIR}"
sudo cp "${CERT_DIR}/fullchain.pem" "${NGINX_SSL_DIR}/fullchain.pem"
sudo cp "${CERT_DIR}/privkey.pem" "${NGINX_SSL_DIR}/privkey.pem"
sudo chmod 644 "${NGINX_SSL_DIR}/fullchain.pem"
sudo chmod 600 "${NGINX_SSL_DIR}/privkey.pem"

echo "Certificates copied to ${NGINX_SSL_DIR}"
echo ""

# Setup auto-renewal cron
echo "Setting up auto-renewal..."
(crontab -l 2>/dev/null; echo "0 3 1,15 * * certbot renew --quiet && cp /etc/letsencrypt/live/${DOMAIN}/fullchain.pem ${NGINX_SSL_DIR}/fullchain.pem && cp /etc/letsencrypt/live/${DOMAIN}/privkey.pem ${NGINX_SSL_DIR}/privkey.pem && docker exec syndock-nginx nginx -s reload") | crontab -

echo "=== SSL Setup Complete ==="
echo ""
echo "Next steps:"
echo "  1. Update docker/nginx/nginx.conf to enable SSL (uncomment ssl blocks)"
echo "  2. Restart: docker compose -f docker-compose.prod.yml restart nginx"
echo "  3. Verify: curl -I https://${DOMAIN}/health/live"
