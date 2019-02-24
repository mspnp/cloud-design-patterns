#!/bin/bash
set -e

if ! command az >/dev/null; then
    echo "Must install Azure CLI: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli" >&2
    exit 1
fi

on_error() {
    set +e
    echo "There was an error, execution halted" >&2
    exit 1
}

trap on_error ERR

rm -f azcli-execution.log 

if [[ -z $1 ]]; then
    export RG="pnp3"
else
    export RG="${1}"
fi
export PREFIX="${RG}cc"
export LOCATION="eastus"

echo "create: group ${RG}"
az group create --name "${RG}" --location "${LOCATION}" -o json >> azcli-execution.log

echo "create: storage account ${PREFIX}storage"
az storage account create --name "${PREFIX}storage" --kind "StorageV2" --location "${LOCATION}" --resource-group "${RG}" --sku "Standard_LRS" -o json >> azcli-execution.log

echo "show: storage account ${PREFIX}storage"
export SID=$(az storage account show --name "${PREFIX}storage" --query "id" --resource-group "${RG}" -o tsv)

echo "show: storage account ${PREFIX}storage connection string"
export STORAGE_CONNECTION_STRING=$(az storage account show-connection-string --resource-group "${RG}" --name "${PREFIX}storage" -o tsv)

echo "create: storage container 'attachments'"
az storage container create --name "attachments" --connection-string "${STORAGE_CONNECTION_STRING}" -o json >> azcli-execution.log

echo "create: service bus namespace ${PREFIX}sbname"
az servicebus namespace create --name "${PREFIX}sbname" --resource-group "${RG}" --location "${LOCATION}" -o json >> azcli-execution.log

echo "create: service bus queue ${PREFIX}sbq"
az servicebus queue create --resource-group "${RG}" --namespace-name "${PREFIX}sbname" --name "${PREFIX}sbq" -o json >> azcli-execution.log

echo "show: service bus ${PREFIX} connection string"
export SERVICE_BUS_CONNECTION_STRING=$(az servicebus namespace authorization-rule keys list --resource-group "${RG}" --namespace-name "${PREFIX}sbname" --name RootManageSharedAccessKey --query "primaryConnectionString" -o tsv)

echo "create: resource ${PREFIX}functionappinsights"
az resource create --name "${PREFIX}functionappinsights" --location "${LOCATION}" --properties '{"Application_Type": "other", "ApplicationId": "function", "Flow_Type": "Redfield"}' --resource-group "${RG}" --resource-type "Microsoft.Insights/components" -o json >> azcli-execution.log

echo "show: resource ${PREFIX}functionappinsights"
export APPINSIGHTS_KEY=$(az resource show --name "${PREFIX}functionappinsights" --query "properties.InstrumentationKey" --resource-group "${RG}" --resource-type "Microsoft.Insights/components" -o tsv)

echo "create: appservice plan ${PREFIX}plan"
az appservice plan create --name "${PREFIX}plan" --number-of-workers "1" --resource-group "${RG}" --sku "S1" -o json >> azcli-execution.log

echo "create: functionapp ${PREFIX}functionapp"
az functionapp create --name "${PREFIX}functionapp" --plan "${PREFIX}plan" --resource-group "${RG}" --storage-account "${PREFIX}storage" -o json >> azcli-execution.log

echo "config-zip: functionapp deployment source ${PREFIX}functionapp"
az functionapp deployment source config-zip --name "${PREFIX}functionapp" --resource-group "${RG}" --src "./sample-3-bin.zip" -o json >> azcli-execution.log

echo "set: functionapp config appsettings for appinsights"
az functionapp config appsettings set --name "${PREFIX}functionapp" --resource-group "${RG}" --settings "APPINSIGHTS_INSTRUMENTATIONKEY=${APPINSIGHTS_KEY}" -o json >> azcli-execution.log

echo "set: functionapp config appsettings for storage account connection string"
az functionapp config appsettings set --name "${PREFIX}functionapp" --resource-group "${RG}" --settings "STORAGE_CONNECTION_STRING=${STORAGE_CONNECTION_STRING}" -o json >> azcli-execution.log

echo "set: functionapp config appsettings for service bus connection string"
az functionapp config appsettings set --name "${PREFIX}functionapp" --resource-group "${RG}" --settings "SERVICE_BUS_CONNECTION_STRING=${SERVICE_BUS_CONNECTION_STRING}" -o json >> azcli-execution.log

echo "set: functionapp config appsettings for queue name"
az functionapp config appsettings set --name "${PREFIX}functionapp" --resource-group "${RG}" --settings "QUEUE_NAME=${PREFIX}sbq" -o json >> azcli-execution.log

echo "set: functionapp config appsettings for service bus queue name"
az functionapp config appsettings set --name "${PREFIX}functionapp" --resource-group "${RG}" --settings "APPINSIGHTS_INSTRUMENTATIONKEY=${APPINSIGHTS_KEY}" -o json >> azcli-execution.log

echo "done"

echo "The following values will be copied into App.config (using App.config.template as source):"

echo "STORAGE_CONNECTION_STRING = ${STORAGE_CONNECTION_STRING}"
sed "s|{STORAGE_CONNECTION_STRING}|${STORAGE_CONNECTION_STRING}|g" client-consumer/App.config.template > client-consumer/App.config

echo "SERVICE_BUS_CONNECTION_STRING = ${SERVICE_BUS_CONNECTION_STRING}"
sed -i.bak "s|{SERVICE_BUS_CONNECTION_STRING}|${SERVICE_BUS_CONNECTION_STRING}|g" client-consumer/App.config

echo "QUEUE_NAME = ${PREFIX}sbq"
sed -i.bak "s|{QUEUE_NAME}|${PREFIX}sbq|g" client-consumer/App.config

rm -f client-consumer/App.config.bak

echo "done"

