# Deployment Guide for PromoStandards Validator

This guide explains how to deploy the application to your AWS T4g instance for **demo18.com**.

## Prerequisites

*   **SSH Key**: The `.pem` file you downloaded when creating the AWS instance.
*   **Server IP**: The public IP address of your AWS instance (or use `demo18.com` if DNS is already propagated).
*   **GitHub Repo URL**: The URL of your repository (e.g., `https://github.com/yourusername/PSValidator.git`).

## Step 1: Connect to your Server

Open your terminal (PowerShell or Command Prompt) and SSH into your AWS instance:

```bash
# Replace path/to/key.pem with your actual key file path
# Replace 1.2.3.4 with your server's IP address
ssh -i path/to/key.pem ec2-user@1.2.3.4
```

> **Note**: If you get a "Permission denied" error for the key file, you may need to restrict its permissions (on Linux/Mac: `chmod 400 key.pem`). On Windows, standard permissions usually work, but ensure your user has read access.

## Step 2: Clone the Repository

Once logged in to the server, install Git and clone your repository:

```bash
# 1. Update system and install git
sudo yum update -y
sudo yum install -y git

# 2. Clone your repository
# Replace with your actual GitHub URL
git clone https://github.com/YOUR_USERNAME/PSValidator.git

# 3. Enter the project directory
cd PSValidator
```

## Step 3: Run the Deployment Script

We have prepared a `deploy.sh` script that handles Docker installation and startup.

```bash
# 1. Make the script executable
chmod +x deploy.sh

# 2. Run the deployment script
./deploy.sh
```

## Step 4: Verify Deployment

1.  Wait for the script to finish. It will install Docker, build the containers, and start them.
2.  Open your browser and visit: `http://demo18.com` (or your server's IP).
3.  You should see the PromoStandards Validator app.

## Troubleshooting

*   **Site not loading?**
    *   Check your AWS **Security Group** settings. Ensure **Inbound Rules** allow **HTTP (port 80)** traffic from Anywhere (`0.0.0.0/0`).
*   **Permission errors?**
    *   The script adds `ec2-user` to the `docker` group. If you get docker permission errors, try logging out and logging back in, or run `newgrp docker`.
*   **View Logs**:
    *   To see logs: `docker compose logs -f`
