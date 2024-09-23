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

@description('Built-in Azure RBAC role that is applied to a Storage account to grant "Storage Queue Data Contributor" privileges.')
resource storageQueueDataContributorRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
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
    allowSharedKeyAccess: false // Only allowed access by Managed Identity
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

  resource QueueServices 'queueServices' = {
    name: 'default'

    @description('The queue to serve as the message queue for the sample.')
    resource claimcheckqueue 'queues' = {
      name: 'claimcheckqueue'
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

@description('Set permissions to give the user principal access to Storage Queues from the sample applications')
resource queueContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount::QueueServices::claimcheckqueue.id, storageQueueDataContributorRole.id, principalId)
  scope: storageAccount::QueueServices::claimcheckqueue
  properties: {
    principalId: principalId
    roleDefinitionId: storageQueueDataContributorRole.id
    principalType: 'User' // 'ServicePrincipal' if this was a managed identity
    description: 'Allows this Microsoft Entra principal to access messages in this storage queue.'
  }
}

@description('Event Grid system topic to subscribe to blob created events.')
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

@description('Event Grid system topic subscription to queue blob created events.')
resource eventGridBlobCreatedQueueSubscription 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2023-12-15-preview' = {
  parent: eventGridStorageBlobTopic
  name: 'storagequeue'
  properties: {
    destination: {
      properties: {
        resourceId: storageAccount.id
        queueName: 'claimcheckqueue'
      }
      endpointType: 'StorageQueue'
    }
    filter: {
      includedEventTypes: [
        'Microsoft.Storage.BlobCreated'
      ]
    }
    eventDeliverySchema: 'EventGridSchema'
  }
}

