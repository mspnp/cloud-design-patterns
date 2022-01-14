# Valet Key Pattern

This document describes the Valet Key Pattern example from the guide [Cloud Design Patterns](http://aka.ms/Cloud-Design-Patterns).

## System Requirements

* Microsoft .NET Framework 5
* Microsoft Visual Studio 2019 or Later
* Windows Azure SDK for .NET release 2021

## Before you start

Ensure that you have installed all of the software prerequisites.
If you don't have one, provision an Azure Storage account from the Azure Portal.
Once you storage account is created, add a container to it, name it "valetkeysample".

## About the Example
 
This example shows how a client application can obtain a shared access signature with the necessary permissions to write directly to blob storage. For simplicity, this sample focuses on the mechanism to obtain and consume a valet key and does not show how to implement authentication or secure communications.

## Running the Example

You can run this example locally in the Visual Studio Windows Azure emulator. You can also run this example by deploying it to a Windows Azure App Service.

If you want to run this example locally

* Start Visual Studio, set the ValetKet.Web project as startup
* Edit the appsettings.json and provide values for these settings:

	1 - StorageConnectionString
	2 - StorageKey

	You should be able to get these values from the Azure Portal; go to the storage account, and from the
	left menu, in the Security + networking section, select the option "Access Keys".
	In the "Access Keys" dialog, click on "show keys"; you will be able to see and copy the key and the connection string.

* Hit F5 to run the Web Api
* Start a new instance of the ValetKey.Client project to upload the blob. Right-click the project, select Debug, and click Start new instance.

* If you want to run the example on Windows Azure, provision a Windows Azure App Service and deploy the application to it from Visual Studio.
* Right click on the ValetKey.Web project, select publish.
* Select Azure as target and Azure App Service as specific target.
* Select an app service instance or create a new one.
* Skip the Api Management step.
* Once the publish profile is created, in the Hosting section click on the "..." button in the upper right corner.
* Select "Manage Azure App Service Settings"
* Add these three settings:

	1 - ContainerName (set the remote value to 'valetkeysample')
	2 - StorageConnectionString
	3 - StorageKey

	You should be able to get thes Azure Storage connection string and key values from the Azure Portal; go to the storage account, and from the
	left menu, in the Security + networking section, select the option "Access Keys".
	In the "Access Keys" dialog, click on "show keys"; you will be able to see and copy the key and the connection string.

* Run the App Service instance and note the base URL of the web api shown in the browser address bar.
* Open the file appsettings.json from the ValetKey.Client project and change the setting for ServiceEndpointUrl to   [your-URL]**/api/sas/**
	* By default this is set to **http://localhost:10194/api/sas** which set up to run locally.

* You can verify that the blob has been sucessfully uploaded by using the Azure Portal.
* Go to your Storage Account, from left menu select "container"
* Click on the container named "valetkeysample"
* You should be able to see the list of uploaded blobs.
