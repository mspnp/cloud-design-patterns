#!/bin/bash
set -e

if ! command az >/dev/null; then
    echo "Must install Azure CLI: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli" >&2
    exit 1
fi

export curVer=`printf "%03d%03d%03d" $(az --version | grep 'azure-cli ' | awk '{gsub(/[()]/, "", $2); print $2}' | tail -1 | tr '.' ' ')`
export reqVer=`printf "%03d%03d%03d" $(echo '2.0.50' | tr '.' ' ')`
if [ ${curVer} -lt ${reqVer} ] ; then
    echo "Version 2.0.50 at least required for eventhubs with kafka" >&2    
    echo 'You can add it by following instructions given here' >&2
    echo 'https://docs.microsoft.com/en-us/cli/azure/install-azure-cli' >&2
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
    export RG="pnp4"
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

echo "create: storage account container heavypayload"
az storage container create --name "heavypayload" --connection-string "${STORAGE_CONNECTION_STRING}" -o json >> azcli-execution.log

echo "create: eventhubs namespace ${PREFIX}ehubns"
az eventhubs namespace create --name "${PREFIX}ehubns" --resource-group "${RG}" \
--sku Standard --location "${LOCATION}" --capacity 8 --enable-kafka True \
-o json >> azcli-execution.log

echo "create: eventhub ${PREFIX}ehub instance"
az eventhubs eventhub create --name "${PREFIX}ehub" --resource-group "${RG}" \
--message-retention 1 --partition-count 1 --namespace-name "${PREFIX}ehubns" \
-o json >> azcli-execution.log

echo "show event hub name ${PREFIX}ehub"
export EVENTHUB_NAME="${PREFIX}ehub"

echo "show: event hub ${PREFIX}ehub connection string"
export EVENTHUB_CS=$(az eventhubs namespace authorization-rule keys list --resource-group "${RG}" --namespace-name ${PREFIX}ehubns --name RootManageSharedAccessKey --query "primaryConnectionString" -o tsv)

echo "create: consumer group"
az eventhubs eventhub consumer-group create --name "${PREFIX}ehubcg" --resource-group "${RG}" \
--eventhub-name "${PREFIX}ehub" --namespace-name "${PREFIX}ehubns" \
-o json >> azcli-execution.log

echo "create: resource ${PREFIX}functionappinsights"
az resource create --name "${PREFIX}functionappinsights" --location "${LOCATION}" --properties '{"Application_Type": "other", "ApplicationId": "function", "Flow_Type": "Redfield"}' --resource-group "${RG}" --resource-type "Microsoft.Insights/components" -o json >> azcli-execution.log

echo "show: resource ${PREFIX}functionappinsights"
export APPINSIGHTS_KEY=$(az resource show --name "${PREFIX}functionappinsights" --query "properties.InstrumentationKey" --resource-group "${RG}" --resource-type "Microsoft.Insights/components" -o tsv)

echo "create: appservice plan ${PREFIX}plan"
az appservice plan create --name "${PREFIX}plan" --number-of-workers "1" --resource-group "${RG}" --sku "S1" -o json >> azcli-execution.log

echo "create: functionapp ${PREFIX}functionapp"
az functionapp create --name "${PREFIX}functionapp" --plan "${PREFIX}plan" --resource-group "${RG}" --storage-account "${PREFIX}storage" -o json >> azcli-execution.log

echo "config-zip: functionapp deployment source ${PREFIX}functionapp"
az functionapp deployment source config-zip --name "${PREFIX}functionapp" --resource-group "${RG}" --src "./sample-4-bin.zip" -o json >> azcli-execution.log

echo "set: functionapp config appsettings for appinsights"
az functionapp config appsettings set --name "${PREFIX}functionapp" --resource-group "${RG}" --settings "APPINSIGHTS_INSTRUMENTATIONKEY=$APPINSIGHTS_KEY" -o json >> azcli-execution.log

echo "set: functionapp config appsettings for storage account connection string"
az functionapp config appsettings set --name "${PREFIX}functionapp" --resource-group "${RG}" --settings "STORAGE_CONNECTION_STRING=$STORAGE_CONNECTION_STRING" -o json >> azcli-execution.log

echo "set: functionapp config appsettings for event hub connection string"
az functionapp config appsettings set --name "${PREFIX}functionapp" --resource-group "${RG}" --settings "EventHubConnectionAppSetting=$EVENTHUB_CS" -o json >> azcli-execution.log

echo "set: functionapp config appsettings for event hub name"
az functionapp config appsettings set --name "${PREFIX}functionapp" --resource-group "${RG}" --settings "EVENTHUB_NAME=$EVENTHUB_NAME" -o json >> azcli-execution.log

echo "set: functionapp config appsettings for event hub consumer group"
az functionapp config appsettings set --name "${PREFIX}functionapp" --resource-group "${RG}" --settings "EVENTHUB_NAME_CONSUMER_GROUP=${PREFIX}ehubcg" -o json >> azcli-execution.log

echo "done"

echo "The following values will be copied into App.config (using App.config.template as source):"

echo "STORAGE_CONNECTION_STRING = ${STORAGE_CONNECTION_STRING}"
sed "s|{STORAGE_CONNECTION_STRING}|${STORAGE_CONNECTION_STRING}|g" client-producer/App.config.template > client-producer/App.config

echo "EH_FQDN = ${PREFIX}ehubns.servicebus.windows.net:9093"
sed -i.bak "s|{EH_FQDN}|${PREFIX}ehubns.servicebus.windows.net:9093|g" client-producer/App.config

echo "EH_CONNECTION_STRING = ${EVENTHUB_CS}"
sed -i.bak "s|{EH_CONNECTION_STRING}|${EVENTHUB_CS}|g" client-producer/App.config

echo "EH_NAME = ${EVENTHUB_NAME}"
sed -i.bak "s|{EH_NAME}|${EVENTHUB_NAME}|g" client-producer/App.config

rm -f client-producer/App.config.bak

echo "done"
