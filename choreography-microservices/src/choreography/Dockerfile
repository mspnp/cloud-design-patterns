FROM microsoft/dotnet:2.2-aspnetcore-runtime as base
WORKDIR /app
EXPOSE 8080

FROM microsoft/dotnet:2.2-sdk AS build

WORKDIR /app
COPY Fabrikam.Choreography.ChoreographyService/*.csproj ./Fabrikam.Choreograph.ChoreographyService/
WORKDIR /app
RUN dotnet restore /app/Fabrikam.Choreography.ChoreographyService/

WORKDIR /app
COPY Fabrikam.Choreography.ChoreographyService/. ./Fabrikam.Choreography.ChoreographyService/

FROM build AS testrunner

WORKDIR /app/tests
COPY Frabrikam.Choreography.ChoreographyService.Tests/*.csproj .
WORKDIR /app/tests
RUN dotnet restore /app/tests/

WORKDIR /app/tests
COPY Frabrikam.Choreography.ChoreographyService.Tests/. .
ENTRYPOINT ["dotnet", "test", "--logger:trx"]

FROM build AS publish

WORKDIR /app
RUN dotnet publish /app/Fabrikam.Choreography.ChoreographyService/ -c Release -o ../out

FROM base AS runtime

LABEL Tags="Azure,AKS,Choreography"

ARG user=choreographyuser

RUN useradd -m -s /bin/bash -U $user

WORKDIR /app
COPY --from=publish /app/out ./
COPY scripts/. ./
RUN \
    # Ensures the entry point is executable
    chmod ugo+x /app/run.sh

RUN chown -R $user.$user /app

# Set it for subsequent commands
USER $user

ENTRYPOINT ["/bin/bash", "/app/run.sh"]