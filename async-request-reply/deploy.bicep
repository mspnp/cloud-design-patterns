param location string = resourceGroup().location

var appStorageAccountName = toLower('stoapp${uniqueString(subscription().subscriptionId, resourceGroup().id)}')
var dataStorageAccountName = toLower('stodata${uniqueString(subscription().subscriptionId, resourceGroup().id)}')
var hostingPlanName = 'app-reqrep'
var functionAppName = 'fapp-reqrep-${uniqueString(subscription().subscriptionId, resourceGroup().id)}'
var serviceBusNamespaceName = toLower('sb-reqrep-${uniqueString(subscription().subscriptionId, resourceGroup().id)}')


resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: serviceBusNamespaceName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {}
}

resource serviceBusNamespace_outqueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'outqueue'
  properties: {
    defaultMessageTimeToLive: 'P14D'
  }
}

resource appStorageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: appStorageAccountName
  sku: {
    name: 'Standard_RAGRS'
  }
  kind: 'StorageV2'
  location: location
  properties: {}
}

resource dataStorageAccount 'Microsoft.Storage/storageAccounts@2019-04-01' = {
  name: dataStorageAccountName
  sku: {
    name: 'Standard_RAGRS'
  }
  kind: 'StorageV2'
  location: location
  properties: {}
}

resource dataStorageAccountName_default_data 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-09-01' = {
  name: '${dataStorageAccountName}/default/data'
  dependsOn: [
    dataStorageAccount
  ]
}

resource hostingPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    name: hostingPlanName
    computeMode: 'Dynamic'
  }
}

resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsDashboard'
          value: 'DefaultEndpointsProtocol=https;AccountName=${appStorageAccount.name};AccountKey=${listKeys(appStorageAccount.id, '2015-05-01-preview').key1}'
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${appStorageAccount.name};AccountKey=${listKeys(appStorageAccount.id, '2015-05-01-preview').key1}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${appStorageAccount.name};AccountKey=${listKeys(appStorageAccount.id, '2015-05-01-preview').key1}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionAppName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'WEBSITE_NODE_DEFAULT_VERSION'
          value: '8.11.1'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'ServiceBusConnectionAppSetting'
          value: listKeys(resourceId('Microsoft.ServiceBus/namespaces/AuthorizationRules', serviceBusNamespace.name, 'RootManageSharedAccessKey'), '2015-08-01').primaryConnectionString
        }
        {
          name: 'StorageConnectionAppSetting'
          value: 'DefaultEndpointsProtocol=https;AccountName=${dataStorageAccount.name};AccountKey=${listKeys(dataStorageAccount.id, '2015-05-01-preview').key1}'
        }
      ]
    }
  }
}

output functionAppName string = functionAppName
