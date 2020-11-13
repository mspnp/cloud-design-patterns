FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
WORKDIR /src

# Copy over the test and main code csproj file into the image and restore dependencies
COPY Fabrikam.Choreography.ChoreographyService/*.csproj Fabrikam.Choreography.ChoreographyService/
COPY Frabrikam.Choreography.ChoreographyService.Tests/*.csproj Frabrikam.Choreography.ChoreographyService.Tests/
RUN dotnet restore Fabrikam.Choreography.ChoreographyService/
RUN dotnet restore Frabrikam.Choreography.ChoreographyService.Tests/

# Now copy over all the files
COPY Fabrikam.Choreography.ChoreographyService Fabrikam.Choreography.ChoreographyService
COPY Frabrikam.Choreography.ChoreographyService.Tests Frabrikam.Choreography.ChoreographyService.Tests

# Build the tests (which also builds the app)
WORKDIR /src/Frabrikam.Choreography.ChoreographyService.Tests
RUN dotnet build

# run the unit tests (on demand)
FROM build as testrunner
WORKDIR /src/Frabrikam.Choreography.ChoreographyService.Tests
ENTRYPOINT ["dotnet", "test", "--logger:trx"]

# publish the code
FROM build AS publish
WORKDIR /src/Fabrikam.Choreography.ChoreographyService
RUN dotnet publish -c Release -o ../out

# Run the app
FROM base AS final

LABEL Tags="Azure,AKS,Choreography"

ARG user=choreographyuser

RUN useradd -m -s /bin/bash -U $user

WORKDIR /app
COPY --from=publish /src/out ./
COPY scripts/. ./
RUN \
    # Ensures the entry point is executable
    chmod ugo+x /app/run.sh

RUN chown -R $user.$user /app

# Set it for subsequent commands
USER $user
ENTRYPOINT ["dotnet", "Fabrikam.Choreography.ChoreographyService.dll"]