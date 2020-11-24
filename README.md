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
   1. `dotnet run produce 2 2`
1. Publish the Azure Batch Application Package
   1. `cd bin/Release/net5.0/linux-x64/publish`
   1. `zip sleeper.zip`
   1. Upload `sleeper.zip` to Azure Batch application packages
1. Run the Azure Batch Job
   1. Update `accountsettings.json` with Batch and Storage account settings
   2. From the azure-batch-queue directory `cd azure-batch`
   3. `dotnet run`