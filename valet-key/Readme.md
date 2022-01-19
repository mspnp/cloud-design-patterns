# Valet Key Pattern

This document describes the Valet Key Pattern example from the guide [Cloud Design Patterns](https://docs.microsoft.com/azure/architecture/patterns/valet-key).

This example shows how a client application can obtain a shared access signature with the necessary permissions to write directly to blob storage. For simplicity, this sample focuses on the mechanism to obtain and consume a valet key and does not show how to implement authentication or secure communications.

## System Requirements

* Microsoft .NET Framework 5
* Microsoft Visual Studio 2019 or newer

## Provision a Azure Storage account

Provision an Azure Storage account from the Azure Portal. Once your storage account is created, add a container to it, name it **valetkeysample**.

## Running the example

You can either run this example locally or deploy it to Azure.

### To run locally

1. Start Visual Studio.
1. Set the _ValetKey.Web_ project as startup project.
1. Edit the appsettings.json and replace the placeholder "\<StorageAccountName>" with the name of your storage account.
1. Start a new instance of the ValetKey.Web.
1. Once the Web API is running start a new instance of the ValetKey.Client project.

### To run on Azure

1. Right click on the _ValetKey.Web_ project, select Publish.
1. Select Azure as target and Azure App Service as specific target.
1. Select an App service instance or create a new one.
1. Skip the API Management step.
1. Once the publish profile is created, in the Hosting section click on the "..." button in the upper right corner.
1. Select "Manage Azure App Service Settings".
1. Add this application setting and set its remote value:

   `BlobEndpoint`: `https://<Your Storage Account Name>.blob.core.windows.net`

1. Publish the _ValetKey.Web_ project and note the URL.
1. Open the file appsettings.json from the _ValetKey.Client_ project and change the setting `ServiceEndpointUrl` to `https://<Your Published API URL>/api/sas`.
1. Once the Web API is running start a new instance of the ValetKey.Client project.

### Validate blob has been uploaded

1. Go to your Storage Account, from left menu select "container"
1. Click on the container named "valetkeysample"
1. You should be able to see the list of uploaded blobs.
