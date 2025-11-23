#!/bin/bash

# Exit on error
set -e

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

# 4. Build and Run
echo "Building and starting containers..."
# Use 'docker compose' if available, else 'docker-compose'
if docker compose version &> /dev/null; then
    docker compose up -d --build
else
    docker-compose up -d --build
fi

echo "Deployment complete! App should be running on port 80."
