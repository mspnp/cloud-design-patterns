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
    allowSharedKeyAccess: true
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
    kafkaEnabled: true
  }

  @description('The Event Hubs to send/receive claim-check event messages.')
  resource claimCheckEventHub 'eventhubs' = {
    name: 'evh-claimcheck'
    properties: {
      messageRetentionInDays: 1
      partitionCount: 2
    }
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
