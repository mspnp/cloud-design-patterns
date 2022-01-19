# Valet Key Pattern

This document describes the Valet Key Pattern example from the guide [Cloud Design Patterns](http://aka.ms/Cloud-Design-Patterns).

## System Requirements

* Microsoft .NET Framework 5
* Microsoft Visual Studio 2019 or Later
* Azure SDK for .NET release 2021

## Before you start

Ensure that you have installed all of the software prerequisites.
If you don't have one, provision an Azure Storage account from the Azure Portal.
Once you storage account is created, add a container to it, name it "valetkeysample".

## About the Example
 
This example shows how a client application can obtain a shared access signature with the necessary permissions to write directly to blob storage. For simplicity, this sample focuses on the mechanism to obtain and consume a valet key and does not show how to implement authentication or secure communications.

## Running the Example

You can either run this example locally or deploy it to Azure.

If you want to run this example locally, follow these steps:

1 - Start Visual Studio
2 - Set the ValetKet.Web project as startup
3 - Edit the appsettings.json and replace the placeholder "<StorageccountName>" with the name of your sorage account; in the settings "ContainerEndpoint" and "BlobEndpoint".
4 - Start a new instance of the Web API project, ValetKey.Web.
5 - Once the Web API is running start a new instance of the ValetKey.Client project, the client.

If you want to run the example on Azure, follow these steps:

1  - provision an Azure App Service and deploy the application to it from Visual Studio.
2  - Right click on the ValetKey.Web project, select publish.
3  - Select Azure as target and Azure App Service as specific target.
4  - Select an app service instance or create a new one.
5  - Skip the Api Management step.
6  - Once the publish profile is created, in the Hosting section click on the "..." button in the upper right corner.
7  - Select "Manage Azure App Service Settings"
8  - Add this application setting and set its remote value:

	BlobEndpoint

		This is the Blob endpoint URL; replace the placeholder with your storage account:

		https://<Your Storage Account Name>.blob.core.windows.net

9  - Run the App Service instance and note the base URL of the web api shown in the browser address bar.
10 - Open the file appsettings.json from the ValetKey.Client project and change the setting for ServiceEndpointUrl to   [your-URL]**/api/sas/**
	* By default this is set to **http://localhost:10194/api/sas** which set up to run locally.

You can verify that the blob has been successfully uploaded by using the Azure portal or Azure Storage Explorer:

1 - Go to your Storage Account, from left menu select "container"
2 - Click on the container named "valetkeysample"
3 - You should be able to see the list of uploaded blobs.
