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

@description('Built-in Azure RBAC role that is applied to a Storage account to grant "Storage Blob Data Contributor" privileges. Used by the managed identity of the valet key Azure Function as for being able to delegate permissions to create blobs.')
resource storageBlobDataContributorRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
  scope: subscription()
}

@description('Built-in Azure RBAC role that is applied to a Storage account to grant "Storage Blob Delegator" privileges. Used by the managed identity of the valet key Azure Function to manage generate SaS tokens.')
resource storageBlobDelegatorRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: 'db58b8e5-c6ad-4a2a-8342-4190687cbf4a'
  scope: subscription()
}

/*** NEW RESOURCES ***/

@description('Workload logs.')
resource workloadLogs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'la-logs'
  location: location
  properties: {
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      dailyQuotaGb: -1
    }
  }
}

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
    allowSharedKeyAccess: false // Only managed identity allowed, we needed to change the way to generate SAS token using UserDelegationKey
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
    sasPolicy: {
      expirationAction: 'Log'
      sasExpirationPeriod: '00.00:10:00' // Log the creation of SaS tokens over 10 minutes long
    }
    keyPolicy: {
      keyExpirationPeriodInDays: 10 // Storage account key isn't used, require agressive rotation
    }
    networkAcls: {
      defaultAction: 'Allow' // For this sample, public Internet access is expected
      bypass: 'None'
      virtualNetworkRules: []
      ipRules: []
    }
  }

  resource blobContainers 'blobServices' = {
    name: 'default'

    @description('The blob container that SaS tokens will be generated for.')
    resource uploadsContainer 'containers' = {
      name: 'uploads'
    }
  }
}

@description('Enable access logs on blob storage.')
resource azureStorageBlobAccessLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'default'
  scope: storageAccount::blobContainers
  properties: {
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
    workspaceId: workloadLogs.id
  }
}

@description('SaS tokens are created at the account level, allow our identified principal the permissions necessary to create those.')
resource blobUploadStorageDelegator 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, storageBlobDelegatorRole.id, principalId)
  scope: storageAccount
  properties: {
    principalId: principalId
    roleDefinitionId: storageBlobDelegatorRole.id
    principalType: 'User' // 'ServicePrincipal' if this was a managed identity
    description: 'Allows this Microsoft Entra principal to create Entra ID-signed SaS tokens for this storage account.'
  }
}

@description('User delegation requires the user doing the delegating to SaS to also have the permissions being delgated. So scoping a Data Contributor to the container. In this scenario, technically this principal only needs permissions to create blobs.')
resource blobContributorUploadStorage 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount::blobContainers::uploadsContainer.id, storageBlobDataContributorRole.id, principalId)
  scope: storageAccount::blobContainers::uploadsContainer
  properties: {
    principalId: principalId
    roleDefinitionId: storageBlobDataContributorRole.id
    principalType: 'User' // 'ServicePrincipal' if this was a managed identity
    description: 'Allows this Microsoft Entra principal to manage blobs in this storage container.'
  }
}
