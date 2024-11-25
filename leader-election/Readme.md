# Leader Election pattern example

This directory contains an example of the [Leader Election cloud design pattern](https://learn.microsoft.com/azure/architecture/patterns/leader-election).

This example shows how a worker process can become a leader among a group of peer instances. The leader could then perform tasks that coordinate and control the other instances; these tasks should be performed by only one worker instance.

This example contains one project showing the implementation of a distributed mutex based on a storage blob lease, and another project showing a simple example worker process, implemented as a console app, that leverages the distributed mutex to ensure only one process runs the leader-specific code.

## :rocket: Deployment guide

Install the prerequisites and follow the steps to run the example and observe the leader election behavior.

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azurite emulator for local Azure Storage development](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) or an [Azure Storage Account](https://learn.microsoft.com/azure/storage/common/storage-account-create)

#### Optional

- [Microsoft Visual Studio 2022 Community, Enterprise, or Professional](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) with [C# for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)

### Steps

1. Clone the repository

   Open a terminal, clone the repository, and navigate to the `leader-election` directory.

   ```shell
   git clone https://github.com/mspnp/cloud-design-patterns.git
   cd cloud-design-patterns
   cd leader-election
   ```

1. Build the solution

   Using the existing terminal window, build the solution.

   ```shell
   dotnet build
   ```

1. Select your storage

#### Running with Azurite storage emulator

   The included `app.config` file is set up to use a local Azure Storage emulator. Open a new terminal window, navigate to an empty working directory for the Azurite data files, and start the emulator with the command `azurite`, or `npx azurite` if you installed via `npm`.  
   Azure SDKs by DefaultAzureCredencials needs https, and azurite by default is http. Follow the instructions [here](https://learn.microsoft.com/azure/storage/common/storage-use-azurite?tabs=visual-studio%2Cblob-storage#azure-sdks).  
   In `LeaderElectionConsoleWorker/app.config` you see `https://127.0.0.1:10000/devstoreaccount1`

#### Running with Azure Storage account

   Using the Azure portal, create a new Azure Storage account, or use an existing account.

   The sample defaults to use a container named `leases`. If you are utilizing an existing storage account, be certain that this container is not already in use.

   Locate the Azure Storage account and copy the account name. Update the StorageUri field in `LeaderElectionConsoleWorker/app.config` with the value https://{0}.blob.core.windows.net, where {0} is the storage account name. Verify your local user is granted the `Storage Blob Data Contributor` role. You may need to navigate to Access Control (IAM) in the Azure portal and add the role manually.

   In Settings > Configuration, ensure that `Allow storage account key access` is disabled to enforce the use of Managed Identity.

### :checkered_flag: Try it out

1. Running the worker process

   Navigate to the `leader-election/LeaderElectionConsoleWorker` directory, and start the application.

   ```shell
   cd LeaderElectionConsoleWorker
   dotnet run
   ```

   Ideally, you should start multiple instances of the worker process to see the coordination. To do this, open additional terminal windows or tabs, and run the same command. When a worker process is the leader, you will see periodic output like:

   ```output
   [14:22:30] This process (51635) is currently the leader. Press any key to exit.
   ```

   If a worker process is not the leader, you will see:

   ```output
   [14:23:21] This process (51686) could not acquire lease. Retrying in 20 seconds. Press any key to exit.
   ```

1. Observe leader election recovery

   You can terminate the current leader and watch one of the other worker processes acquire the lease and become the new leader:

   ```output
   [14:24:21] This process (51686) could not acquire lease. Retrying in 20 seconds. Press any key to exit.
   [14:24:41] This process (51686) could not acquire lease. Retrying in 20 seconds. Press any key to exit.
   [14:25:01] This process (51686) is currently the leader. Press any key to exit.
   ```

1. Run from multiple machines

   If you are using an Azure Storage account, you can run the worker process from multiple machines. Try running it on a second machine, and then temporarily disable or unplug the network on the machine that hosts the leader process. You will see that as the lease expires, one of the still-connected worker processes will claim the lease and assume the role of leader.

1. Observe detailed tracing

   You can use the [`dotnet-trace` tool](https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-trace) to observe detailed tracing information on the worker process as it attempts to acquire or renew the lease on the storage blob.

## :broom: Clean up resources

This example does not require the creation of any resources. If you created a new storage account in step 3, you should delete that account if you are no longer using it. If you used an existing storage account, you can delete the `leases` container that was used by this example.

## Related documentation

- [Azure Blob Storage documentation](https://learn.microsoft.com/azure/storage/blobs/storage-blobs-introduction)
- [Create and manage blob leases with .NET](https://learn.microsoft.com/azure/storage/blobs/storage-blob-lease)

## Contributions

Please see our [Contributor guide](../CONTRIBUTING.md).

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact <opencode@microsoft.com> with any additional questions or comments.

With :heart: from Azure patterns & practices, [Azure Architecture Center](https://azure.com/architecture).
