#!/bin/bash
# deploy.sh — provision all Azure resources from scratch
# Run once to create the environment, then CI/CD handles updates
#
# Usage:
#   chmod +x infra/deploy.sh
#   ./infra/deploy.sh dev eastus

set -e   # exit immediately on any error
set -o pipefail

ENVIRONMENT=${1:-dev}
LOCATION=${2:-eastus}
RESOURCE_GROUP="bookvault-${ENVIRONMENT}-rg"

echo "Deploying BookVault to Azure"
echo "Environment : ${ENVIRONMENT}"
echo "Location    : ${LOCATION}"
echo "Resource group: ${RESOURCE_GROUP}"

# ── 1. Create Resource Group ──────────────────────────────────────
# Interview answer: "What is an Azure Resource Group?"
# A logical container for related Azure resources. All BookVault resources
# go in one group — easier to view costs, apply policies, and delete together.
# Deleting the resource group deletes everything inside it.
echo ""
echo "Creating resource group..."
az group create \
  --name     "${RESOURCE_GROUP}" \
  --location "${LOCATION}" \
  --tags     "Project=BookVault" "Environment=${ENVIRONMENT}"

# ── 2. Generate secrets (first-time only) ─────────────────────────
echo ""
echo "Generating secrets..."

# Generate a strong JWT secret (64 random chars)
JWT_SECRET=$(openssl rand -base64 48 | tr -d "=+/" | cut -c1-64)

# Postgres password — complex enough for Azure's requirements
POSTGRES_PASSWORD=$(openssl rand -base64 24 | tr -d "=+/")
# Ensure it has uppercase, lowercase, digit (Azure requirement)
POSTGRES_PASSWORD="Bv${POSTGRES_PASSWORD}1!"

echo "Secrets generated (not shown for security)"

# ── 3. Deploy Bicep ───────────────────────────────────────────────
echo ""
echo "Deploying Bicep infrastructure..."

# az deployment group create: deploys a Bicep file to a resource group
# --mode Incremental: only creates/updates resources, never deletes
# Interview answer: "What is incremental vs complete deployment mode?"
# Incremental: add and update resources, leave unchanged ones alone.
# Complete: delete any resource in the group NOT in the template — dangerous.
# Always use Incremental unless you know exactly what you're doing.
DEPLOY_OUTPUT=$(az deployment group create \
  --resource-group "${RESOURCE_GROUP}" \
  --template-file  "infra/bicep/main.bicep" \
  --parameters     "environment=${ENVIRONMENT}" \
  --parameters     "postgresAdminPassword=${POSTGRES_PASSWORD}" \
  --parameters     "jwtSecretKey=${JWT_SECRET}" \
  --mode           Incremental \
  --query          "properties.outputs" \
  --output         json)

echo "Infrastructure deployed successfully"

# Extract outputs
CONTAINER_APP_URL=$(echo "${DEPLOY_OUTPUT}" | \
  python3 -c "import sys,json; d=json.load(sys.stdin); print(d['containerAppUrl']['value'])")
ACR_LOGIN=$(echo "${DEPLOY_OUTPUT}" | \
  python3 -c "import sys,json; d=json.load(sys.stdin); print(d['containerRegistryLogin']['value'])")
KV_NAME=$(echo "${DEPLOY_OUTPUT}" | \
  python3 -c "import sys,json; d=json.load(sys.stdin); print(d['keyVaultName']['value'])")

echo ""
echo "Deployment complete:"
echo "  App URL   : ${CONTAINER_APP_URL}"
echo "  ACR Login : ${ACR_LOGIN}"
echo "  Key Vault : ${KV_NAME}"
echo ""
echo "Next steps:"
echo "  1. Push your Docker image to ACR"
echo "  2. Update Container App with the image"
echo "  3. Run database migrations"
echo ""
echo "Store these in GitHub Actions secrets:"
echo "  AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID"
echo "  ACR_LOGIN_SERVER: ${ACR_LOGIN}"
echo "  RESOURCE_GROUP:   ${RESOURCE_GROUP}"