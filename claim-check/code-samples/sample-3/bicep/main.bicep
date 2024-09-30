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

@description('Built-in Azure RBAC role that is applied to a Storage account to grant "Storage Blob Data Contributor" privileges.')
resource storageBlobDataContributorRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
  scope: subscription()
}

@description('Built-in Azure RBAC role that is applied to a Service Bus to grant "Service Bus Data Owner" privileges.')
resource serviceBusDataOwnwerRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: '090c5cfd-751d-490a-894a-3ce6f1109419'
  scope: subscription()
}

@description('Allows for receive access to Azure Service Bus resources.')
resource serviceBusDataReceiverRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
  scope: subscription()
}

/*** NEW RESOURCES ***/

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
    allowSharedKeyAccess: false //Only Managed Identity allowed
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

@description('The Azure Event Grid system topic to use with the sample apps. This will be used to forward BlobCreated events to the Service Bus Queue.')
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

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' = {
  name: 'la-${namePrefix}'
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

@description('The Azure Service Bus namespace to use with the sample apps.')
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: 'sbns-${namePrefix}'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
  }

  @description('The Service Bus queue to receive claim-check messages.')
  resource claimCheckBusQueue 'queues' = {
    name: 'esbq-claimcheck'
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

@description('Set permissions to give the user principal receive data from Service Bus.')
resource userServiceBusDataReceiverRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespace.id, serviceBusDataReceiverRole.id, principalId)
  scope: serviceBusNamespace
  properties: {
    principalId: principalId
    roleDefinitionId: serviceBusDataReceiverRole.id
    principalType: 'User'
    description: 'Allows for receive access to Azure Service Bus resources..'
  }
}

@description('Set permissions to give the Event Grid System Managed identity access to Service Bus')
resource gridServiceBusDataOwnwerRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespace.id, serviceBusDataOwnwerRole.id, eventGridStorageBlobTopic.id)
  scope: serviceBusNamespace
  properties: {
    principalId: eventGridStorageBlobTopic.identity.principalId
    roleDefinitionId: serviceBusDataOwnwerRole.id
    principalType: 'ServicePrincipal' 
    description: 'Allows this Microsoft Entra principal to access Event Hub data.'
  }
}

@description('Event Grid subscription to forward BlobCreated events to our Service Bus Queue.')
resource eventGridBlobCreatedServiceBusSubscription 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2023-12-15-preview' = {
  parent: eventGridStorageBlobTopic
  name: 'eventhub'
  properties: {
    deliveryWithResourceIdentity: {
      identity: {
        type: 'SystemAssigned'
      }
      destination: {
        properties: {
          resourceId: serviceBusNamespace::claimCheckBusQueue.id
        }
        endpointType: 'ServiceBusQueue'
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
    gridServiceBusDataOwnwerRoleAssignment
  ]
}


