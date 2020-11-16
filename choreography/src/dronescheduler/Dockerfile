FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build

LABEL Maintainer="Fernando Antivero (https://github.com/ferantivero)"

WORKDIR /app
COPY delivery/Fabrikam.DroneDelivery.Common/*.csproj ./delivery/Fabrikam.DroneDelivery.Common/
COPY dronescheduler/Fabrikam.DroneDelivery.DroneScheduler/*.csproj ./dronescheduler/Fabrikam.DroneDelivery.DroneScheduler/
WORKDIR /app
RUN dotnet restore /app/delivery/Fabrikam.DroneDelivery.Common/
RUN dotnet restore /app/dronescheduler/Fabrikam.DroneDelivery.DroneScheduler/

WORKDIR /app
COPY delivery/Fabrikam.DroneDelivery.Common/. ./delivery/Fabrikam.DroneDelivery.Common/
COPY dronescheduler/Fabrikam.DroneDelivery.DroneScheduler/. ./dronescheduler/Fabrikam.DroneDelivery.DroneScheduler/

FROM build AS publish

LABEL Maintainer="Microsoft Patterns & Practices (https://github.com/mspnp)"

WORKDIR /app
RUN dotnet publish /app/dronescheduler/Fabrikam.DroneDelivery.DroneScheduler/ -c Release -o ../../out

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

ENTRYPOINT ["dotnet", "Fabrikam.DroneDelivery.DroneScheduler.dll"]
