# Deploy to Azure - Quick Guide

## Prerequisites
1. **Azure Account** - Sign up at https://azure.microsoft.com/free/ (Free $200 credit)
2. **Azure CLI** - Install from https://aka.ms/installazurecli
3. **PowerShell** (Already on Windows)

## One-Click Deployment

Simply run this command in PowerShell:

```powershell
cd "d:\Mess management system\Semester Project"
.\deploy-azure.ps1
```

That's it! The script will:
- ✅ Create all Azure resources (SQL Server, Database, App Service)
- ✅ Deploy your application
- ✅ Configure everything automatically
- ✅ Give you a public URL

## What You'll Get

- **Public URL**: `https://messmgmt[XXXX].azurewebsites.net`
- **SSL Certificate**: Automatic HTTPS
- **Database**: Azure SQL Database
- **Time**: 15-20 minutes total
- **Cost**: Free for 30 days ($200 credit), then ~$28/month

## After Deployment

1. Visit the URL shown in the output
2. Register admin at: `/Admin/Register`
3. Register student at: `/Student/Register`

## Cleanup (Delete Everything)

```powershell
az group delete --name MessManagementRG --yes
```

## Troubleshooting

### Azure CLI Not Installed
Download from: https://aka.ms/installazurecli

### Not Logged In
Run: `az login`

### Need Help?
Contact: tahaba627926@gmail.com

## Manual Steps (If Script Fails)

1. **Login to Azure**
   ```powershell
   az login
   ```

2. **Create Resource Group**
   ```powershell
   az group create --name MessManagementRG --location eastus
   ```

3. **Create SQL Server**
   ```powershell
   az sql server create --name messsql1234 --resource-group MessManagementRG --location eastus --admin-user sqladmin --admin-password "Pass@word123!"
   ```

4. **Create Database**
   ```powershell
   az sql db create --resource-group MessManagementRG --server messsql1234 --name MessManagementDB --service-objective S0
   ```

5. **Create App Service**
   ```powershell
   az appservice plan create --name MessPlan --resource-group MessManagementRG --sku B1 --is-linux
   az webapp create --name messmgmt1234 --resource-group MessManagementRG --plan MessPlan --runtime "DOTNET:8.0"
   ```

6. **Deploy**
   ```powershell
   dotnet publish -c Release -o ./publish
   Compress-Archive -Path ./publish/* -DestinationPath ./deploy.zip
   az webapp deployment source config-zip --name messmgmt1234 --resource-group MessManagementRG --src ./deploy.zip
   ```

## Cost Breakdown

| Resource | SKU | Cost/Month |
|----------|-----|------------|
| App Service | B1 | $13 |
| SQL Database | S0 | $15 |
| **Total** | | **$28** |

**Note**: First 30 days FREE with $200 Azure credit!
