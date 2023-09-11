# Valet Key Pattern

This sample implements the [Valet Key pattern](https://learn.microsoft.com/azure/architecture/patterns/valet-key) from Azure Architecture Center's [Cloud Design Patterns](https://learn.microsoft.com/azure/architecture/patterns/) catalog.

This example shows how a client application can obtain a shared access signature with the necessary permissions to write directly to blob storage. For simplicity, this sample focuses on the mechanism to obtain and consume a valet key and does not show how to implement authentication or secure communications.

## Prerequisite

- Microsoft .NET 7
- This repository cloned to your workstation

## :rocket: Steps

### Prepare an Azure Storage account

1. [Create an Azure Storage account](https://learn.microsoft.com/azure/storage/common/storage-account-create) for this sample, using the following configuration.

    - Standard (general purpose v2)
    - LRS (cost savings for sample)
    - Require secure transfer
    - Enable storage account key access
    - Hot tier
    - Public access from all networks (or at least needs to be network-accessible from the workstation that you are running the sample from.)

1. Add a _private_ container to the storage account named **valetkeysample**.

1. Grant yourself **Storage Blob Data Owner** on the storage account.

### Launch key-granting web application

1. Update the **ValetKey.Web/appsettings.json** file.

   Replace the `<StorageAccountName>` placeholder with the name of your storage account.

1. Configure authentication.

   This sample uses `DefaultAzureCredential` for the web application to authenticate to the Azure storage account as your identity with **Storage Blob Data Owner**. This must be [configured in your enviornment](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/?tabs=command-line#exploring-the-sequence-of-defaultazurecredential-authentication-methods). (Visual Studio, Visual Studio Code, Azure CLI, etc.)

1. Start the web api from a terminal instance.

   ```bash
   dotnet run --project ValetKey.Web
   ```

### Launch client

1. Run the client from another terminal instance.

   ```bash
   dotnet run --project ValetKey.Client
   ```

   This will use the "User delegation key"-generated SaS token provided by the web application to upload a single file per execution.

### Validate the blob has been uploaded

1. Open the the **Storage brower** on your Storage Account.
1. Select **Blob containers**.
1. Click on the container named "valetkeysample."
1. You should be able to see the list of uploaded blobs.

### :broom: Clean up

Remove the storage account when you are done with this sample.