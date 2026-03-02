targetScope = 'resourceGroup'

@minLength(5)
@description('Location of the resources. Defaults to resource group location.')
param location string = resourceGroup().location

@minLength(15)
@description('Service Bus Namespace Name.')
param serviceBusNamespaceName string

@description('Defines the name of the Storage Account used by the Function Apps. It uses a unique string based on the resource group ID to ensure global uniqueness.')
param storageAccountName string = 'st${uniqueString(resourceGroup().id)}'

@description('Sets the name of the Application Insights resource for monitoring and diagnostics. Like the storage account, it uses a unique string based on the resource group ID.')
param appInsightsName string = 'ai${uniqueString(resourceGroup().id)}'

var logAnalyticsName = 'loganalytics-${uniqueString(subscription().subscriptionId, resourceGroup().id)}'

var senderRoleId = '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39' // Azure Service Bus Data Sender
var receiverRoleId = '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0' // Azure Service Bus Data Receiver

resource queueNamespacesResource 'Microsoft.ServiceBus/namespaces@2025-05-01-preview' = {
  name: serviceBusNamespaceName
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    geoDataReplication: {
      maxReplicationLagDurationInSeconds: 0
      locations: [
        {
          locationName: location
          roleType: 'Primary'
        }
      ]
    }
    premiumMessagingPartitions: 0
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
    zoneRedundant: false
  }
}

resource queueNamespacesResourceRootManageSharedAccessKey 'Microsoft.ServiceBus/namespaces/authorizationrules@2025-05-01-preview' = {
  parent: queueNamespacesResource
  name: 'RootManageSharedAccessKey'
  properties: {
    rights: [
      'Listen'
      'Manage'
      'Send'
    ]
  }
}

resource queueNamespacesResourceNetworkRules 'Microsoft.ServiceBus/namespaces/networkrulesets@2025-05-01-preview' = {
  parent: queueNamespacesResource
  name: 'default'
  properties: {
    publicNetworkAccess: 'Enabled'
    defaultAction: 'Allow'
    virtualNetworkRules: []
    ipRules: []
    trustedServiceAccessEnabled: false
  }
}

resource queueNamespacesResourceTopic 'Microsoft.ServiceBus/namespaces/topics@2025-05-01-preview' = {
  parent: queueNamespacesResource
  name: 'messages'
  properties: {
    maxMessageSizeInKilobytes: 256
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: false
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    enableBatchedOperations: true
    status: 'Active'
    supportOrdering: true
    enablePartitioning: false
    enableExpress: false
  }
}

resource queueNamespacesResourceTopicHigPriority 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2025-05-01-preview' = {
  parent: queueNamespacesResourceTopic
  name: 'highPriority'
  properties: {
    isClientAffine: false
    lockDuration: 'PT1M'
    requiresSession: false
    deadLetteringOnMessageExpiration: false
    deadLetteringOnFilterEvaluationExceptions: true
    maxDeliveryCount: 10
    status: 'Active'
    enableBatchedOperations: true
  }
}

resource queueNamespacesResourceTopicLowPriority 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2025-05-01-preview' = {
  parent: queueNamespacesResourceTopic
  name: 'lowPriority'
  properties: {
    isClientAffine: false
    lockDuration: 'PT1M'
    requiresSession: false
    deadLetteringOnMessageExpiration: false
    deadLetteringOnFilterEvaluationExceptions: true
    maxDeliveryCount: 10
    status: 'Active'
    enableBatchedOperations: true
  }
}

resource queueNamespacesResourceTopicHigPriorityRules 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2025-05-01-preview' = {
  parent: queueNamespacesResourceTopicHigPriority
  name: 'priorityFilter'
  properties: {
    action: {}
    filterType: 'SqlFilter'
    sqlFilter: {
      sqlExpression: 'Priority = \'highpriority\''
      compatibilityLevel: 20
    }
  }
}

resource queueNamespacesResourceTopicLowPriorityRules 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2025-05-01-preview' = {
  parent: queueNamespacesResourceTopicLowPriority
  name: 'priorityFilter'
  properties: {
    action: {}
    filterType: 'SqlFilter'
    sqlFilter: {
      sqlExpression: 'Priority = \'lowpriority\''
      compatibilityLevel: 20
    }
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  name: logAnalyticsName
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

resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${queueNamespacesResource.name}-diagnostic'
  scope: queueNamespacesResource
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
