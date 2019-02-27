#!/bin/bash
set -e

if ! command az >/dev/null; then
    echo "Must install Azure CLI: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli" >&2
    exit 1
fi

export curVer=`printf "%03d%03d%03d" $(az --version | grep 'eventgrid' | awk '{gsub(/[()]/, "", $2); print $2}' | tail -1 | tr '.' ' ')`
export reqVer=`printf "%03d%03d%03d" $(echo '0.4.0' | tr '.' ' ')`
if [[ curVer -lt reqVer ]] ; then
    echo "Version 0.4.0 at least required for the evengrid module" >&2    
    echo 'You can add it by doing this:' >&2
    echo 'sudo az extension add --name "eventgrid"' >&2
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
    export RG="pnp1"
else    
    export RG="${1}"
fi

export PREFIX="${RG}cc"
export LOCATION="eastus"

echo "create: group ${RG}"
az group create --name "${RG}" --location "${LOCATION}" -o json >> azcli-execution.log

echo "create: storage account ${PREFIX}storage"
az storage account create --name "${PREFIX}storage" --kind "StorageV2" --location "${LOCATION}" --resource-group "${RG}" --sku "Standard_LRS" -o json >> azcli-execution.log

echo "create: storage container 'sample'"
az storage container create --name "sample" --account-name "${PREFIX}storage" -o json >> azcli-execution.log

echo "show: storage account ${PREFIX}storage"
export SID=$(az storage account show --name "${PREFIX}storage" --query "id" --resource-group "${RG}" -o tsv)

echo "show-connection-string: storage account ${PREFIX}storage"
export STORAGE_CONNECTION_STRING=$(az storage account show-connection-string --name "${PREFIX}storage" --resource-group "${RG}" -o tsv)

echo "create: storage queue ${PREFIX}queue"
az storage queue create --name "${PREFIX}queue" --account-name "${PREFIX}storage" -o json >> azcli-execution.log

echo "create: resource ${PREFIX}functionappinsights"
az resource create --name "${PREFIX}functionappinsights" --location "${LOCATION}" --properties '{"Application_Type": "other", "ApplicationId": "function", "Flow_Type": "Redfield"}' --resource-group "${RG}" --resource-type "Microsoft.Insights/components" -o json >> azcli-execution.log

echo "show: resource ${PREFIX}functionappinsights"
export APPINSIGHTS_KEY=$(az resource show --name "${PREFIX}functionappinsights" --query "properties.InstrumentationKey" --resource-group "${RG}" --resource-type "Microsoft.Insights/components" -o tsv)

echo "create: appservice plan ${PREFIX}plan"
az appservice plan create --name "${PREFIX}plan" --number-of-workers "1" --resource-group "${RG}" --sku "S1" -o json >> azcli-execution.log

echo "create: functionapp ${PREFIX}functionapp"
az functionapp create --name "${PREFIX}functionapp" --plan "${PREFIX}plan" --resource-group "${RG}" --storage-account "${PREFIX}storage" -o json >> azcli-execution.log

echo "config-zip: functionapp deployment source ${PREFIX}functionapp"
az functionapp deployment source config-zip --name "${PREFIX}functionapp" --resource-group "${RG}" --src "./sample-1-bin.zip" -o json >> azcli-execution.log

echo "set: functionapp config appsettings ${PREFIX}functionapp"
az functionapp config appsettings set --name "${PREFIX}functionapp" --resource-group "${RG}" --settings "APPINSIGHTS_INSTRUMENTATIONKEY=$APPINSIGHTS_KEY" -o json >> azcli-execution.log

echo "create: eventgrid topic ${PREFIX}eventgrid"
az eventgrid topic create --name "${PREFIX}eventgrid" --location "${LOCATION}" --resource-group "${RG}" -o json >> azcli-execution.log

echo "create: eventgrid event-subscription function"
az eventgrid event-subscription create --name "function" --included-event-types "Microsoft.Storage.BlobCreated" --endpoint "https://${PREFIX}functionapp.azurewebsites.net/api/ClaimCheck" --endpoint-type "webhook" --source-resource-id "${SID}" -o json >> azcli-execution.log

echo "create: eventgrid event-subscription queue"
az eventgrid event-subscription create --name "queue" --included-event-types "Microsoft.Storage.BlobCreated" --endpoint "${SID}/queueservices/default/queues/${PREFIX}queue" --endpoint-type "storagequeue" --source-resource-id "${SID}" -o json >> azcli-execution.log

echo "done"

echo "The following values will be copied into App.config (using App.config.template as source):"

echo "StorageConnectionString = ${STORAGE_CONNECTION_STRING}"
sed "s|{StorageConnectionString}|${STORAGE_CONNECTION_STRING}|g" client-consumer/App.config.template > client-consumer/App.config

echo "StorageQueueName = ${PREFIX}queue"
sed -i.bak "s|{StorageQueueName}|${PREFIX}queue|g" client-consumer/App.config

rm -f client-consumer/App.config.bak

echo "done"