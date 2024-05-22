targetScope = 'resourceGroup'

@minLength(5)
@description('Location of the resources. Defaults to resource group location.')
param location string = resourceGroup().location

@minLength(5)
@description('The globally unique name for the storage account.')
param storageAccountName string

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
