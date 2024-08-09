param location string = resourceGroup().location

var appStorageAccountName = toLower('stoapp${uniqueString(subscription().subscriptionId, resourceGroup().id)}')
var dataStorageAccountName = toLower('stodata${uniqueString(subscription().subscriptionId, resourceGroup().id)}')
var hostingPlanName = 'app-reqrep'
var functionAppName = 'fapp-reqrep-${uniqueString(subscription().subscriptionId, resourceGroup().id)}'
var serviceBusNamespaceName = toLower('sb-reqrep-${uniqueString(subscription().subscriptionId, resourceGroup().id)}')

var senderServiceBusRole = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
) // Azure Service Bus Data Sender
var receiverServiceBusRole = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
) // Azure Service Bus Data Receiver

var storageBlobDataContributorRole = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
) // Azure Storage Blob Data Contributor

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: serviceBusNamespaceName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {}
}

resource serviceBusNamespace_outqueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'outqueue'
  properties: {
    defaultMessageTimeToLive: 'P14D'
  }
}

resource appStorageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: appStorageAccountName
  sku: {
    name: 'Standard_RAGRS'
  }
  kind: 'StorageV2'
  location: location
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

resource dataStorageAccount 'Microsoft.Storage/storageAccounts@2019-04-01' = {
  name: dataStorageAccountName
  sku: {
    name: 'Standard_RAGRS'
  }
  kind: 'StorageV2'
  location: location
  properties: {
    allowSharedKeyAccess: false
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

resource dataStorageAccountNameContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-09-01' = {
  name: '${dataStorageAccountName}/default/data'
  dependsOn: [
    dataStorageAccount
  ]
}

resource hostingPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
    size: 'Y1'
  }
  properties: {}
}

resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    enabled: true
    serverFarmId: hostingPlan.id
    httpsOnly: true
    redundancyMode: 'None'
    publicNetworkAccess: 'Enabled'
    keyVaultReferenceIdentity: 'SystemAssigned'
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      numberOfWorkers: 1
      alwaysOn: false
      http20Enabled: false
      functionAppScaleLimit: 200
      minimumElasticInstanceCount: 0
      use32BitWorkerProcess: false
      appSettings: [
        {
          name: 'AzureWebJobsDashboard'
          value: 'DefaultEndpointsProtocol=https;AccountName=${appStorageAccount.name};AccountKey=${listKeys(appStorageAccount.id, '2015-05-01-preview').key1}'
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${appStorageAccount.name};AccountKey=${listKeys(appStorageAccount.id, '2015-05-01-preview').key1}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${appStorageAccount.name};AccountKey=${listKeys(appStorageAccount.id, '2015-05-01-preview').key1}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionAppName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED'
          value: '1'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'ServiceBusConnection__fullyQualifiedNamespace'
          value: '${serviceBusNamespace.name}.servicebus.windows.net'
        }
        {
          name: 'DataStorage__blobServiceUri '
          value: 'https://${dataStorageAccount.name}.blob.${environment().suffixes.storage}'
        }
      ]
    }
  }
}

// Assign Role to allow sending messages to the Service Bus
resource serviceBusSenderRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, functionApp.id, 'ServiceBusSenderRole')
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: senderServiceBusRole
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Assign Role to allow receiving messages from the Service Bus
resource serviceBusReceiverRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, functionApp.id, 'ServiceBusReceiverRole')
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: receiverServiceBusRole
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}


// Assign Role to allow Read, write, and delete Azure Storage containers and blobs. 
resource dataStorageBlobDataContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, dataStorageAccount.id, 'StorageBlobDataContributorRole')
  scope: dataStorageAccount
  properties: {
    roleDefinitionId: storageBlobDataContributorRole
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output functionAppName string = functionAppName
