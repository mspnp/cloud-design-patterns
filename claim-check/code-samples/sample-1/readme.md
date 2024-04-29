# Sample 1: Automatic claim check token generation with Event Grid, Storage Queues as messaging system

## Technologies used: Azure Blob Storage, Azure Event Grid, Azure Functions, Azure Storage Queue, .NET 8.0

This example uses Azure Blob Store to store the payload, but any service that supports [Event Grid](https://azure.microsoft.com/services/event-grid/) integration can be used. A client application uploads the payload to Azure Blob Store and Event Grid automatically generates an event, with a reference to the blob that can be used as a claim check token. The event is forwarded to an Azure Storage Queue from where it can be retrieved by the consumer sample app.

This approach allows a consumer application to poll the queue, get the message and then use the reference to Azure Storage Blob embedded in the message to download the payload directly from Azure Blob Storage. While not showcased in this sample, Azure Functions can also consume the Event Grid message directly.

> This example uses [`DefaultAzureCredential`](https://learn.microsoft.com/dotnet/azure/sdk/authentication/#defaultazurecredential) for authentication while accessing Azure resources. the user principal must be provided as a parameter to the included Bicep script. The Bicep script is responsible for assigning the necessary RBAC (Role-Based Access Control) permissions for accessing the various Azure resources. While the principal can be the account associated with the interactive user, there are alternative [configurations](https://learn.microsoft.com/dotnet/azure/sdk/authentication/?tabs=command-line#exploring-the-sequence-of-defaultazurecredential-authentication-methods) available.

![Diagram.](images/sample-1-diagram.png)

## :rocket: Deployment guide

Install the prerequisites and follow the steps to deploy and run the examples.

## Prerequisites

- Permission to create a new resource group and resources in an [Azure subscription](https://azure.com/free)
- Unix-like shell. Also available in:
  - [Azure Cloud Shell](https://shell.azure.com/)
  - [Windows Subsystem for Linux (WSL)](https://learn.microsoft.com/windows/wsl/install)
- [Git](https://git-scm.com/downloads)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local#install-the-azure-functions-core-tools)
- [Azurite](/azure/storage/common/storage-use-azurite)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
- Optionally, an IDE, like  [Visual Studio](https://visualstudio.microsoft.com/downloads/) or [Visual Studio Code](https://code.visualstudio.com/). 

## Getting Started

Make sure you have WSL (Windows System For Linux) installed and have AZ CLI version > 2.0.50. Before running any script also make sure you are authenticated on AZ CLI using

### Steps

1. Clone this repository to your workstation and navigate to the working directory.

   ```shell
   git clone https://github.com/mspnp/cloud-design-patterns
   cd cloud-design-patterns/claim-check/code-samples/sample-1
   ```

1. Log into Azure and create an empty resource group.

   ```azurecli
   az login
   az account set -s <Name or ID of subscription>

   NAME_PREFIX=<unique value between three to five characters>
   az group create -n "rg-${NAME_PREFIX}" -l eastus2
   ```

1. Deploy the supporting Azure resources.

   ```azurecli
   CURRENT_USER_OBJECT_ID=$(az ad signed-in-user show -o tsv --query id)

   # This could take a few minutes
   az deployment group create -n deploy-claim-check -f bicep/main.bicep -g "rg-${NAME_PREFIX}" -p namePrefix=$NAME_PREFIX principalId=$CURRENT_USER_OBJECT_ID
   ```

1. Configure the sample consumer to use the created Azure resources.

   ```shell
   sed "s/{STORAGE_ACCOUNT_NAME}/st${NAME_PREFIX}cc/g" FunctionConsumer1/local.settings.json.template > FunctionConsumer1/local.settings.json
   ```

1. [Run Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite#run-azurite) blob storage emulation service.

   > The local storage emulator is required as an Azure Storage account is a required "backing resource" for Azure Functions.

1. Launch the consumer sample application to receive and process claim check messages from Storage Queue.

   The message consumer sample application for this scenario is implemented as an Azure Function, showcasing the serveless approach. Run the sample application to connect to the the queue and process messages as they arrive.

   ```bash
   cd sample-1\FunctionConsumer1
   func start
   ```

  > Please note: For demo purposes, the sample consumer application will write the payload content to the the screen. Keep that in mind before you try sending really large payloads.

### :checkered_flag: Try it out

To generate a claim check message you just have to drop a file in the created Azure Storage account. You can use Azure **Storage browser** to do that. [Refer this to know how to upload blobs to a container using Storage Explorer](https://learn.microsoft.com/azure/storage/blobs/quickstart-storage-explorer#upload-blobs-to-the-container).

### :broom: Clean up

Remove the resource group that you created when you are done with this sample.

```azurecli
az group delete -n "rg-${NAME_PREFIX}"
```
