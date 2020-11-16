FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 as base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build

WORKDIR /app
COPY Fabrikam.DroneDelivery.Common/*.csproj ./Fabrikam.DroneDelivery.Common/
COPY Fabrikam.DroneDelivery.DeliveryService/*.csproj ./Fabrikam.DroneDelivery.DeliveryService/
WORKDIR /app
RUN dotnet restore /app/Fabrikam.DroneDelivery.Common/
RUN dotnet restore /app/Fabrikam.DroneDelivery.DeliveryService/

WORKDIR /app
COPY Fabrikam.DroneDelivery.Common/. ./Fabrikam.DroneDelivery.Common/
COPY Fabrikam.DroneDelivery.DeliveryService/. ./Fabrikam.DroneDelivery.DeliveryService/

FROM build AS testrunner

WORKDIR /app/tests
COPY Fabrikam.DroneDelivery.DeliveryService.Tests/*.csproj .
WORKDIR /app/tests
RUN dotnet restore /app/tests/

WORKDIR /app/tests
COPY Fabrikam.DroneDelivery.DeliveryService.Tests/. .
ENTRYPOINT ["dotnet", "test", "--logger:trx"]

FROM build AS publish

LABEL Maintainer="Fernando Antivero (https://github.com/ferantivero)"

WORKDIR /app
RUN dotnet publish /app/Fabrikam.DroneDelivery.DeliveryService/ -c Release -o ../out

FROM base AS runtime

LABEL Maintainer="Microsoft Patterns & Practices (https://github.com/mspnp)"

LABEL Tags="Azure,AKS,DroneDelivery"

ARG user=deliveryuser

RUN useradd -m -s /bin/bash -U $user

WORKDIR /app
COPY --from=publish /app/out ./

RUN chown -R $user.$user /app

# Set it for subsequent commands
USER $user

ENTRYPOINT ["dotnet", "Fabrikam.DroneDelivery.DeliveryService.dll"]