# Asynchronous Request-Reply pattern

Decouple backend processing from a frontend host, where backend processing needs to be asynchronous, but the frontend still needs a clear response.

For more information about this pattern, see [Asynchronous Request-Reply pattern](https://docs.microsoft.com/azure/architecture/patterns/async-request-reply) on the Azure Architecture Center.

![Data flow of the async request-reply pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/_images/async-request-fn.png)

## Deploying the sample

### Prerequisites

- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli?view=azure-cli-latest)
- [.NET Core SDK version 6](https://dotnet.microsoft.com/en-us/download)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local#v4)

### Deploy the Azure resources

1. Clone or download this repo.

2. Navigate to the `async-request-reply` folder.

   ```bash
   cd async-request-reply
   ```

3. Create a resource group.

   ```bash
   az group create --name rg-asyncrequestreply --location eastus
   ```

4. Deploy the template.

   ```bash
   az deployment group create -g rg-asyncrequestreply -f deploy.bicep
   ```

5. Wait for the deployment to complete.

### Deploy the Functions app

1. Navigate to the `async-request-reply/src` folder.

   ```bash
   cd src
   ```

2. Deploy the app.

   ```bash
   FUNC_APP_NAME=$(az deployment group show -g rg-asyncrequestreply -n deploy --query properties.outputs.functionAppName.value -o tsv)

   func azure functionapp publish $FUNC_APP_NAME --dotnet
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

   Open a browser and execute the location url, a file with the data you have send would be received.

   > **Note** the app uses the WEBSITE_HOSTNAME environment variable. This environment variable is set automatically by the Azure App Service runtime environment. For more information, see [Azure runtime environment](https.://github.com/projectkudu/kudu/wiki/Azure-runtime-environment)

### Clean up

1. Delete all the Azure resources for this Cloud Async Pattern

   ```bash
   az group delete -n rg-asyncrequestreply -y
   ```

### Running localy

You could open the solution with Visual Studio, then you need to create on the root `local.settings.json`

   ```json
    {
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "ServiceBusConnectionAppSetting": "<yourdata>",
        "StorageConnectionAppSetting": "<yourData>"
    }
    }
   ```