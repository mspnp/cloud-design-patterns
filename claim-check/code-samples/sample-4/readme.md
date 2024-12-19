# Sample 4: Manual token generation, Azure Event Hubs with Kafka as messaging system

## Technologies used: Azure Blob Storage, Azure Event Hubs with Kafka API enabled, Azure Functions, .NET 9.0

In this example the client application uploads the payload to Azure Blob Storage and manually generates the claim check token, which is sent via Event Hubs.

The sample producer CLI application uses the Apache Kafka libraries to send the messages to [Event Hubs with Kafka enabled](https://learn.microsoft.com/azure/event-hubs/event-hubs-create-kafka-enabled), to demonstrate the ease of using other Azure services like Azure Blob Storage, Azure functions etc. with a different messaging protocol like Kafka. The Azure Function is used to demonstrate a client application that acts as the consumer for the payload.

> This example uses [`DefaultAzureCredential`](https://learn.microsoft.com/dotnet/azure/sdk/authentication/#defaultazurecredential) for authentication while accessing Azure resources. the user principal must be provided as a parameter to the included Bicep script. The Bicep script is responsible for assigning the necessary RBAC (Role-Based Access Control) permissions for accessing the various Azure resources. While the principal can be the account associated with the interactive user, there are alternative [configurations](https://learn.microsoft.com/dotnet/azure/sdk/authentication/?tabs=command-line#exploring-the-sequence-of-defaultazurecredential-authentication-methods) available.

![A diagram showing a client CLI application acting as a producer and an Azure Function as the consumer, with Azure Blob Storage serving as the data store and Event Hubs as the messaging syste. The producer uploads the payload to Blob Storage, manually creates the claim-check message containing the blob location, and sends the message using the Kafka API to Event Hubs. The consumer Function receives the message from Event Hubs, extracts the reference, and dowloads the blob from the storage account.](images/sample-4-diagram.png)

1. The producer CLI application uploads the payload to Azure Blob Storage.
1. The producer creates the claim-check message containing the blob location, and sends the message using the Kafka API to Event Hubs.
1. The consumer Function receives the message from Event Hubs.
1. The Function extracts the reference to the payload blob from the message and downloads the blob directly from storage.

## :rocket: Deployment guide

Install the prerequisites and follow the steps to deploy and run the examples.

### Prerequisites

- Permission to create a new resource group and resources in an [Azure subscription](https://azure.com/free)
- Unix-like shell. Also available in:
  - [Azure Cloud Shell](https://shell.azure.com/)
  - [Windows Subsystem for Linux (WSL)](https://learn.microsoft.com/windows/wsl/install)
- [Git](https://git-scm.com/downloads)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local#install-the-azure-functions-core-tools)
- [Azurite](/azure/storage/common/storage-use-azurite)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
- Optionally, an IDE, like  [Visual Studio](https://visualstudio.microsoft.com/downloads/) or [Visual Studio Code](https://code.visualstudio.com/).

### Steps

1. Clone this repository to your workstation and navigate to the working directory.

   ```bash
   git clone https://github.com/mspnp/cloud-design-patterns
   cd cloud-design-patterns/claim-check/code-samples/sample-4
   ```

1. Log into Azure and create an empty resource group.

   ```bash
   az login
   az account set -s <Name or ID of subscription>

   NAME_PREFIX=<unique value between three to five characters of length>
   LOCATION=eastus2
   RESOURCE_GROUP_NAME="rg-${NAME_PREFIX}-${LOCATION}"

   az group create -n "${RESOURCE_GROUP_NAME}" -l ${LOCATION}
   ```

1. Deploy the supporting Azure resources.

   ```bash
   CURRENT_USER_OBJECT_ID=$(az ad signed-in-user show -o tsv --query id)

   # This could take a few minutes
   az deployment group create -n deploy-claim-check -f bicep/main.bicep -g "${RESOURCE_GROUP_NAME}" -p namePrefix=$NAME_PREFIX principalId=$CURRENT_USER_OBJECT_ID
   ```

1. Configure the samples to use the created Azure resources.

   ```bash
   sed "s/{EVENT_HUBS_NAMESPACE}/evhns-${NAME_PREFIX}/g" ClientProducer4/appsettings.json.template >ClientProducer4/appsettings.json
   sed -i "s/{STORAGE_ACCOUNT_NAME}/st${NAME_PREFIX}cc/g" ClientProducer4/appsettings.json

   sed "s/{EVENT_HUBS_NAMESPACE}/evhns-${NAME_PREFIX}/g" FunctionConsumer4/local.settings.json.template > FunctionConsumer4/local.settings.json
   ```

1. [Run Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite#run-azurite) blob storage emulation service.

   > The local storage emulator is required as an Azure Storage account is a required "backing resource" for Azure Functions.

1. Launch the Function sample that will process the claim check messages as they arrive to Event Hubs.

   ```bash
   cd ./FunctionConsumer4
   func start
   ```

  > Please note: For demo purposes, the sample application will write the payload content to the the screen. Keep that in mind before you try sending really large payloads.

### :checkered_flag: Try it out

1. Run the CLI sample that will generate a claim check message and send it to Event Hubs using the Kafka Api.

   _You'll need to run this from another terminal._

  ```bash
  dotnet run --project ./ClientProducer4
  ```

### :broom: Clean up

Remove the resource group that you created when you are done with this sample.

```bash
az group delete -n "${RESOURCE_GROUP_NAME}" -y
```
