targetScope = 'resourceGroup'

@minLength(3)
@description('The name of the storage account. Must be globally unique.')
param storageAccountName string

@minLength(36)
@description('The Object ID (GUID) the user your Azure CLI session is logged in as.')
param userObjectId string

@minLength(5)
@description('Location of the resources. Defaults to resource group location')
param location string = resourceGroup().location

/*** EXISTING RESOURCES ***/

@description('Built-in Azure RBAC role that is applied to a Storage account to grant "Storage Blob Data Contributor" privileges. Granted to the user provided in the paramters.')
resource storageBlobDataContributorRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
  scope: subscription()
}

@description('Built-in Azure RBAC role that is applied to a Storage account to grant "Storage Queue Data Contributor" privileges. Granted to the user provided in the paramters.')
resource storageQueueDataContributorRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
  scope: subscription()
}

/*** NEW RESOURCES ***/

@description('The Azure Storage account which will contain the pipes (queues) and the images to be sent through the filters (Azure Functions).')
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: true
    allowCrossTenantReplication: false
    allowSharedKeyAccess: false   // This storage account is configured to only work with Microsoft Entra ID authentication
    isLocalUserEnabled: false
    isHnsEnabled: false
    isNfsV3Enabled: false
    isSftpEnabled: false
    largeFileSharesState: 'Disabled'
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'   // This sample does not use private networking, but could be configured to use Private Link connections if fully deploy to Azure
    supportsHttpsTrafficOnly: true
  }

  resource blobContainers 'blobServices' = {
    name: 'default'
    
    resource images 'containers' = {
      name: 'images'
    }

    resource processed 'containers' = {
      name: 'processed'
    }
  }

  resource queueContainers 'queueServices' = {
    name: 'default'
    
    resource pipexfty 'queues' = {
      name: 'pipe-xfty'
    }
  }
}

@description('Grant Storage Blob Data Contributor to the user.')
resource blobContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(userObjectId, storageAccount.id, storageBlobDataContributorRole.id)
  scope: storageAccount
  properties: {
    principalId: userObjectId
    roleDefinitionId: storageBlobDataContributorRole.id
    principalType: 'User'
  }
}

@description('Grant Storage Queue Data Contributor to the user.')
resource queueContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(userObjectId, storageAccount.id, storageQueueDataContributorRole.id)
  scope: storageAccount
  properties: {
    principalId: userObjectId
    roleDefinitionId: storageQueueDataContributorRole.id
    principalType: 'User'
  }
}
