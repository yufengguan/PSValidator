#!/bin/bash

# Exit on error
set -e

echo "Starting deployment..."

# 1. Install Docker if not present (Assuming Amazon Linux 2023 or similar)
if ! command -v docker &> /dev/null; then
    echo "Docker not found. Installing..."
    sudo yum update -y
    sudo yum install -y docker
    sudo service docker start
    sudo usermod -a -G docker ec2-user
    echo "Docker installed. You may need to logout and login again for group changes to take effect."
else
    echo "Docker is already installed."
fi

# 2. Install Docker Compose if not present
if ! command -v docker-compose &> /dev/null; then
    echo "Docker Compose not found. Installing..."
    # Check if docker compose plugin is available (newer versions)
    if docker compose version &> /dev/null; then
        echo "Docker Compose plugin found."
    else
        echo "Installing Docker Compose..."
        sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
        sudo chmod +x /usr/local/bin/docker-compose
    fi
fi

# 2.5 Install Git if not present
if ! command -v git &> /dev/null; then
    echo "Git not found. Installing..."
    sudo yum install -y git
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
