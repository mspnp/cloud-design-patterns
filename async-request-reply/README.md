# Asynchronous Request-Reply pattern

Decouple backend processing from a frontend host, where backend processing needs to be asynchronous, but the frontend still needs a clear response.

For more information about this pattern, see [Asynchronous Request-Reply pattern](https://learn.microsoft.com/azure/architecture/patterns/async-request-reply) on the Azure Architecture Center.

![Data flow of the async request-reply pattern](https://learn.microsoft.com/azure/architecture/patterns/_images/async-request-fn.png)  

The implementation uses a managed identity to control access to your storage accounts and Service Bus in the code, which is highly recommended wherever possible as a security best practice.

When running [Function App in a Consumption](https://techcommunity.microsoft.com/t5/apps-on-azure-blog/use-managed-identity-instead-of-azurewebjobsstorage-to-connect-a/ba-p/3657606), your app uses the WEBSITE_AZUREFILESCONNECTIONSTRING and WEBSITE_CONTENTSHARE settings when connecting to Azure Files on the storage account used by your function app. Azure Files doesn't support using managed identity when accessing the file share. Then that reference is using connection string. The manage identity is used on aplication dependencies.  

The typical way to generate a SAS token in code requires the storage account key. In this scenario, you won’t have a storage account key, so you’ll need to find another way to generate the shared access signatures. To do that, we need to use an approach called “user delegation” SAS . By using a user delegation SAS, we can sign the signature with the Azure Ad credentials instead of the storage account key.

## Deploying the sample

### Prerequisites

- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli?view=azure-cli-latest)
- [.NET Core SDK version 8](https://dotnet.microsoft.com/en-us/download)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local#v4)

### Deploy the Azure resources

1. Clone or download this repo.

2. Navigate to the `async-request-reply` folder.

   ```bash
   cd async-request-reply
   ```

3. Azure Login

   Our journey begins with logging into Azure. Use the command below:

   ```bash
   az login
   # Optionally, set the default subscription:
   # az account set --subscription <subscription_id>
   ```

4. Environment Setup

   We need to prepare our environment:

   ```bash
   export LOCATION=eastus
   export RESOURCEGROUP_BASE_NAME=rg-asyncrequestreply
   export RESOURCEGROUP=${RESOURCEGROUP_BASE_NAME}-${LOCATION}
   ```

5. Create a resource group.

   ```bash
   az group create --name ${RESOURCEGROUP} --location ${LOCATION}
   ```

6. Deploy the template.
   All the resources are going to be created on the resouce group location.

   ```bash
   az deployment group create -g ${RESOURCEGROUP} -f deploy.bicep
   ```

7. Wait for the deployment to complete.

### Deploy the Functions app

1. Navigate to the `async-request-reply/src` folder.

   ```bash
   cd src
   ```

2. Deploy the app.

   ```bash
   FUNC_APP_NAME=$(az deployment group show -g ${RESOURCEGROUP} -n deploy --query properties.outputs.functionAppName.value -o tsv)

   func azure functionapp publish $FUNC_APP_NAME --dotnetIsolated
   ```

### Validate the Azure Function app

1. Send an http request through the Async Processor Work Acceptor

   ```bash
   curl -X POST "https://${FUNC_APP_NAME}.azurewebsites.net/api/asyncprocessingworkacceptor" --header 'Content-Type: application/json' --header 'Accept: application/json' -k -i -d '{
      "id": "1234",
      "customername": "Contoso"
   }'
   ```

   The response will be something like:

   ```bash
    HTTP/1.1 202 Accepted
    Content-Length: 155
    Content-Type: application/json; charset=utf-8
    Date: Wed, 13 Dec 2023 20:18:55 GMT
    Location: http://<appservice-name>.azurewebsites.net/api/RequestStatus/<guid>
   ```

   Using a browser open the url from the _Location_ field in the response. A file with the data you have sent will be downloaded.

   > **Note** the app uses the WEBSITE_HOSTNAME environment variable. This environment variable is set automatically by the Azure App Service runtime environment. For more information, see [Azure runtime environment](https.://github.com/projectkudu/kudu/wiki/Azure-runtime-environment)

### Clean up

1. Delete all the Azure resources for this Cloud Async Pattern

   ```bash
   az group delete -n ${RESOURCEGROUP} -y
   ```

### Running localy

You could open the solution with Visual Studio, then you need to create on the root `local.settings.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ServiceBusConnection__fullyQualifiedNamespace": "<yourData>",
    "DataStorage__blobServiceUri": "<yourData>"
  }
}
```

As far the implementation is using manage identity, you need to assign the role to your [developer identity](https://learn.microsoft.com/azure/azure-functions/functions-reference?tabs=blob&pivots=programming-language-csharp#local-development-with-identity-based-connections).

```yarm
// Assign Role to allow sending messages to the Service Bus
resource serviceBusSenderRoleAssignmentUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, 'LocalUser', 'ServiceBusSenderRole')
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: senderServiceBusRole
    principalId: <your user object id>
    principalType: 'User'
  }
}

// Assign Role to allow receiving messages from the Service Bus
resource serviceBusReceiverRoleAssignmentUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, 'LocalUser', 'ServiceBusReceiverRole')
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: receiverServiceBusRole
    principalId: <your user object id>
    principalType: 'User'
  }
}

// Assign Role to allow Read, write, and delete Azure Storage containers and blobs. 
resource dataStorageBlobDataContributorRoleAssignmentUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, 'LocalUser', 'StorageBlobDataContributorRole')
  scope: dataStorageAccount
  properties: {
 roleDefinitionId: receiverServiceBusRole
    principalId: <your user object id>
    principalType: 'User'
  }
}
```
