targetScope = 'resourceGroup'

@minLength(5)
@description('Location of the resources. Defaults to resource group location.')
param location string = resourceGroup().location

@minLength(36)
@description('The guid of the principal running the valet key generation code. In Azure this would be replaced with the managed identity of the Azure Function, when running locally it will be your user.')
param principalId string

@minLength(5)
@description('The globally unique name for the storage account.')
param storageAccountName string

/*** EXISTING RESOURCES ***/

@description('Built-in Azure RBAC role that is applied to a Storage account to grant "Storage Blob Delegator" privileges. Used by the managed identity of the valet key Azure Function to manage generate SaS tokens.')
resource storageBlobContributorRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
  scope: subscription()
}

/*** NEW RESOURCES ***/

@description('The Azure Storage account which will be where authorized clients upload large blobs to. The Azure Function will hand out scoped, time-limited SaS tokens for this blobs in this account.')
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
    allowSharedKeyAccess: false // Only managed identity allowed
    isLocalUserEnabled: false
    isHnsEnabled: false
    isNfsV3Enabled: false
    isSftpEnabled: false
    largeFileSharesState: 'Disabled'
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled' // In a valet key scenario, typically clients are not hosted in your virtual network. However if they were, then you could disable this. In this sample, you'll be accessing this from your workstation.
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
}

@description('SaS tokens are created at the account level, allow our identified principal the permissions necessary to create those.')
resource blobUploadStorageDelegator 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, storageBlobContributorRole.id, principalId)
  scope: storageAccount
  properties: {
    principalId: principalId
    roleDefinitionId: storageBlobContributorRole.id
    principalType: 'User' // 'ServicePrincipal' if this was a managed identity
    description: 'Allows this Microsoft Entra principal upload the files.'
  }
}
