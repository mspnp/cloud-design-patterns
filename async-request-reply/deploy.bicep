param location string = resourceGroup().location

var appStorageAccountName = toLower('stoapp${uniqueString(subscription().subscriptionId, resourceGroup().id)}')
var dataStorageAccountName = toLower('stodata${uniqueString(subscription().subscriptionId, resourceGroup().id)}')
var hostingPlanName = 'app-reqrep'
var functionAppName = 'fapp-reqrep-${uniqueString(subscription().subscriptionId, resourceGroup().id)}'
var serviceBusNamespaceName = toLower('sb-reqrep-${uniqueString(subscription().subscriptionId, resourceGroup().id)}')
var deploymentStorageContainerName = 'app-package-${uniqueString(subscription().subscriptionId, resourceGroup().id)}'
var appInsigthName = 'appinsigth-${uniqueString(subscription().subscriptionId, resourceGroup().id)}'
var logAnalyticsName = 'loganalytics-${uniqueString(subscription().subscriptionId, resourceGroup().id)}'
//var vnetName = 'asycReplyVnet'

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

var storageBlobDataOwnerRole = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
) //Storage Blob Data Owner role

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

resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${serviceBusNamespace.name}-diagnostic'
  scope: serviceBusNamespace
  properties: {
    logs: [
      {
        category: 'OperationalLogs'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
    workspaceId: logAnalytics.id
  }
}

resource appStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: appStorageAccountName
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  location: location
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    networkAcls: {
      bypass: 'AzureServices'
      virtualNetworkRules: []
      ipRules: []
      defaultAction: 'Allow'
    }
  }

  resource blobServices 'blobServices' = {
    name: 'default'
    properties: {
      deleteRetentionPolicy: {}
    }
    resource container 'containers' = {
      name: deploymentStorageContainerName
      properties: {
        publicAccess: 'None'
      }
    }
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

resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: hostingPlanName
  location: location
  kind: 'functionapp'
  sku: {
    tier: 'FlexConsumption'
    name: 'FC1'
  }
  properties: {
    reserved: true
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' = {
  name: logAnalyticsName
  location: location
  properties: {
    retentionInDays: 30
    features: {
      searchVersion: 1
    }
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsigthName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
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
    //virtualNetworkSubnetId: functiondsbnt.id // Adding VNet Integration
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      numberOfWorkers: 1
      alwaysOn: false
      http20Enabled: false
      minimumElasticInstanceCount: 0
      use32BitWorkerProcess: false
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'AzureWebJobsStorage__accountName'
          value: appStorageAccount.name
        }
        {
          name: 'ServiceBusConnection__fullyQualifiedNamespace'
          value: '${serviceBusNamespace.name}.servicebus.windows.net'
        }
        {
          name: 'DataStorage__blobServiceUri'
          value: 'https://${dataStorageAccount.name}.blob.${environment().suffixes.storage}'
        }
      ]
    }
    clientCertEnabled: true
    clientCertMode: 'OptionalInteractiveUser'
    clientCertExclusionPaths: '/public'
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${appStorageAccount.properties.primaryEndpoints.blob}${deploymentStorageContainerName}'
          authentication: {
            type: 'SystemAssignedIdentity'
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: 100
        instanceMemoryMB: 2048
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '8.0'
      }
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

// Allow access from function app to storage account using a managed identity
resource storageRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(resourceGroup().id, appStorageAccount.id, 'StorageBlobDataOwnerRole')
  scope: appStorageAccount
  properties: {
    roleDefinitionId: storageBlobDataOwnerRole
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output functionAppName string = functionAppName
