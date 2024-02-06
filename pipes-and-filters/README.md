# Pipes and Filters Pattern

This directory contains an example of the [Pipes and Filters cloud design pattern](https://learn.microsoft.com/azure/architecture/patterns/pipes-and-filters).

This example contains three filters that perform processing on an image. The three filters are combined into a pipe; the output of one filter is passed as the input to the next. The filters are implemented as separate Function App functions and storage queue is the pipe. While this pattern shows the Function App functions being co-located under the same Function App for simplicity, they could very well be deployed individually to support more per-filter autonomy/scale.

The sample takes a source image, resizes it (first filter), adds a watermark (second filter), then publishes it to a output destination (third filter). In this sample, the order of the first two filters could be reversed as they are not dependent on each other. The pipe is Azure Storage queues.

## Deployment guide

### Prerequisites

- A resource group in [Azure subscription](https://azure.com/free) under which you can create resources.
- [Git](https://git-scm.com/downloads)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local#install-the-azure-functions-core-tools)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)

### Steps

1. Clone this repository to your workstation and navigate to the working directory.

   ```shell
   git clone https://github.com/mspnp/cloud-design-patterns
   cd pipes-and-filters
   ```

1. Log into Azure and set subscription information.

   ```azurecli
   az login
   az account set -s <Name or ID of subscription>

   RESOURCE_GROUP_NAME=<Existing Azure resource group name>
   ```

1. Deploy the storage account.

   This storage account contains the queues that will act as the pipe in this sample. This deployment will also configure your identity to be able to create blobs and add queue messages. The Azure Function app will  under your identity, and needs these permissions.

   ```azurecli
   CURRENT_USER_OBJECT_ID=$(az ad signed-in-user show -o tsv --query id)
   STORAGE_ACCOUNT_NAME=st<UniqueName>

   # This takes about one minute
   az deployment group create -n deploy-pipe -f bicep/main.bicep -g $RESOURCE_GROUP_NAME -p storageAccountName=$STORAGE_ACCOUNT_NAME userObjectId=$CURRENT_USER_OBJECT_ID
   ```

1. Configure local Functions project to use the deployed Storage account.

   ```shell
   sed -i "s/STORAGE_ACCOUNT_NAME/${STORAGE_ACCOUNT_NAME}/g" ImageProcessingPipeline/local.settings.json
   ```

1. Start the filters.

   The filters are running as individual functions, connected by the queues (pipes).

   **Run the following in a new terminal tab.**

   ```shell
   cd ImageProcessingPipeline
   func start
   ```

   Continue once you see the output message "Host lock lease acquired by instance ID ...".  You monitor log output from this terminal tab for the following commands which 

## Try it out

Now with your pipes deployed and filters ready to execute, it's time to send up a request for an image to be processed.

1. Open a smaple image to see a "before" state.

   This repo has a sample image for you to see. Open images/clouds.png to see what the "before" image looks like. You'll notice it's larger than 600px wide and does not have a watermark on it.

   ![A picture of fluffly clouds in a blue sky](./images/clouds.png)

1. Upload a sample image to the storage account for processing through the pipes and filters.

   ```azurecli
   az storage blob upload -f images/clouds.png -c images --account-name $STORAGE_ACCOUNT_NAME --auth-mode login --overwrite true
   az storage message put --content "$(echo -n 'images/clouds.png' | base64)" -q pipe-xfty --account-name $STORAGE_ACCOUNT_NAME --auth-mode login
   ```

1. Observe the filter process.

   If you look at your function output you should see log entries similar to the following, which shows the three filters being executed in series, with the final filter writing a copy of the file to the destination.

   ```output
   [TS] Executing 'Functions.Resize' (Reason='New queue message detected on 'pipe-xfty'.', Id=3918665c-eba0-4f80-bc3b-45260dccc7fe)
   [TS] Trigger Details: MessageId: 5fd6858f-bfbc-4c58-a4ae-1ab2a0003ea2, DequeueCount: 1, InsertedOn: TS
   [TS] Processing image https://STORAGE_ACCOUNT_NAME.blob.core.windows.net/images/clouds.png for resizing.
   [TS] Image resizing done. Adding image "images/clouds.png" into the next pipe.
   [TS] Executed 'Functions.Resize' (Succeeded, Id=3918665c-eba0-4f80-bc3b-45260dccc7fe, Duration=10989ms)

   [TS] Executing 'Functions.Watermark' (Reason='New queue message detected on 'pipe-fjur'.', Id=e943a681-51ad-4a19-9003-d561c3c3ade1)
   [TS] Trigger Details: MessageId: ab496909-42c0-459c-951e-d0a3374604dc, DequeueCount: 1, InsertedOn: TS
   [TS] Processing image https://STORAGE_ACCOUNT_NAME.blob.core.windows.net/images/clouds.png for watermarking.
   [TS] Watermarking done. Adding image "images/clouds.png" into the next pipe.
   [TS] Executed 'Functions.Watermark' (Succeeded, Id=e943a681-51ad-4a19-9003-d561c3c3ade1, Duration=10790ms)

   [TS] Executing 'Functions.PublishFinal' (Reason='New queue message detected on 'pipe-yhrb'.', Id=58db3c60-3470-42e1-959b-6be63d6ef1e5)
   [TS] Trigger Details: MessageId: 2bc7191b-78d5-4ece-b52f-c98d8a193124, DequeueCount: 1, InsertedOn: TS
   [TS] Copied https://STORAGE_ACCOUNT_NAME.blob.core.windows.net/images/clouds.png into https://STORAGE_ACCOUNT_NAME.blob.core.windows.net/processed and deleted original.
   [TS] Executed 'Functions.PublishFinal' (Succeeded, Id=58db3c60-3470-42e1-959b-6be63d6ef1e4, Duration=648ms)
   ```

1. Download the processed image.

   ```azurecli
   az storage blob download -f images/clouds-processed.png -c processed -n clouds.png --account-name $STORAGE_ACCOUNT_NAME --auth-mode login
   ```

1. Open the processed image to see the "after" state.

   You'll notice it's resized to 600px wide and has a watermark on it.

## Clean up

Be sure to delete Azure resources when not using them. Execute these commands to delete resources created as part of this deployment.

```azurecli
az storage account delete -n $STORAGE_ACCOUNT_NAME -g $RESOURCE_GROUP_NAME
```

## Deploying the functions to Azure

If you wish to deploy the Azure functions to Azure instead of running locally, ensure the Azure Function has a system-managed identity and that identity is granted blob and queue data contributor rights on the storage account.
