targetScope = 'resourceGroup'

@minLength(5)
@description('Location of the resources. Defaults to resource group location.')
param location string = resourceGroup().location

@minLength(36)
@description('The guid of the principal running the valet key generation code. In Azure this would be replaced with the managed identity of the Azure Function, when running locally it will be your user.')
param principalId string

@minLength(3)
@maxLength(5)
@description('The globally unique prefix naming resources.')
param namePrefix string

/*** EXISTING RESOURCES ***/

@description('Built-in Azure RBAC role that is applied to a Storage account to grant "Storage Blob Data Contributor" privileges.')
resource storageBlobDataContributorRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
  scope: subscription()
}

@description('Built-in Azure RBAC role that is applied to an Event Hub  to grant "Azure Event Hubs Data Owner" privileges.')
resource eventHubDataOwnwerRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: 'f526a384-b230-433a-b45c-95f59c4a2dec'
  scope: subscription()
}

/*** NEW RESOURCES ***/

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' = {
  name: 'la-${namePrefix}'
  location: location
  properties: any({
    retentionInDays: 30
    features: {
      searchVersion: 1
    }
    sku: {
      name: 'PerGB2018'
    }
  })
}

@description('The Azure Storage account which will be where authorized clients upload large blobs to. The Azure Function will hand out scoped, time-limited SaS tokens for this blobs in this account.')
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'st${namePrefix}cc'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowCrossTenantReplication: false
    allowSharedKeyAccess: false //Only access by Managed Identity allowed
    isLocalUserEnabled: false
    isHnsEnabled: false
    isNfsV3Enabled: false
    isSftpEnabled: false
    largeFileSharesState: 'Disabled'
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled' // Enabled to test the sample code from a local machine
    supportsHttpsTrafficOnly: true
    defaultToOAuthAuthentication: true
    allowedCopyScope: 'PrivateLink'
    networkAcls: {
      defaultAction: 'Allow' // For this sample, public Internet access is expected
      bypass: 'None'
      virtualNetworkRules: []
      ipRules: []
    }
  }

  resource blobContainers 'blobServices' = {
    name: 'default'

    @description('The blob container to serve as the large payloads data store.')
    resource payloadsContainer 'containers' = {
      name: 'payloads'
    }
  }
}

@description('Set permissions to give the user principal access to Storage Blob from the sample applications')
resource blobContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount::blobContainers::payloadsContainer.id, storageBlobDataContributorRole.id, principalId)
  scope: storageAccount::blobContainers::payloadsContainer
  properties: {
    principalId: principalId
    roleDefinitionId: storageBlobDataContributorRole.id
    principalType: 'User' // 'ServicePrincipal' if this was a managed identity
    description: 'Allows this Microsoft Entra principal to access blobs in this storage blob container.'
  }
}

@description('The Azure Event Grid system topic to use with the sample apps. This topic will be used to forward BlobCreated events to the Azure Event Hub.')
resource eventGridStorageBlobTopic 'Microsoft.EventGrid/systemTopics@2023-12-15-preview' = {
  name: '${storageAccount.name}${guid(namePrefix, 'storage')}'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    source: storageAccount.id
    topicType: 'microsoft.storage.storageaccounts'
  }
}

@description('The Azure Storage account to use together with Event Hubs. Used to support the functionality of the EventProcessor class')
resource eventHubStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'st${namePrefix}ehub'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowCrossTenantReplication: false
    allowSharedKeyAccess: false  //Only access by Managed Identity allowed
    isLocalUserEnabled: false
    isHnsEnabled: false
    isNfsV3Enabled: false
    isSftpEnabled: false
    largeFileSharesState: 'Disabled'
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled' // Enabled to test the sample code from a local machine
    supportsHttpsTrafficOnly: true
    defaultToOAuthAuthentication: true
    allowedCopyScope: 'PrivateLink'
    networkAcls: {
      defaultAction: 'Allow' // For this sample, public Internet access is expected
      bypass: 'None'
      virtualNetworkRules: []
      ipRules: []
    }
  }

  resource blobContainers 'blobServices' = {
    name: 'default'

    @description('The blob container to use with the EventProcessor class of the Event Hubs SDK.')
    resource eventProcessorContainer 'containers' = {
      name: 'eventprocessor'
    }
  }
}

@description('Set permissions to give the user principal access to Storage Blob from the sample applications')
resource eventProcessorBlobContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(
    eventHubStorageAccount::blobContainers::eventProcessorContainer.id,
    storageBlobDataContributorRole.id,
    principalId
  )
  scope: eventHubStorageAccount::blobContainers::eventProcessorContainer
  properties: {
    principalId: principalId
    roleDefinitionId: storageBlobDataContributorRole.id
    principalType: 'User' // 'ServicePrincipal' if this was a managed identity
    description: 'Allows this Microsoft Entra principal to access blobs in this storage blob container.'
  }
}

@description('The Azure Event Hubs namespace to use with the sample apps.')
resource eventHubNamespace 'Microsoft.EventHub/namespaces@2023-01-01-preview' = {
  name: 'evhns-${namePrefix}'
  location: location
  sku: {
    name: 'Standard'
    capacity: 2
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
    privateEndpointConnections: []
    zoneRedundant: false
    isAutoInflateEnabled: false
    maximumThroughputUnits: 0
  }

  @description('The Event Hubs to send/receive claim-check venet messages.')
  resource claimCheckEventHub 'eventhubs' = {
    name: 'evh-claimcheck'
    properties: {
      messageRetentionInDays: 1
      partitionCount: 2
    }
  }
}

@description('Diagnostic settings for the Event Hub namespace.')
resource eventHubDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'evhns-diagnostics'
  scope: eventHubNamespace
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

@description('Set permissions to give the Event Grid System Managed identity access to Event Hub')
resource gridEventHubDataOwnwerRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(eventHubNamespace.id, eventHubDataOwnwerRole.id, eventGridStorageBlobTopic.id)
  scope: eventHubNamespace
  properties: {
    principalId: eventGridStorageBlobTopic.identity.principalId
    roleDefinitionId: eventHubDataOwnwerRole.id
    principalType: 'ServicePrincipal'
    description: 'Allows this Microsoft Entra principal to access Event Hub data.'
  }
}

@description('Set permissions to give the user principal access to  Event Hub')
resource userEventHubDataOwnwerRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(eventHubNamespace.id, eventHubDataOwnwerRole.id, principalId)
  scope: eventHubNamespace
  properties: {
    principalId: principalId
    roleDefinitionId: eventHubDataOwnwerRole.id
    principalType: 'User'
    description: 'Allows this Microsoft Entra principal to access Event Hub data.'
  }
}

@description('Event Grid subscription to forward BlobCreated events to our Azure Hub.')
resource eventGridBlobCreatedEventHubSubscription 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2023-12-15-preview' = {
  parent: eventGridStorageBlobTopic
  name: 'eventhub'
  properties: {
    deliveryWithResourceIdentity: {
      identity: {
        type: 'SystemAssigned'
      }
      destination: {
        properties: {
          resourceId: eventHubNamespace::claimCheckEventHub.id
        }
        endpointType: 'EventHub'
      }
    }
    filter: {
      includedEventTypes: [
        'Microsoft.Storage.BlobCreated'
      ]
    }
    eventDeliverySchema: 'EventGridSchema'
  }
  dependsOn:[
    gridEventHubDataOwnwerRoleAssignment
  ]
}
