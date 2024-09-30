targetScope = 'resourceGroup'

@minLength(5)
@description('Location of the resources. Defaults to resource group location.')
param location string = resourceGroup().location

@minLength(5)
@description('The globally unique name for the storage account.')
param storageAccountName string

@minLength(5)
@description('The user assignee object id to be granted with Storage Blob Data Contributor permissions.')
param assigneeObjectId string


/*** EXISTING SUBSCRIPTION RESOURCES ***/

resource storageBlobDataContributorRole 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  name: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
  scope: subscription()
}

/*** EXISTING RESOURCES ***/

/*** NEW RESOURCES ***/

@description('The Azure Storage account with static website support enabled and where operations upload static content to.')
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowCrossTenantReplication: false
    allowSharedKeyAccess: false // This storage account is not configured to allow SaS tokens, since all content in this static content hosting example is public. For content that need to be protected, this field could be enabled.
    isLocalUserEnabled: false
    isHnsEnabled: false
    isNfsV3Enabled: false
    isSftpEnabled: false
    largeFileSharesState: 'Disabled'
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled' // In a static content hosting scenario, typically clients are not hosted in your virtual network. However if they were, then you could disable this. In this sample, you'll be accessing this from your workstation.
    supportsHttpsTrafficOnly: true
    defaultToOAuthAuthentication: true
    allowedCopyScope: 'PrivateLink'
    networkAcls: {
      defaultAction: 'Allow' // For this sample, public Internet access is expected since content is delivered to anonymous client
      bypass: 'None'
      virtualNetworkRules: []
      ipRules: []
    }
  }
}

resource storageAccountUserStorageBlobDataContributorRole_roleAssignment 'Microsoft.Authorization/roleAssignments@2020-10-01-preview' = {
  scope: storageAccount
  name: guid(storageAccount.id, storageBlobDataContributorRole.id, assigneeObjectId)
  properties: {
    roleDefinitionId: storageBlobDataContributorRole.id
    description: 'Allows cluster identity to join the nodepool vmss resources to this subnet.'
    principalId: assigneeObjectId
    principalType: 'User'
  }
}

