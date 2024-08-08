# Asynchronous Request-Reply pattern

Decouple backend processing from a frontend host, where backend processing needs to be asynchronous, but the frontend still needs a clear response.

For more information about this pattern, see [Asynchronous Request-Reply pattern](https://learn.microsoft.com/azure/architecture/patterns/async-request-reply) on the Azure Architecture Center.

![Data flow of the async request-reply pattern](https://learn.microsoft.com/azure/architecture/patterns/_images/async-request-fn.png)

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
    "ServiceBusConnectionAppSetting": "<yourdata>",
    "StorageConnectionAppSetting": "<yourData>"
  }
}
```
