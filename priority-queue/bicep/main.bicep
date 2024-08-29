targetScope = 'resourceGroup'

@minLength(5)
@description('Location of the resources. Defaults to resource group location.')
param location string = resourceGroup().location

@minLength(15)
@description('Service Bus Namespace Name.')
param queueNamespaces string

@minLength(36)
@description('The guid of the principal running the valet key generation code. In Azure this would be replaced with the managed identity of the Azure Function, when running locally it will be your user.')
param principalId string

var logAnalyticsName = 'loganalytics-${uniqueString(subscription().subscriptionId, resourceGroup().id)}'

var senderServiceBusRole = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
) // Azure Service Bus Data Sender
var receiverServiceBusRole = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
) // Azure Service Bus Data Receiver

resource queueNamespacesResource 'Microsoft.ServiceBus/namespaces@2023-01-01-preview' = {
  name: queueNamespaces
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

resource queueNamespacesResourceRootManageSharedAccessKey 'Microsoft.ServiceBus/namespaces/authorizationrules@2023-01-01-preview' = {
  parent: queueNamespacesResource
  name: 'RootManageSharedAccessKey'
  location: location
  properties: {
    rights: [
      'Listen'
      'Manage'
      'Send'
    ]
  }
}

resource queueNamespacesResourceNetworkRules 'Microsoft.ServiceBus/namespaces/networkrulesets@2023-01-01-preview' = {
  parent: queueNamespacesResource
  name: 'default'
  location: location
  properties: {
    publicNetworkAccess: 'Enabled'
    defaultAction: 'Allow'
    virtualNetworkRules: []
    ipRules: []
    trustedServiceAccessEnabled: false
  }
}

resource queueNamespacesResourceTopic 'Microsoft.ServiceBus/namespaces/topics@2023-01-01-preview' = {
  parent: queueNamespacesResource
  name: 'messages'
  location: location
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

resource queueNamespacesResourceTopicHigPriority 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2023-01-01-preview' = {
  parent: queueNamespacesResourceTopic
  name: 'highPriority'
  location: location
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

resource queueNamespacesResourceTopicLowPriority 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2023-01-01-preview' = {
  parent: queueNamespacesResourceTopic
  name: 'lowPriority'
  location: location
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

resource queueNamespacesResourceTopicHigPriorityRules 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2023-01-01-preview' = {
  parent: queueNamespacesResourceTopicHigPriority
  name: 'priorityFilter'
  location: location
  properties: {
    action: {}
    filterType: 'SqlFilter'
    sqlFilter: {
      sqlExpression: 'Priority = \'highpriority\''
      compatibilityLevel: 20
    }
  }
}

resource LqueueNamespacesResourceTopicLowPriorityRules 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2023-01-01-preview' = {
  parent: queueNamespacesResourceTopicLowPriority
  name: 'priorityFilter'
  location: location
  properties: {
    action: {}
    filterType: 'SqlFilter'
    sqlFilter: {
      sqlExpression: 'Priority = \'lowpriority\''
      compatibilityLevel: 20
    }
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' = {
  name: logAnalyticsName
  location: location
  properties: any({
    retentionInDays: 30
    features: {
      searchVersion: 1
    }
    sku: {
      name: 'PerGB2018'
    }
  })
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

// Assign Role to allow sending messages to the Service Bus
resource serviceBusSenderRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, principalId, 'ServiceBusSenderRole')
  scope: queueNamespacesResource
  properties: {
    roleDefinitionId: senderServiceBusRole
    principalId: principalId
    principalType: 'User' // 'ServicePrincipal' if this was a managed identity
  }
}

// Assign Role to allow receiving messages from the Service Bus
resource serviceBusReceiverRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, principalId, 'ServiceBusReceiverRole')
  scope: queueNamespacesResource
  properties: {
    roleDefinitionId: receiverServiceBusRole
    principalId: principalId
    principalType: 'User' // 'ServicePrincipal' if this was a managed identity
  }
}
