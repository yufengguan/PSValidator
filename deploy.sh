#!/bin/bash

# Exit on error
set -e

# Load configuration
if [ ! -f "./deploy.config" ]; then
    echo "Error: deploy.config not found!"
    echo "Please copy deploy.config.example to deploy.config and update with your values."
    exit 1
fi

source ./deploy.config

# Default StubServer domain if not set
if [ -z "$STUBSERVER_DOMAIN" ]; then
    STUBSERVER_DOMAIN="StubServer.${DOMAIN}"
fi

echo "Starting deployment..."

# 1. Install Docker if not present
if ! command -v docker &> /dev/null; then
    echo "Docker not found. Installing..."
    
    # Update package index
    sudo apt-get update
    
    # Install prerequisites
    sudo apt-get install -y ca-certificates curl gnupg
    
    # Add Docker's official GPG key
    sudo install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
    sudo chmod a+r /etc/apt/keyrings/docker.gpg

    # Set up the repository
    echo \
      "deb [arch=\"$(dpkg --print-architecture)\" signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
      $(. /etc/os-release && echo \"$VERSION_CODENAME\") stable" | \
      sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
      
    # Install Docker Engine
    sudo apt-get update
    sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

    # Start and enable Docker
    sudo systemctl start docker
    sudo systemctl enable docker
    
    # Add user to docker group
    sudo usermod -aG docker $USER
    echo "Docker installed. You may need to logout and login again for group changes to take effect."
else
    echo "Docker is already installed."
fi

# 2. Install Docker Compose if not present
if ! command -v docker-compose &> /dev/null; then
    # Check if docker compose plugin is available (newer versions)
    if docker compose version &> /dev/null; then
        echo "Docker Compose plugin found."
    else
        echo "Installing Docker Compose..."
        sudo apt-get install -y docker-compose-plugin
    fi
fi

# 2.5 Install Git if not present
if ! command -v git &> /dev/null; then
    echo "Git not found. Installing..."
    sudo apt-get install -y git
fi

# 2.6 Configure Firewall (UFW)
if command -v ufw &> /dev/null; then
    echo "Configuring UFW firewall..."
    # Always allow SSH first to prevent lockout
    sudo ufw allow 22/tcp
    sudo ufw allow 80/tcp
    sudo ufw allow 443/tcp
    
    # Enable UFW if not already enabled (non-interactive)
    echo "y" | sudo ufw enable
    
    sudo ufw status
else
    echo "UFW not found. Skipping firewall configuration."
fi

# 3. Pull latest changes (if this is a git repo)
if [ -d ".git" ]; then
    echo "Pulling latest changes from git..."
    git pull
fi

# Ensure Docs directory exists (mapped in docker-compose)
mkdir -p Docs

# 4. Build and Run with SSL Setup
echo "Building and starting containers..."

# Define paths
NGINX_CONF="./nginx/nginx.conf"
NGINX_INIT="./nginx/nginx-init.conf"
NGINX_HTTPS="./nginx/nginx-https.conf"
NGINX_HTTPS_TEMPLATE="./nginx/nginx-https.conf.template"
NGINX_API_CONF="./nginx/nginx-api.conf"
NGINX_API_TEMPLATE="./nginx/nginx-api.conf.template"
NGINX_STUBSERVER_CONF="./nginx/nginx-stubserver.conf"
NGINX_STUBSERVER_TEMPLATE="./nginx/nginx-stubserver.conf.template"
CERT_PATH="./certbot/conf/live/${DOMAIN}/fullchain.pem"
API_CERT_PATH="./certbot/conf/live/${API_DOMAIN}/fullchain.pem"
STUBSERVER_CERT_PATH="./certbot/conf/live/${STUBSERVER_DOMAIN}/fullchain.pem"

# Ensure nginx directory exists
mkdir -p nginx

# Generate nginx-https.conf from template with domain substitution
echo "Generating nginx configuration with domain: ${DOMAIN}"
sed -e "s/DOMAIN_PLACEHOLDER/${DOMAIN}/g" \
    -e "s/WWW_DOMAIN_PLACEHOLDER/${WWW_DOMAIN}/g" \
    "$NGINX_HTTPS_TEMPLATE" > "$NGINX_HTTPS"

# Generate nginx-stubserver.conf from template
echo "Generating StubServer nginx configuration with domain: ${STUBSERVER_DOMAIN}"
sed -e "s/STUBSERVER_DOMAIN_PLACEHOLDER/${STUBSERVER_DOMAIN}/g" \
    "$NGINX_STUBSERVER_TEMPLATE" > "$NGINX_STUBSERVER_CONF"

# Check if certificates exist (use sudo to check root-owned files)
CERTS_EXIST=true
if ! sudo test -f "$CERT_PATH"; then
    echo "Main domain SSL certificate not found."
    CERTS_EXIST=false
fi
if ! sudo test -f "$API_CERT_PATH"; then
    echo "API domain SSL certificate not found."
    CERTS_EXIST=false
fi
if ! sudo test -f "$STUBSERVER_CERT_PATH"; then
    echo "StubServer domain SSL certificate not found."
    CERTS_EXIST=false
fi

# Pull latest images
if docker compose version &> /dev/null; then
    docker compose pull
else
    docker-compose pull
fi

if [ "$CERTS_EXIST" = false ]; then
    echo "SSL certificates not found. Starting bootstrap process..."
    
    # 1. Start with HTTP-only config
    echo "Using HTTP-only config for validation..."
    cp "$NGINX_INIT" "$NGINX_CONF"
    
    # Disable API and StubServer config temporarily to prevent Nginx crash due to missing certs
    echo "# Temporary empty config for bootstrap" > "$NGINX_API_CONF"
    echo "# Temporary empty config for bootstrap" > "$NGINX_STUBSERVER_CONF"
    
    # Start Nginx
    if docker compose version &> /dev/null; then
        docker compose up -d app
    else
        docker-compose up -d app
    fi
    
    # Wait for Nginx to start
    echo "Waiting for Nginx to start..."
    sleep 10
    
    # 2. Request Certificates
    echo "Requesting SSL certificates..."
    # Request certificate for main domain
    if docker compose version &> /dev/null; then
        docker compose run --rm certbot certonly --webroot --webroot-path /var/www/certbot -d ${DOMAIN} -d ${WWW_DOMAIN} --email ${EMAIL} --agree-tos --no-eff-email --non-interactive --keep-until-expiring
        # Request certificate for API domain
        docker compose run --rm certbot certonly --webroot --webroot-path /var/www/certbot -d ${API_DOMAIN} --email ${EMAIL} --agree-tos --no-eff-email --non-interactive --keep-until-expiring
        # Request certificate for StubServer domain
        docker compose run --rm certbot certonly --webroot --webroot-path /var/www/certbot -d ${STUBSERVER_DOMAIN} --email ${EMAIL} --agree-tos --no-eff-email --non-interactive --keep-until-expiring
    else
        docker-compose run --rm certbot certonly --webroot --webroot-path /var/www/certbot -d ${DOMAIN} -d ${WWW_DOMAIN} --email ${EMAIL} --agree-tos --no-eff-email --non-interactive --keep-until-expiring
        # Request certificate for API domain
        docker-compose run --rm certbot certonly --webroot --webroot-path /var/www/certbot -d ${API_DOMAIN} --email ${EMAIL} --agree-tos --no-eff-email --non-interactive --keep-until-expiring
        # Request certificate for StubServer domain
        docker-compose run --rm certbot certonly --webroot --webroot-path /var/www/certbot -d ${STUBSERVER_DOMAIN} --email ${EMAIL} --agree-tos --no-eff-email --non-interactive --keep-until-expiring
    fi
    
    # 3. Switch to HTTPS config
    echo "Certificate obtained. Switching to HTTPS config..."
    cp "$NGINX_HTTPS" "$NGINX_CONF"
    
    # Restore configs with SSL
    echo "Restoring API nginx configuration with SSL..."
    sed -e "s/API_DOMAIN_PLACEHOLDER/${API_DOMAIN}/g" \
        "$NGINX_API_TEMPLATE" > "$NGINX_API_CONF"

    echo "Restoring StubServer nginx configuration with SSL..."
    sed -e "s/STUBSERVER_DOMAIN_PLACEHOLDER/${STUBSERVER_DOMAIN}/g" \
        "$NGINX_STUBSERVER_TEMPLATE" > "$NGINX_STUBSERVER_CONF"
    
    # Reload Nginx
    if docker compose version &> /dev/null; then
        docker compose exec app nginx -s reload
    else
        docker-compose exec app nginx -s reload
    fi
    
else
    echo "SSL certificates found. Using HTTPS config..."
    cp "$NGINX_HTTPS" "$NGINX_CONF"
    
    if docker compose version &> /dev/null; then
        docker compose up -d
    else
        docker-compose up -d
    fi
fi

echo "Deployment complete! App should be running on https://${DOMAIN}"
echo "StubServer should be running on https://${STUBSERVER_DOMAIN}"
