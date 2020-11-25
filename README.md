# Azure Batch + Azure Queue example

This project is designed as a working example for demonstrating using Azure Batch to drain objects off of an Azure Storage Queue.

This repo has two .NET Projects:
* `azure-batch` - Contains the Azure Batch client which creates the Pool, Job and tasks
* `workload` - Contains an sample app which serves as the workload for Azure Batch.  Pulls and pushes items from an Azure Queue

## Usage

1. Build the workload
   1. `cd workload`
   1. `dotnet publish -r linux-x64 -c Release --self-contained=true`
1. Push workload messages to the queue
   1. `export AZURE_STORAGE_CONNECTION_STRING="..."`
   1. `dotnet run produce 20 30`
      1. This pushes 20 messages, each with the value "30"
2. Publish the Azure Batch Application Package
   1. `cd bin/Release/net5.0/linux-x64/publish`
   2. `zip sleeper.zip *`
   3. Upload `sleeper.zip` to [Azure Batch application packages](https://docs.microsoft.com/en-us/azure/batch/batch-application-packages)
      1. `az batch application package create -n $BATCH_NAME -g $BATCH_RG --application-name sleeper --version-name 1.0 --package-file sleeper.zip`
3. Run the Azure Batch Job
   1. Update `accountsettings.json` with Batch and Storage account settings
   1. Update `settings.json` with the runtime settings
   2. From the azure-batch-queue directory `cd azure-batch`
   3. `dotnet run`