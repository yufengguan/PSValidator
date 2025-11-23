#!/bin/bash

# Exit on error
set -e

echo "Checking for certificate renewal..."

# 1. Attempt to renew certificates
# This will only renew if the certificate is close to expiry (within 30 days)
if docker compose version &> /dev/null; then
    docker compose run --rm certbot renew
else
    docker-compose run --rm certbot renew
fi

# 2. Reload Nginx to pick up new certificates
echo "Reloading Nginx..."
if docker compose version &> /dev/null; then
    docker compose exec app nginx -s reload
else
    docker-compose exec app nginx -s reload
fi

echo "Renewal check complete."
