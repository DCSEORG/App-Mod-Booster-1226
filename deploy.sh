#!/bin/bash
# deploy.sh - Deploy Expense Management App WITHOUT GenAI services
# Usage: bash deploy.sh
# Prerequisites: az CLI logged in, jq installed

set -e

# ── Variables (update these before running) ─────────────────────────────────
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"
ADMIN_OBJECT_ID="<your-object-id>"      # az ad signed-in-user show --query id -o tsv
ADMIN_LOGIN="<your-upn>"               # az ad signed-in-user show --query userPrincipalName -o tsv

echo "============================================"
echo " Expense Management - Infrastructure Deploy"
echo "============================================"

# ── 1. Create Resource Group ─────────────────────────────────────────────────
echo "Step 1: Creating resource group '$RESOURCE_GROUP' in '$LOCATION'..."
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none
echo "  ✓ Resource group ready"

# ── 2. Deploy Infrastructure ─────────────────────────────────────────────────
echo "Step 2: Deploying infrastructure (App Service + SQL)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file infra/main.bicep \
  --parameters adminObjectId="$ADMIN_OBJECT_ID" adminLogin="$ADMIN_LOGIN" deployGenAI=false \
  --query properties.outputs -o json)

# ── 3. Extract Outputs ────────────────────────────────────────────────────────
echo "Step 3: Extracting deployment outputs..."
APP_SERVICE_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.appServiceName.value')
SQL_SERVER_FQDN=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.sqlServerFqdn.value')
DATABASE_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.databaseName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.managedIdentityClientId.value')
MANAGED_IDENTITY_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.managedIdentityName.value')
SQL_SERVER_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.sqlServerName.value')

echo "  App Service : $APP_SERVICE_NAME"
echo "  SQL Server  : $SQL_SERVER_FQDN"
echo "  Database    : $DATABASE_NAME"
echo "  Identity    : $MANAGED_IDENTITY_NAME"

# ── 4. Configure App Service Settings ────────────────────────────────────────
echo "Step 4: Configuring App Service application settings..."
az webapp config appsettings set \
  --name "$APP_SERVICE_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --settings \
  "ConnectionStrings__DefaultConnection=Server=tcp:${SQL_SERVER_FQDN},1433;Database=${DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};" \
  "AZURE_CLIENT_ID=${MANAGED_IDENTITY_CLIENT_ID}" \
  --output none
echo "  ✓ App Service settings configured"

# ── 5. Add Firewall Rules ─────────────────────────────────────────────────────
echo "Step 5: Configuring SQL firewall rules..."
MY_IP=$(curl -s https://api.ipify.org)

az sql server firewall-rule create \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER_NAME" \
  --name "AllowAllAzureIPs" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0 \
  --output none

az sql server firewall-rule create \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER_NAME" \
  --name "AllowDeploymentIP" \
  --start-ip-address "$MY_IP" \
  --end-ip-address "$MY_IP" \
  --output none

echo "  ✓ Firewall rules set (Azure services + $MY_IP)"
echo "  Waiting 15 seconds for firewall rules to propagate..."
sleep 15

# ── 6. Wait for SQL to be Ready ───────────────────────────────────────────────
echo "Step 6: Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30

# ── 7. Replace Managed Identity placeholder in script.sql ────────────────────
echo "Step 7: Preparing managed identity DB role script..."
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak

# ── 8. Install Python Dependencies ───────────────────────────────────────────
echo "Step 8: Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity

# ── 9. Run Schema Import ──────────────────────────────────────────────────────
echo "Step 9: Importing database schema..."
# Update server/db in python scripts
sed -i.bak "s|SERVER = \"example.database.windows.net\"|SERVER = \"$SQL_SERVER_FQDN\"|g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s|DATABASE = \"Northwind\"|DATABASE = \"$DATABASE_NAME\"|g" run-sql.py && rm -f run-sql.py.bak
python3 run-sql.py

# ── 10. Run DB Role Setup ──────────────────────────────────────────────────────
echo "Step 10: Configuring managed identity database roles..."
sed -i.bak "s|SERVER = \"example.database.windows.net\"|SERVER = \"$SQL_SERVER_FQDN\"|g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s|DATABASE = \"Northwind\"|DATABASE = \"$DATABASE_NAME\"|g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
python3 run-sql-dbrole.py

# ── 11. Run Stored Procedures Deployment ─────────────────────────────────────
echo "Step 11: Deploying stored procedures..."
sed -i.bak "s|SERVER = \"example.database.windows.net\"|SERVER = \"$SQL_SERVER_FQDN\"|g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
sed -i.bak "s|DATABASE = \"Northwind\"|DATABASE = \"$DATABASE_NAME\"|g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
python3 run-sql-stored-procs.py

# ── 12. Deploy App Code ────────────────────────────────────────────────────────
echo "Step 12: Deploying application code..."
az webapp deploy \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_SERVICE_NAME" \
  --src-path ./app.zip \
  --output none
echo "  ✓ App deployed"

echo ""
echo "============================================"
echo " Deployment Complete!"
echo "============================================"
echo ""
echo " App URL : https://${APP_SERVICE_NAME}.azurewebsites.net/Index"
echo ""
echo " Note: Navigate to /Index (not root) to view the app."
echo " Chat UI is available but GenAI is not configured."
echo " To enable chat with AI, run: bash deploy-with-chat.sh"
echo ""
