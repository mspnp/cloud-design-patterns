# Cloud Design Patterns

This repository contains companion code for the article series found in the [Cloud Design Patterns](https://aka.ms/cloud-design-patterns) series in Azure Architecture Center.

## Patterns demonstrated

| Pattern definition | Sample code |
| :----------------- | :---------- |
| [Asynchronous Request-Reply](https://learn.microsoft.com/azure/architecture/patterns/async-request-reply) | ./async-request-reply/ |
| [Choreography](https://learn.microsoft.com/azure/architecture/patterns/choreography) | ./choreography/ |
| [Claim Check](https://learn.microsoft.com/azure/architecture/patterns/claim-check) | ./claim-check/ |
| [Health Endpoint Monitoring](https://learn.microsoft.com/azure/architecture/patterns/health-endpoint-monitoring) | [dotnet/AspNetCore.Docs#health-checks](https://github.com/dotnet/AspNetCore.Docs/tree/main/aspnetcore/host-and-deploy/health-checks/samples/8.x/HealthChecksSample)$^{*}$ |
| [Leader Election](https://learn.microsoft.com/azure/architecture/patterns/leader-election) | ./leader-election/ |
| [Pipes & Filters](https://learn.microsoft.com/azure/architecture/patterns/pipes-and-filters) | ./pipes-and-filters/ |
| [Priority Queue](https://learn.microsoft.com/azure/architecture/patterns/priority-queue) | ./priority-queue/ |
| [Rate Limiting](https://learn.microsoft.com/azure/architecture/patterns/rate-limiting-pattern) | Go: [Azure-Samples/go-batcher](https://github.com/Azure-Samples/go-batcher)<sup>\*</sup><br/>Java: [Azure-Samples/java-rate-limiting-pattern-sample](https://github.com/Azure-Samples/java-rate-limiting-pattern-sample)<sup>\*</sup> |
| [Saga](https://learn.microsoft.com/azure/architecture/reference-architectures/saga/saga) | [Azure-Samples/saga-orchestration-serverless](https://github.com/Azure-Samples/saga-orchestration-serverless)$^{*}$ |
| [Sharding](https://learn.microsoft.com/azure/architecture/patterns/sharding) | ./sharding/ |
| [Static Content Hosting](https://learn.microsoft.com/azure/architecture/patterns/static-content-hosting) | ./static-content-hosting/ |
| [Valet Key](https://learn.microsoft.com/en-us/azure/architecture/patterns/valet-key) | ./valet-key/ |

> Items donated with $^{*}$ are part of the Azure Architecture Center series, but the sample code is hosted in a different repository.

## Contributions

Please see our [Contributor guide](./CONTRIBUTING.md).

## Microsoft Open Source Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).

Resources:

- [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/)
- [Microsoft Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/)
- Contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with questions or concerns

With :heart: from Azure patterns & practices, [Azure Architecture Center](https://azure.com/architecture).
