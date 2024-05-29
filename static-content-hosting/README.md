# Static Content Hosting pattern example

This directory contains an example of the [Static Content Hosting cloud design pattern](https://learn.microsoft.com/azure/architecture/patterns/static-content-hosting).

This example shows how to reference static content from a publicly accessible storage service. The example contains steps to host a 404 HTML document and a stylesheet file into an Azure storage account. This type of content is typically deployed to the storage account as part of the application deployment process. However, to simplify the example as well as concentrate on the pattern itself, files are uploaded to the storage account by following the steps below.

![A diagram showing a client navigating to contoso.com home page hosted in an Azure App Service instance and getting as result the index.html file with dynamically generated links targeting files at static.contoso.com which is a componion Azure Storage account with static websites support enabled.](static-content-hosting-pattern.png)

The example is divided into two different parts:

1. `src/static` pre-generated content with a 404 HTML document and a stylesheet file. Both are going to be referenced from an index.html file.
1. `src/index.html` is a generated document during the deployment guide for the sake of simplicity. This is just a html file in your local working copy targeting the static hosted content. Typically, this file would be hosted from an Azure compute instance where its content including URLs are dynamically generated.

When navigating the **index.html** from your browser, all static resources are directly served out of the storage account, as opposed to being delivered by the compute instance.

## :rocket: Deployment guide

Install the prerequisites and follow the steps to deploy and run an example of the Static Content Hosting pattern.

> Note: at the time of writing this, [Azure storage account emulator(Azurite)](https://github.com/Azure/Azurite) doesn't provide with support to the static website feature.

### Prerequisites

- Permission to create a new resource group and resources in an [Azure subscription](https://azure.com/free)
- Unix-like shell. Also available in:
  - [Azure Cloud Shell](https://shell.azure.com/)
  - [Windows Subsystem for Linux (WSL)](https://learn.microsoft.com/windows/wsl/install)
- [Git](https://git-scm.com/downloads)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)

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
   az deployment group create -n deploy-hosting-static-content -f bicep/main.bicep -g rg-hosting-static-content -p storageAccountName=$STORAGE_ACCOUNT_NAME assigneeObjectId=$(az ad signed-in-user show --query 'id' -o tsv)
   ```

   > :warning: for those using devcontainers and running on arm platform, consider `az bicep uninstall && az bicep install --target-platform linux-arm64`

1. [Data Plane] Enable static website and upload content

   ```bash
   # Enables static website
   az storage blob service-properties update --account-name $STORAGE_ACCOUNT_NAME --static-website --404-document 404.html --auth-mode login

   # upload static content
   az storage blob upload-batch -s "./src/static" --destination "\$web" --account-name $STORAGE_ACCOUNT_NAME --pattern "*.html" --content-type "text/html" --content-cache max-age=3600 --auth-mode login
   az storage blob upload-batch -s "./src/static" --destination "\$web" --account-name $STORAGE_ACCOUNT_NAME --pattern "*.css" --content-type "text/css" --content-cache max-age=3600 --auth-mode login
   ```

1. Obtain the public URL of the storage account.

   ```bash
   # Retrieve the static website endpoint
   export STATIC_WEBSITE_URL=$(az storage account show -n $STORAGE_ACCOUNT_NAME -g rg-hosting-static-content  --query primaryEndpoints.web --output tsv)
   ```

1. Replace the url placeholder from the _index.html_ document using the recently created storage account static website url

   ```bash
   sed "s#<static-website-url>#${STATIC_WEBSITE_URL}#g" ./src/index.template.html > ./src/index.html
   ```

### :checkered_flag: Try it out

Open the browser of your preference and navigate to the website URL.

```bash
open src/index.html
```

### :broom: Clean up

Remove the resource group that you created when you are done with this example.

```bash
az group delete -n rg-hosting-static-content -y
```

## Related documentation

- [Static website hosting in Azure Storage](https://learn.microsoft.com/azure/storage/blobs/storage-blob-static-website)
- [Map a custom domain to an Azure Blob Storage endpoint](https://learn.microsoft.com/azure/storage/blobs/storage-custom-domain-name)

## Contributions

Please see our [Contributor guide](../CONTRIBUTING.md).

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact <opencode@microsoft.com> with any additional questions or comments.

With :heart: from Azure patterns & practices, [Azure Architecture Center](https://azure.com/architecture).

