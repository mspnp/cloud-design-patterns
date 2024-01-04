# Choregraphy Pattern - Code sample

This code sample contains a set of services designed to show how to implement the [choreography pattern](https://learn.microsoft.com/azure/architecture/patterns/choreography) using a [Drone Delivery app](https://github.com/mspnp/microservices-reference-implementation). These services run as a microservices in an [Azure Kubernetes Service](https://learn.microsoft.com/azure/aks/) cluster.

## Projects and folder structure
> Technologies used: .NET 5, java, Azure Event Grid, Azure Cosmos DB, Azure Key Vault 

- Ingestion service. This business service Receives delivery requests made by a client through an HTTP endpoint and buffers them by sending them to a message bus (./src/ingestion).
- Package service. This business service creates and update packages (./src/package).
- Drone scheduler service. This business service assigns a drone to deliver the package (./src/dronescheduler).
- Delivery service. This business manages deliveries that are scheduled or in-transit (./src/delivery).
- Choreography service. Implements the workflow of the Drone Delivery app transactions by correlating calls across all business services and Event Grid hops.

## The choreography service web API

This service implements the application flow by receiving a list of [EventGrid](https://learn.microsoft.com/azure/event-grid/) events. Each event in the list has a Choreography operation associated to its event type, here is the list of operations:

            - ScheduleDelivery
            - RescheduledDelivery
            - CancelDelivery
            - GetDrone
            - CreatePackage

Depending on the operation, the choreography service makes calls to the associated business services and, if the call is successful, it sets the EventType to the next choreography step (the next operation). This workflow continues until the entire request has been processed.

[Learn more about the application design](https://learn.microsoft.com/azure/architecture/patterns/choreography#design)

## Contributions

Please see our [contributor guide](../CONTRIBUTING.md).

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information, see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact <opencode@microsoft.com> with any additional questions or comments.

With :heart: from Microsoft Patterns & Practices, [Azure Architecture Center](https://aka.ms/architecture).
