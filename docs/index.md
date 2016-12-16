---
title: Cloud Design Patterns | Azure | Microsoft Docs 
description: Cloud Design Patterns for Microsoft Azure
keywords: Azure
author: bennage
manager: marksou
ms.date: 12/14/2016
ms.topic: article
---

# Cloud Design Patterns

[!INCLUDE [pnp-branding](../includes/header.md)]

These design patterns are useful for building reliable, scalable, secure applications in the cloud.

Each pattern describes the problem that the pattern addresses, considerations for applying the pattern, and an example based on Microsoft Azure. Most of the patterns include code samples or snippets that show how to implement the pattern on Azure. However, most of the patterns are relevant to any distributed system, whether hosted on Azure or on other cloud platforms.

| Pattern | Description |
| ------- | ----------- |
| [Cache-Aside](cache-aside.md) | Load data on demand from a data store into a cache. |
| [Circuit Breaker](circuit-breaker.md) | Handle faults that might take a variable amount of time to recover from. |
| [Command and Query Responsibility Segregation (CQRS)](command-and-query-responsibility-segregation-cqrs.md) | Segregate operations that read data from operations that update data by using separate interfaces. |
| [Compensating Transaction](compensating-transaction.md) | Undo the work performed by a series of steps, which together define an eventually consistent operation, if one or more steps fail.  |
| [Competing Consumers](competing-consumers.md) | Multiple concurrent consumers process messages on the same messaging channel.  |
| [Compute Resource Consolidation](compute-resource-consolidation.md) | Consolidate multiple tasks or operations into a single computational unit. | 
| [Event Sourcing](event-sourcing.md) | Use an append-only store to record the full series of actions taken on a data set.  |
| [External Configuration Store](external-configuration-store.md) | Move configuration information out of the application deployment package to a centralized location. |
| [Federated Identity](federated-identity.md) | Delegate authentication to an external identity provider.  |
| [Gatekeeper](gatekeeper.md) | Use a dedicated host instance to broker between clients and the application or service. | 
| [Health Endpoint Monitoring](health-endpoint-monitoring.md) | Implement functional checks in an application, and provide the status through an endpoint that can be queried. | 
| [Index Table](index-table.md) | Create indexes over fields that are frequently referenced by queries. |
| [Leader Election](leader-election.md) | In a distributed application, elect one instance as the leader that manages the others. | 
| [Materialized View](materialized-view.md) | Generate prepopulated views over one or more data stores, to support efficient querying. | 
| [Pipes and Filters](pipes-and-filters.md) | Decompose a complex task into a series of separate reusable elements.  |
| [Priority Queue](priority-queue.md) | Prioritize requests, so that requests with a higher priority are processed more quickly. | 
| [Queue-Based Load Leveling](queue-based-load-leveling.md) | Use a queue as a buffer between a service and a task, to smooth out intermittent heavy loads. |
| [Retry](retry.md) | Handle transient failures to a service, by transparently retrying a failed operation. |
| [Runtime Reconfiguration](runtime-reconfiguration.md) | Reconfigure an application at runtime, without redeploying or restarting the application. | 
| [Scheduler Agent Supervisor](scheduler-agent-supervisor.md) | Coordinate a set of distributed actions as a single operation. |
| [Sharding](sharding.md) | Divide a data store into a set of horizontal partitions.  |
| [Static Content Hosting](static-content-hosting.md) | Deploy static content to a cloud storage service that can deliver them directly to the client. | 
| [Throttling](throttling.md) | Limit the consumption of resources by an application instance, a tenant, or an entire service.  |
| [Valet Key](valet-key.md) | Issue clients a token that provides restricted direct access to a resource, in order to offload data transfer from the application. |

