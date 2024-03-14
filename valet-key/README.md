# Valet Key pattern example

This directory contains an example of the [Valet Key cloud design pattern](https://learn.microsoft.com/azure/architecture/patterns/valet-key).

This example shows how a client application can obtain necessary permissions to write directly to a storage destination, bypassing a server component that would not add value and introduce additional latency and risk to the operation.

Specifically this sample includes an Azure Function that provides a scoped, time-limited shared access signature (SaS) to authorized callers, who would then use that SaS token to perform a data upload to the storage account without consuming the resources of the Azure Function to proxy that request.

## :rocket: Deployment guide

Install the prerequisites and follow the steps to deploy and run an example of the Valet Key pattern.

### Prerequisites

- Permission to create a new resource group and resources in an [Azure subscription](https://azure.com/free)
- [Git](https://git-scm.com/downloads)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local#install-the-azure-functions-core-tools)
- [Azurite](/azure/storage/common/storage-use-azurite)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)

### Steps

1. Clone this repository to your workstation and navigate to the working directory.

   ```shell
   git clone https://github.com/mspnp/cloud-design-patterns
   cd valet-key
   ```

1. Log into Azure and create an empty resource group.

   ```azurecli
   az login
   az account set -s <Name or ID of subscription>

   RESOURCE_GROUP_NAME=rg-valet-key
   az group create -n $RESOURCE_GROUP_NAME -l eastus2
   ```

1. Deploy destination Azure Storage account.

   ```azurecli
   CURRENT_USER_OBJECT_ID=$(az ad signed-in-user show -o tsv --query id)
   STORAGE_ACCOUNT_NAME="stvaletblobs$(LC_ALL=C tr -dc 'a-z0-9' < /dev/urandom | fold -w 7 | head -n 1)"

   # This takes about one minute
   az deployment group create -n deploy-valet-key -f bicep/main.bicep -g $RESOURCE_GROUP_NAME -p storageAccountName=$STORAGE_ACCOUNT_NAME principalId=$CURRENT_USER_OBJECT_ID
   ```

1. Configure local function and client to use the deployed Storage account.

   ```shell
   sed "s/STORAGE_ACCOUNT_NAME/${STORAGE_ACCOUNT_NAME}/g" ValetKey.Function/local.settings.template.json > ValetKey.Function/local.settings.json
   sed "s/STORAGE_ACCOUNT_NAME/${STORAGE_ACCOUNT_NAME}/g" ValetKey.Client/local.settings.template.json > ValetKey.Client/local.settings.json
   ```

<!-- 
1. Build the Azure Function.

   ```bash
   cd ValetKey.Function

   dotnet publish -o publish/
   cd publish && zip -r publish-00.zip * && cd ..
   ```

1. Deploy the Azure Function code.

   > This storage account uses account key based access for uploading the .zip from your console. In a production system your pipeline's identity would be granted RBAC access and the agent would be running from a private network.

   ```bash
   az storage blob upload -f publish/publish-00.zip -c function-deployments --account-name $FUNCTION_STORAGE_ACCOUNT_NAME
   az functionapp config appsettings set -n functionappck04 -g rg-valet-key --settings WEBSITE_RUN_FROM_PACKAGE=$(az storage blob url --account-name $FUNCTION_STORAGE_ACCOUNT_NAME --container-name function-deployments -n publish-00.zip -o tsv)
   ```
-->

### :checkered_flag: Try it out

### Launch valet key API

1. [Run Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite#run-azurite)

   > The local storage emulator is required as an Azure Storage account is a required "backing resource" for Azure Functions.

1. Start the valet key API.

   ```bash
   func start ValetKey.Function/
   ```

   > This Azure Function implicitly uses `DefaultAzureCredential` for to authenticate to the Azure storage account as your identity with **Storage Blob Delegator**. This must be [configured in your environment](https://learn.microsoft.com/dotnet/azure/sdk/authentication/?tabs=command-line#exploring-the-sequence-of-defaultazurecredential-authentication-methods), and if you followed the instructions it already is with your usage of `az login`. The Azure Function is executing under your identity when running locally.

### Launch client

1. Run the client that will use the valet key.

   It's recommended to run this from another terminal instance.

   ```bash
   dotnet run --project ValetKey.Client
   ```

   The client will reach out to the valet-key API and request a scope and time-limited SaS token. The API will generate one for the client and return it to the client. The client will use that token to upload a file.

### Validate the blob has been uploaded

1. Open the the **Storage browser** on your Storage Account.
1. Select **Blob containers**.
1. Click on the container named `uploads`.
1. You should be able to see the list of uploaded blobs.

### :broom: Clean up

Remove the storage account when you are done with this sample.
