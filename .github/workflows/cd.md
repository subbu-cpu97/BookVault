# CD pipeline — Continuous Deployment to Azure
# Triggers: when CI pipeline passes on main branch
# Interview answer: "What is the difference between CI and CD?"
# CI (Continuous Integration): build, test, validate on every push.
# CD (Continuous Delivery): deploy passing builds to an environment automatically.
# Together they eliminate manual deployments — the only way code reaches
# production is through this pipeline, with tests as the gate.

name: BookVault CD

on:
  workflow_run:
    workflows: [ "BookVault CI" ]  # triggers after CI completes
    types: [ completed ]
    branches: [ main ]

# Permissions needed for OIDC authentication to Azure
# Interview answer: "What is OIDC authentication in GitHub Actions?"
# Instead of storing an Azure service principal password as a secret,
# GitHub and Azure federate via OpenID Connect. GitHub proves to Azure
# "this workflow is running from THIS repository on THIS branch."
# Azure trusts that proof and issues a short-lived access token.
# No long-lived secrets to rotate or leak.
permissions:
  id-token   : write   # required for OIDC
  contents   : read
  packages   : write

env:
  DOTNET_VERSION: '10.0.x'
  IMAGE_NAME    : 'bookvault-api'

jobs:
  deploy:
    name: Deploy to Azure
    runs-on: ubuntu-latest
    # Only deploy if CI passed — never deploy broken code
    if: ${{ github.event.workflow_run.conclusion == 'success' }}

    environment:
      name: production
      url : ${{ steps.deploy.outputs.app_url }}

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      # OIDC login — no passwords stored anywhere
      # Prerequisites: configure federated credential in Azure AD
      # (Step 8 below shows how to set this up)
      - name: Azure login via OIDC
        uses: azure/login@v2
        with:
          client-id      : ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id      : ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      # ── Build and push Docker image ──────────────────────────────
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Cache NuGet
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key : nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}

      # Login to Azure Container Registry using the OIDC token
      # No ACR password needed — Azure RBAC grants access
      - name: Login to ACR
        run: |
          az acr login --name ${{ secrets.ACR_NAME }}

      # Tag image with git SHA — every deployment is traceable to a commit
      # Interview answer: "Why tag images with git SHA instead of 'latest'?"
      # 'latest' is mutable — two deployments with 'latest' might use different images.
      # SHA tags are immutable — you know exactly which code is running.
      # Rollback = re-deploy the previous SHA tag. Instant and precise.
      - name: Build and push Docker image
        run: |
          IMAGE_TAG="${{ secrets.ACR_LOGIN_SERVER }}/${{ env.IMAGE_NAME }}:${{ github.sha }}"
          IMAGE_LATEST="${{ secrets.ACR_LOGIN_SERVER }}/${{ env.IMAGE_NAME }}:latest"

          docker build \
            -f docker/api/Dockerfile \
            -t "${IMAGE_TAG}" \
            -t "${IMAGE_LATEST}" \
            .

          docker push "${IMAGE_TAG}"
          docker push "${IMAGE_LATEST}"

          echo "IMAGE_TAG=${IMAGE_TAG}" >> $GITHUB_ENV

      # ── Run database migrations ───────────────────────────────────
      # Interview answer: "How do you run EF Core migrations in production?"
      # Option 1: App auto-migrates on startup (what we did in Docker Compose).
      # Option 2: Run migrations as a separate CD step BEFORE updating the app.
      # Option 2 is correct for production — if migration fails, old app
      # keeps running against the old schema. No downtime, safe rollback.
      - name: Run database migrations
        run: |
          # Get connection string from Key Vault
          CONN_STRING=$(az keyvault secret show \
            --vault-name ${{ secrets.KEY_VAULT_NAME }} \
            --name "postgres-connection-string" \
            --query "value" \
            --output tsv)

          # Install EF Core tools
          dotnet tool install --global dotnet-ef

          # Run migrations against production database
          ConnectionStrings__DefaultConnection="${CONN_STRING}" \
          dotnet ef database update \
            --project src/BookVault.Infrastructure \
            --startup-project src/BookVault.Api \
            --no-build
        env:
          ASPNETCORE_ENVIRONMENT: Production

      # ── Deploy new image to Container Apps ───────────────────────
      # Interview answer: "How does zero-downtime deployment work in Container Apps?"
      # Container Apps creates a new "revision" with the new image.
      # It starts the new revision's containers and runs health checks.
      # Only when the new revision is healthy does traffic shift to it.
      # The old revision stays running during the switchover — zero downtime.
      - name: Deploy to Container Apps
        id: deploy
        run: |
          az containerapp update \
            --name           ${{ secrets.CONTAINER_APP_NAME }} \
            --resource-group ${{ secrets.RESOURCE_GROUP }} \
            --image          ${{ env.IMAGE_TAG }} \
            --query          "properties.configuration.ingress.fqdn" \
            --output         tsv

          APP_URL=$(az containerapp show \
            --name           ${{ secrets.CONTAINER_APP_NAME }} \
            --resource-group ${{ secrets.RESOURCE_GROUP }} \
            --query          "properties.configuration.ingress.fqdn" \
            --output         tsv)

          echo "app_url=https://${APP_URL}" >> $GITHUB_OUTPUT
          echo "Deployed to: https://${APP_URL}"

      # ── Smoke test ───────────────────────────────────────────────
      # Interview answer: "What is a smoke test in deployment?"
      # A quick sanity check after deployment — is the app responding?
      # Not a full test suite — just enough to catch "app won't start" failures.
      # If smoke test fails, we alert and optionally auto-rollback.
      - name: Smoke test
        run: |
          APP_URL="${{ steps.deploy.outputs.app_url }}"
          echo "Running smoke test against ${APP_URL}"

          # Retry up to 6 times with 10s delay — Container Apps needs
          # time to start after scaling from zero
          for i in {1..6}; do
            STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
              "${APP_URL}/health" || echo "000")

            if [ "${STATUS}" = "200" ]; then
              echo "Smoke test passed (attempt ${i}) — status ${STATUS}"
              exit 0
            fi

            echo "Attempt ${i}: status ${STATUS} — retrying in 10s..."
            sleep 10
          done

          echo "Smoke test FAILED after 6 attempts"
          exit 1

      # ── Rollback on failure ──────────────────────────────────────
      - name: Rollback on failure
        if: failure()
        run: |
          echo "Deployment failed — rolling back to previous revision"

          # Get the previous revision name
          PREV_REVISION=$(az containerapp revision list \
            --name           ${{ secrets.CONTAINER_APP_NAME }} \
            --resource-group ${{ secrets.RESOURCE_GROUP }} \
            --query          "sort_by([?properties.active==\`true\`], &createdTime)[-2].name" \
            --output         tsv)

          if [ -n "${PREV_REVISION}" ]; then
            az containerapp ingress traffic set \
              --name           ${{ secrets.CONTAINER_APP_NAME }} \
              --resource-group ${{ secrets.RESOURCE_GROUP }} \
              --revision-weight "${PREV_REVISION}=100"
            echo "Traffic shifted back to: ${PREV_REVISION}"
          else
            echo "No previous revision to rollback to"
          fi