param location string = resourceGroup().location
param storageAccountName string = 'st${uniqueString(resourceGroup().id)}'
param serviceBusNamespaceName string
param appInsightsName string = 'ai${uniqueString(resourceGroup().id)}'

var senderRoleId = '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
var receiverRoleId = '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'

resource storageAccount 'Microsoft.Storage/storageAccounts@2025-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    defaultToOAuthAuthentication: true
    publicNetworkAccess: 'Enabled'
    allowCrossTenantReplication: false
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    networkAcls: {
      bypass: 'AzureServices'
      virtualNetworkRules: []
      ipRules: []
      defaultAction: 'Allow'
    }
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        file: {
          keyType: 'Account'
          enabled: true
        }
        blob: {
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

module functionApp './sites.bicep' = [
  for name in [
    'funcPriorityQueueSender'
    'funcPriorityQueueConsumerLow'
    'funcPriorityQueueConsumerHigh'
  ]: {
    name: name
    params: {
      location: location
      functionAppName: name
      storageAccountName: storageAccount.name
      serviceBusNamespaceName: serviceBusNamespaceName
      roleId: name == 'funcPriorityQueueSender' ? senderRoleId : receiverRoleId
      appInsightsName: appInsights.name
      scaleUp: name == 'funcPriorityQueueConsumerHigh' ? 200 : 40
    }
  }
]
