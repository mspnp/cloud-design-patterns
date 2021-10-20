# Asynchronous Request-Reply pattern

Decouple backend processing from a frontend host, where backend processing needs to be asynchronous, but the frontend still needs a clear response.

For more information about this pattern, see [Asynchronous Request-Reply pattern](https://docs.microsoft.com/azure/architecture/patterns/async-request-reply) on the Azure Architecture Center.

![Data flow of the async request-reply pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/_images/async-request-fn.png)

1. AsyncProcessingWorkAcceptor: Http trigger Azure function which enqueue the message.
1. AsyncProcessingBackgroundWorker: Service Buss trigger Azure function which take the event from the queue and create the file on Azure Blob Storage
1. AsyncOperationStatusChecker: Http trigger Azure function which show you the status. If it was created, you will see the file.

## Deploying the sample

### Prerequisites

- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli?view=azure-cli-latest)
- [.NET Core SDK version 3.1](https://microsoft.com/net)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local#v2)

### Deploy the Azure resources

1. Clone or download this repo.

   ```bash
   git clone https://github.com/mspnp/cloud-design-patterns.git
   cd cloud-design-patterns
   ```

2. Navigate to the `async-request-reply` folder.

   ```bash
   cd async-request-reply
   ```

3. Login and select subcription

   ```bash
   az login
   //Select the Azure Subscription you want to use for the tests:
   az account list --output table
   az account set --subscription "<YOUR SUBSCRIPTION NAME>"
   ```

4. Create a resource group.

   ```bash
   export RESOURCE_GROUP=<resource-group-name>
   export APP_NAME=<app-name> # 2-6 characters
   export LOCATION=<azure-region>

   az group create --name $RESOURCE_GROUP --location $LOCATION
   ```

5. Deploy the template.

   ```bash
   az deployment group create \
   --resource-group $RESOURCE_GROUP \
   --template-file deploy.json \
   --parameters appName=$APP_NAME
   ```

6. Wait for the deployment to complete.

### Deploy the Functions app

1. Navigate to the Azure Function source folder.

   ```bash
   export FUNC_APP_NAME=$APP_NAME-fn

   cd src/asyncpattern/asyncpattern
   ```

2. Deploy the app.

   ```bash
   func azure functionapp publish $FUNC_APP_NAME
   ```

### Send a request

```bash
curl -X POST "https://$FUNC_APP_NAME.azurewebsites.net/api/asyncprocessingworkacceptor" --header 'Content-Type: application/json' --header 'Accept: application/json' -k -i -d '{
   "id": "1234",
   "customername": "Contoso"
 }'
```

_Note:_ Pay attention to the response, there is the link to check the status. It looks like ` http://$FUNC_APP_NAME.azurewebsites.net/api/RequestStatus/{Guid}`

### Check the application

- In a browser execute the link presented as output from the previous step. The expected result is a json with the data, which comes from blob storage with a SAS token (The [Valet key Pattern](https://docs.microsoft.com/azure/architecture/patterns/valet-key) is used ).
- Check the creation of the file. It is done by the AsyncProcessingBackgroundWorker Azure Function. The function is trigger with a Service Bus event. It is possible to go to the AppInsight and go to Logs.  
  You can run the following query (the record could take some time to arrive to AppInsight):

```
traces
| where message contains "AsyncProcessingBackgroundWorker"
| order by timestamp desc
```

Look for something like `AsyncProcessingBackgroundWorker: Created resource: data/{Guid}.blobdata`

- Check the file generated on the Storage Account

### Delete resources

```dotnetcli
  az group delete -n $RESOURCE_GROUP
```

> Note. The app uses the WEBSITE_HOSTNAME environment variable. This environment variable is set automatically by the Azure App Service runtime environment. For more information, see [Azure runtime environment](https.://github.com/projectkudu/kudu/wiki/Azure-runtime-environment)
