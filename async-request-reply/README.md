# Asynchronous Request-Reply pattern

Decouple backend processing from a frontend host, where backend processing needs to be asynchronous, but the frontend still needs a clear response.

For more information about this pattern, see [Asynchronous Request-Reply pattern](https://learn.microsoft.com/azure/architecture/patterns/async-request-reply) on the Azure Architecture Center.

![Data flow of the async request-reply pattern](https://learn.microsoft.com/azure/architecture/patterns/_images/async-request-fn.png)  

The implementation uses a managed identity to control access to your storage accounts and Service Bus in the code, which is highly recommended wherever possible as a security best practice.

The reference implementation was moved to [Azure Functions Flex Consumption plan hosting](https://learn.microsoft.com/azure/azure-functions/flex-consumption-plan). Flex Consumption is a Linux-based Azure Functions hosting plan that builds on the Consumption pay for what you use serverless billing model. It gives you more flexibility and customizability by introducing private networking, instance memory size selection, and fast/large scale-out features still based on a serverless model. It also allows access the internal storage account by manage identity, it was not possible [before](https://techcommunity.microsoft.com/t5/apps-on-azure-blog/use-managed-identity-instead-of-azurewebjobsstorage-to-connect-a/ba-p/3657606)

The typical way to generate a SAS token in code requires the storage account key. In this scenario, you won’t have a storage account key, so you’ll need to find another way to generate the shared access signatures. To do that, we need to use an approach called “user delegation” SAS . By using a user delegation SAS, we can sign the signature with the Azure Ad credentials instead of the storage account key. It is disabled storage account key access.

## Deploying the sample

### Prerequisites

- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli?view=azure-cli-latest)
- [.NET Core SDK version 8](https://dotnet.microsoft.com/download)
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
   # Set the subscription
   az account set --subscription <subscription_id>
   ```

4. Environment Setup

   We need to prepare our environment:

   ```bash
   LOCATION=eastus
   RESOURCEGROUP=rg-asyncrequestreply-${LOCATION}
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

### Try it out!

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

## :broom: Clean up resources

Most of the Azure resources deployed in the prior steps will incur ongoing charges unless removed.

   ```bash
   az group delete -n ${RESOURCEGROUP} -y
   ```

## Running locally

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

You need to add the following lines to the Bicep file to assign roles for Service Bus and Azure Storage permissions.

```bicep
// Assign Role to allow sending messages to the Service Bus
resource serviceBusSenderRoleAssignmentUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, functionApp.id, 'LocalUser', 'ServiceBusSenderRole')
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: senderServiceBusRole
    principalId: <your user object id>
    principalType: 'User'
  }
}

// Assign Role to allow receiving messages from the Service Bus
resource serviceBusReceiverRoleAssignmentUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, functionApp.id, 'LocalUser', 'ServiceBusReceiverRole')
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: receiverServiceBusRole
    principalId: <your user object id>
    principalType: 'User'
  }
}

// Assign Role to allow Read, write, and delete Azure Storage containers and blobs. 
resource dataStorageBlobDataContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, dataStorageAccount.id, 'LocalUser', 'StorageBlobDataContributorRole')
  scope: dataStorageAccount
  properties: {
    roleDefinitionId: storageBlobDataContributorRole
    principalId: <your user object id>
    principalType: 'User'
  }
}

```

## Contributions

Please see our [Contributor guide](./CONTRIBUTING.md).

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact <opencode@microsoft.com> with any additional questions or comments.

With :heart: from Azure Patterns & Practices, [Azure Architecture Center](https://azure.com/architecture).
