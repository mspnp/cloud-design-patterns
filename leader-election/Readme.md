# Leader Election Pattern

This document describes the Leader Election Pattern example from the guide [Cloud Design Patterns](http://aka.ms/Cloud-Design-Patterns).

## System Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azurite emulator for local Azure Storage development](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) or an [Azure Storage Account](https://learn.microsoft.com/azure/storage/common/storage-account-create)

Optional:
- [Microsoft Visual Studio 2022 Community, Enterprise, or Professional](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) with [C# for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)

## Before you start

Ensure that you have installed the software prerequisites.

## About the Example

This example shows how a worker process can become a leader among a group of peer instances. The leader can then perform tasks that coordinate and control the other instances; these tasks should be performed by only one instance of the worker role. The leader is elected by acquiring a blob lease.

## Running the Example

You can run this example anywhere you can run a .NET 8.0 console application.

### Clone the repository

Open a terminal, clone the repository, and navigate to the `leader-election` directory.

```shell
git clone https://github.com/mspnp/cloud-design-patterns.git
cd cloud-design-patterns
cd leader-election
```

### Running with Azurite storage emulator

The default `app.config` file is set up to use a local Azure Storage emulator. Open a new terminal window, navigate to an empty working directory for the Azurite data files, and start the emulator with the command `azurite`, or `npx azurite` if you installed via `npm`.

### Running with Azure Storage account

Using the Azure portal, create a new Azure Storage account, or us an existing account.

The sample defaults to use a container named `leases`. If you are utilizing an existing storage account, be certain that this container is not already in use.

Find the connection string in your Storage Account's `Access keys` section in the portal. Copy the Connection string value from either key1 or key2 and replace the `StorageConnectionString` value field in `app.config` with the copied value.

### Running the worker process

Navigate to the `leader-election/LeaderElectionConsoleApp` directory, and start the application

```shell
cd LeaderElectionConsoleWorker
dotnet run
```

You can start multiple instances of the worker process by opening additional terminal windows or tabs, and running the same command. When a worker process is the leader, you will see periodic output like:

```output
[14:22:30] This process (51635) is currently the leader. Press any key to exit.
```

If a worker process is not the leader, you will see:

```output
[14:23:21] This process (51686) could not acquire lease. Retrying in 20 seconds. Press any key to exit.
```

You can terminate the current leader, and watch the other processes to see one of them acquire the lease and become the new leader:

```output
[14:24:21] This process (51686) could not acquire lease. Retrying in 20 seconds. Press any key to exit.
[14:24:41] This process (51686) could not acquire lease. Retrying in 20 seconds. Press any key to exit.
[14:25:01] This process (51686) is currently the leader. Press any key to exit.
```