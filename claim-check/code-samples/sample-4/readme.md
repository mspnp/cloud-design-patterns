# Sample 4: Manual Tagging

### Technologies used: Azure Blob Storage, Azure Event Hubs with Kafka, .NET Core 2.1

The reason this example uses Event Hubs with Kafka is to demonstrate the ease of using other Azure services like Azure Blob Storage, Azure functions etc. with a different messaging protocol like Kafka from your existing Kafka clients to implement the claim check messaging pattern.
This sample consists of a Kafka client which drops the payload in the designated Azure Blob Storage and creates a notification message with location details to be sent to the consumer. The notification message is sent using [Event Hubs with Kafka enabled](https://docs.microsoft.com/en-us/azure/event-hubs/event-hubs-create-kafka-enabled).  
The consumer is notified each time these is a message in the Event Hub and can access the payload using the location information in the message received.

![Imported Sript](images/Sample-4-diagram.jpg)

## Prerequisites

If you don't have an Azure subscription, create a [free account](https://azure.microsoft.com/free/?ref=microsoft.com&utm_source=microsoft.com&utm_medium=docs&utm_campaign=visualstudio) before you begin.

In addition:

* [Visual Studio 2017](https://visualstudio.microsoft.com/downloads/) or  [Visual Studio Code](https://code.visualstudio.com/)
* [.NET Core SDK](https://dotnet.microsoft.com/download)
* [Git](https://www.git-scm.com/downloads)
* [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
* [Azure Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/)

## Getting Started
Make sure you have WSL (Windows System For Linux) installed and have AZ CLI version > 2.0.50
Before running any script make sure you are authenticated on AZ CLI using

```bash
az login
```

and have selected the Azure Subscription you want to use for the tests:

```bash
az account list --output table
az account set --subscription "<YOUR SUBSCRIPTION NAME>"
```

## Clone the sample project

Clone the repository and open the code-samples directory from your command line tool.

```bash
git clone https://github.com/mspnp/cloud-design-patterns.git claimncheck
cd claimncheck/code-samples
```

## Run Azure Setup Script

Run the azure setup script to get the resources deployed and everything set up

```bash
./sample-4-azure-setup.sh
```

This script will create

* a resource group
* a V2 storage account
* a storage account container
* an event hub namespace with Kafka enabled, event hub
* a function app in an app service plan
* an application insights service

Copy the Connection string values displayed at the end of this script on execution. These will be used later.

## Running the sample

Access the console application by opening the below solution in VS2017:

```bash
cloud-design-patterns/claimncheck/code-samples/sample-4/client-producer/client-producer.sln
```

Open the App.config file in the solution and update the connection strings and event hub information obtained earlier on running the ```sample-4-azure-setup.sh``` script in this file.

```xml
<appSettings>
    <add key="STORAGE_CONNECTION_STRING" value=""/>
    <add key="EH_FQDN" value="EH_FQDN:9093"/>
    <add key="EH_CONNECTION_STRING" value=""/>
    <add key="EH_NAME" value=""/>
    <add key="CA_CERT_LOCATION" value=".\cacert.pem"/>
  </appSettings>
```

After making above update, run the console application locally.

This console application will create a test file on your local machine which acts as the large payload and uploads it to blob storage. The console application is a Kafka client producer which then sends a Kafka message with the blob details as a notification to your Event Hub.

The Kafka message will contain something like this:

```json
        {
          "ContainerName":"heavypayload0fc21425-5df3-4c0d-930c-9261ee83ad53",
          "BlobName":"HeavyPayload_82db7213-ca94-4aa0-9e65-fb668e51ccc9.txt"
        }
```

The Azure Function is used to demonstrate a client application that acts as the consumer  for the large payload. We have used the Azure Function Event Hub binding here to be notified of an incoming message in the Event Hub.

You can use the Azure portal to see the output of the Azure Function.

## Cleanup

To complete cleanup of your solution, since this will create a dedicated resource group for the sample, you can just delete the entire resource group:

```bash
az group delete -n pnp4
```