# Set your variables
$RESOURCE_GROUP = "MessManagementRG"
$LOCATION = "eastus"
$APP_NAME = "messmgmt" + (Get-Random -Maximum 9999)
$SQL_SERVER = "messsql" + (Get-Random -Maximum 9999)
$SQL_DB = "MessManagementDB"
$SQL_ADMIN = "sqladmin"
$SQL_PASSWORD = "Pass@word123!"
$PROJECT_PATH = "d:\Mess management system\Semester Project"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Mess Management System - Azure Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Login to Azure
Write-Host "Step 1: Logging in to Azure..." -ForegroundColor Yellow
az login
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to login to Azure. Please check your credentials." -ForegroundColor Red
    exit 1
}

# Create Resource Group
Write-Host ""
Write-Host "Step 2: Creating Resource Group..." -ForegroundColor Yellow
az group create --name $RESOURCE_GROUP --location $LOCATION
Write-Host "Resource Group created: $RESOURCE_GROUP" -ForegroundColor Green

# Create SQL Server
Write-Host ""
Write-Host "Step 3: Creating SQL Server..." -ForegroundColor Yellow
az sql server create `
    --name $SQL_SERVER `
    --resource-group $RESOURCE_GROUP `
    --location $LOCATION `
    --admin-user $SQL_ADMIN `
    --admin-password $SQL_PASSWORD
Write-Host "SQL Server created: $SQL_SERVER" -ForegroundColor Green

# Create SQL Database
Write-Host ""
Write-Host "Step 4: Creating SQL Database..." -ForegroundColor Yellow
az sql db create `
    --resource-group $RESOURCE_GROUP `
    --server $SQL_SERVER `
    --name $SQL_DB `
    --service-objective S0
Write-Host "Database created: $SQL_DB" -ForegroundColor Green

# Configure Firewall
Write-Host ""
Write-Host "Step 5: Configuring SQL Server Firewall..." -ForegroundColor Yellow
az sql server firewall-rule create `
    --resource-group $RESOURCE_GROUP `
    --server $SQL_SERVER `
    --name AllowAzureServices `
    --start-ip-address 0.0.0.0 `
    --end-ip-address 0.0.0.0
Write-Host "Firewall configured" -ForegroundColor Green

# Create App Service Plan
Write-Host ""
Write-Host "Step 6: Creating App Service Plan..." -ForegroundColor Yellow
az appservice plan create `
    --name "MessPlan" `
    --resource-group $RESOURCE_GROUP `
    --location $LOCATION `
    --sku B1 `
    --is-linux
Write-Host "App Service Plan created" -ForegroundColor Green

# Create Web App
Write-Host ""
Write-Host "Step 7: Creating Web App..." -ForegroundColor Yellow
az webapp create `
    --name $APP_NAME `
    --resource-group $RESOURCE_GROUP `
    --plan "MessPlan" `
    --runtime "DOTNET:8.0"
Write-Host "Web App created: $APP_NAME" -ForegroundColor Green

# Configure Connection String
Write-Host ""
Write-Host "Step 8: Configuring Connection String..." -ForegroundColor Yellow
$CONNECTION_STRING = "Server=tcp:$SQL_SERVER.database.windows.net,1433;Initial Catalog=$SQL_DB;User ID=$SQL_ADMIN;Password=$SQL_PASSWORD;Encrypt=True;TrustServerCertificate=False;"

az webapp config connection-string set `
    --name $APP_NAME `
    --resource-group $RESOURCE_GROUP `
    --connection-string-type SQLAzure `
    --settings DefaultConnection=$CONNECTION_STRING
Write-Host "Connection string configured" -ForegroundColor Green

# Set Application Settings
Write-Host ""
Write-Host "Step 9: Setting Application Environment..." -ForegroundColor Yellow
az webapp config appsettings set `
    --name $APP_NAME `
    --resource-group $RESOURCE_GROUP `
    --settings ASPNETCORE_ENVIRONMENT="Production"
Write-Host "Environment configured" -ForegroundColor Green

# Publish Application
Write-Host ""
Write-Host "Step 10: Publishing Application..." -ForegroundColor Yellow
cd $PROJECT_PATH
dotnet publish -c Release -o ./publish
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to publish application." -ForegroundColor Red
    exit 1
}
Write-Host "Application published" -ForegroundColor Green

# Create Deployment Package
Write-Host ""
Write-Host "Step 11: Creating Deployment Package..." -ForegroundColor Yellow
if (Test-Path "./deploy.zip") {
    Remove-Item "./deploy.zip"
}
Compress-Archive -Path ./publish/* -DestinationPath ./deploy.zip
Write-Host "Deployment package created" -ForegroundColor Green

# Deploy to Azure
Write-Host ""
Write-Host "Step 12: Deploying to Azure (this may take a few minutes)..." -ForegroundColor Yellow
az webapp deployment source config-zip `
    --name $APP_NAME `
    --resource-group $RESOURCE_GROUP `
    --src ./deploy.zip
Write-Host "Application deployed successfully!" -ForegroundColor Green

# Display Results
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deployment Completed Successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Application URL: https://$APP_NAME.azurewebsites.net" -ForegroundColor Yellow
Write-Host "SQL Server: $SQL_SERVER.database.windows.net" -ForegroundColor Yellow
Write-Host "Database: $SQL_DB" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Visit https://$APP_NAME.azurewebsites.net" -ForegroundColor White
Write-Host "2. Register admin account at /Admin/Register" -ForegroundColor White
Write-Host "3. Register student account at /Student/Register" -ForegroundColor White
Write-Host ""
Write-Host "To delete all resources later, run:" -ForegroundColor Cyan
Write-Host "az group delete --name $RESOURCE_GROUP --yes" -ForegroundColor White
Write-Host ""

# Save deployment info
$deploymentInfo = @"
Deployment Information
======================
Date: $(Get-Date)
Resource Group: $RESOURCE_GROUP
App Name: $APP_NAME
App URL: https://$APP_NAME.azurewebsites.net
SQL Server: $SQL_SERVER.database.windows.net
SQL Database: $SQL_DB
SQL Admin: $SQL_ADMIN
SQL Password: $SQL_PASSWORD
Connection String: $CONNECTION_STRING
"@

$deploymentInfo | Out-File -FilePath "./deployment-info.txt"
Write-Host "Deployment details saved to: deployment-info.txt" -ForegroundColor Green
Write-Host ""
Write-Host "Opening browser..." -ForegroundColor Yellow
Start-Process "https://$APP_NAME.azurewebsites.net"
