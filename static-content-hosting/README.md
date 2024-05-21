# Static Content Hosting Pattern

This document describes the Static Content Hosting Pattern example from the guide [Cloud Design Patterns](http://aka.ms/Cloud-Design-Patterns).


## Before you start

Ensure that you have installed all of the software prerequisites.

The example demonstrates operational aspects of static websites hosted in Azure Storage Accounts with support for static websites. Therefore, you will need to deploy this example in Azure.

## About the Example

This example shows how to reference static content from a publicly accessible storage service. The example contains steps to host a 404 HTML document and a stylesheet file into an Azure storage account. This type of content is typically deployed to the storage account as part of the application deployment process. However, to simplify the example as well as concentrate on the pattern itself, files are uploaded to the storage account by following the steps below.

![A diagram showing a client navigating to contoso.com home page hosted in an Azure App Service instance and getting as result the index.html file with dynamically generated links targeting files at static.contoso.com which is a componion Azure Storage account with static websites support enabled.](static-content-hosting-pattern.png)

The example is divided into two different parts:

1. `src/static` pre-generated content with a 404 HTML document and a stylesheet file. Both are going to be referenced from a index.html file.
1. `src/index.html` is a generated document during the deployment guide for the sake of simplicty. This is just a html file in your local working copy targeting the static hosted content. Typically, this _index.html_ is hosted from an Azure compute instance where its content including urls are dynamically generated.

When navigating the _index.html_ from your browser, all static resources are directly served out of the storage account, as opposed to being delivered by the compute instance.

## 🚀 Deployment guide

Install the prerequisites and follow the steps to deploy and run an example of the Static Content Hosting pattern.

> Note: at the time of writing this, [Azure storage account emulator(Azurite)](https://github.com/Azure/Azurite) doesn't provide with support to the static website feature.

### Prerequisites

1. Latest Azure CLI installed (must be at least 2.40) locally, or you can perform this from Azure Cloud Shell by clicking below or using devcontainers in GitHub.

   [![Launch Azure Cloud Shell](https://learn.microsoft.com/azure/includes/media/cloud-shell-try-it/launchcloudshell.png)](https://shell.azure.com)

   > 💡 The steps shown here and elsewhere in the example use Bash shell commands. On Windows, locally you can use the Windows Subsystem for Linux to run Bash.

1. An Azure subscription.

   The subscription used in this deployment cannot be a [free account](https://azure.microsoft.com/free); it must be a standard EA, pay-as-you-go, or Visual Studio benefit subscription. This is because the resources deployed here are beyond the quotas of free subscriptions.

   > :warning: The user or service principal initiating the deployment process *must* have the following minimal set of Azure role-based access control (RBAC) roles:
   >
   > - [User Access Administrator role](https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#user-access-administrator) is *required* at the subscription level since you'll be performing role assignments to managed identities across various resource groups.

### Steps

1. Clone/download this repo locally.

   ```bash
   git clone https://github.com/mspnp/cloud-design-patterns.git
   cd cloud-design-patterns/static-content-hosting
   ```

1. Log into Azure and create an empty resource group.

   ```azurecli
   az login
   az account set -s <Name or ID of subscription>
   ```

1. Create an empty resource group

   ```bash
   # [This takes less than one minute to run.]
   az group create -n rg-hosting-static-content -l eastus2
   ```

1. Control Plane - Deploy a new Azure storage account

   ```bash
   STORAGE_ACCOUNT_NAME="stvaletblobs$(LC_ALL=C tr -dc 'a-z0-9' < /dev/urandom | fold -w 7 | head -n 1)"

   # [This takes about one minute to run.]
   az deployment group create -n deploy-hosting-static-content -f bicep/main.bicep -g rg-hosting-static-content -p storageAccountName=$STORAGE_ACCOUNT_NAME
   ```

   > :warning: for those using devcontainers and running on arm platform, consider `az bicep uninstall && az bicep install --target-platform linux-arm64`

1. [Control Plane] RBAC your user with Storage Blob Data Contributor. This is required to be able to upload files to the recently created storage account

   ```bash
   ROLEASSIGNMENT_TO_UPLOAD_CONTENT=$(az role assignment create --role ba92f5b4-2d11-453d-a403-e96b0029c9fe --assignee-principal-type user --assignee-object-id $(az ad signed-in-user show --query 'id' -o tsv) --scope $(az storage account show -g rg-hosting-static-content -n $STORAGE_ACCOUNT_NAME --query 'id' -o tsv) --query 'id' -o tsv)
   echo ROLEASSIGNMENT_TO_UPLOAD_CONTENT: $ROLEASSIGNMENT_TO_UPLOAD_CONTENT
   ```

1. [Data Plane] Enable static website and upload content

   ```bash
   # Enables static website
   az storage blob service-properties update --account-name $STORAGE_ACCOUNT_NAME --static-website --404-document 404.html --index-document index.html --auth-mode login

   # upload more files
   az storage blob upload-batch -s "./src/static" --destination "\$web" --account-name $STORAGE_ACCOUNT_NAME --pattern "*.html" --content-type "text/html" --content-cache max-age=3600 --auth-mode login
   az storage blob upload-batch -s "./src/static" --destination "\$web" --account-name $STORAGE_ACCOUNT_NAME --pattern "*.css" --content-type "text/css" --content-cache max-age=3600 --auth-mode login
   ```

1. Obtain the public url

   ```bash
   # Retrieve the static website endpoint
   export STATIC_WEBSITE_URL=$(az storage account show -n $STORAGE_ACCOUNT_NAME -g rg-hosting-static-content  --query primaryEndpoints.web --output tsv)
   ```

1. Generate a _index.html_ document file for validation purposes

   ```bash
   cat > src/index.html << EOF
   <!DOCTYPE html>
   <html>
   <head>
       <link href="${STATIC_WEBSITE_URL}style.css" rel="stylesheet" type="text/css" data-preload="true"/>
   </head>
   <body>
       <h1>Welcome to contoso.com</h1>
   </body>
   </html>
   EOF
   ```

### 🏁 Try it out

Open the browsers of your preference and navigate to the website url

```bash
open src/index.html
```

🧹 Clean up

Remove the resource group that you created when you are done with this example.

```bash
az group delete -n rg-hosting-static-content -y
```
