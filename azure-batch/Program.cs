﻿//Copyright (c) Microsoft Corporation

namespace Microsoft.Azure.Batch.Samples.HelloWorld
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Auth;
    using Batch.Common;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// The main program of the HelloWorld sample
    /// </summary>
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                AccountSettings accountSettings = SampleHelpers.LoadAccountSettings();

                Settings helloWorldSettings = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("settings.json")
                    .Build()
                    .Get<Settings>();

                HelloWorldAsync(accountSettings, helloWorldSettings).Wait();
            }
            catch (AggregateException aggregateException)
            {
                // Go through all exceptions and dump useful information
                foreach (Exception exception in aggregateException.InnerExceptions)
                {
                    Console.WriteLine(exception.ToString());
                    Console.WriteLine();
                }

                throw;
            }

            Console.WriteLine("Press return to exit...");
            //Console.ReadLine();
        }

        public static string CreateJobId(string prefix)
        {
            // a job is uniquely identified by its ID so your account name along with a timestamp is added as suffix
            return string.Format("{0}-{1}-{2}", prefix, Environment.UserName, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        }

        /// <summary>
        /// Submits a job to the Azure Batch service, and waits for it to complete
        /// </summary>
        private static async Task HelloWorldAsync(AccountSettings accountSettings, Settings helloWorldConfigurationSettings)
        {
            Console.WriteLine("Running with the following settings: ");
            Console.WriteLine("-------------------------------------");
            Console.WriteLine(helloWorldConfigurationSettings.ToString());
            Console.WriteLine(accountSettings.ToString());

            // Set up the Batch Service credentials used to authenticate with the Batch Service.
            BatchSharedKeyCredentials credentials = new BatchSharedKeyCredentials(
                accountSettings.BatchServiceUrl,
                accountSettings.BatchAccountName,
                accountSettings.BatchAccountKey);

            // Get an instance of the BatchClient for a given Azure Batch account.
            using (BatchClient batchClient = BatchClient.Open(credentials))
            {
                // add a retry policy. The built-in policies are No Retry (default), Linear Retry, and Exponential Retry
                batchClient.CustomBehaviors.Add(RetryPolicyProvider.ExponentialRetryProvider(TimeSpan.FromSeconds(5), 3));

                string jobId = CreateJobId("HelloWorldJob");

                try
                {
                    // Submit the job
                    await SubmitJobAsync(batchClient, helloWorldConfigurationSettings, accountSettings, jobId);

                    // Wait for the job to complete
                    await WaitForJobAndPrintOutputAsync(batchClient, jobId);
                }
                finally
                {
                    // Delete the job to ensure the tasks are cleaned up
                    if (!string.IsNullOrEmpty(jobId) && !helloWorldConfigurationSettings.JobKeepAlive)
                    {
                        Console.WriteLine("Deleting job: {0}", jobId);
                        await batchClient.JobOperations.DeleteJobAsync(jobId);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a job and adds a task to it.
        /// </summary>
        /// <param name="batchClient">The BatchClient to use when interacting with the Batch service.</param>
        /// <param name="configurationSettings">The configuration settings</param>
        /// <param name="jobId">The ID of the job.</param>
        /// <returns>An asynchronous <see cref="Task"/> representing the operation.</returns>
        private static async Task SubmitJobAsync(BatchClient batchClient, Settings configurationSettings, AccountSettings accountSettings, string jobId)
        {
            // create an empty unbound Job
            CloudJob unboundJob = batchClient.JobOperations.CreateJob();
            unboundJob.Id = jobId;
            unboundJob.CommonEnvironmentSettings = new List<EnvironmentSetting>
            {
                new EnvironmentSetting("AZURE_STORAGE_CONNECTION_STRING", accountSettings.StorageConnectionString)
            };

            // For this job, ask the Batch service to automatically create a pool of VMs when the job is submitted.
            unboundJob.PoolInformation = new PoolInformation()
            {
                AutoPoolSpecification = new AutoPoolSpecification()
                {
                    AutoPoolIdPrefix = "HelloWorld",
                    PoolSpecification = new PoolSpecification()
                    {
                        TargetDedicatedComputeNodes = configurationSettings.PoolTargetNodeCount,
                        VirtualMachineConfiguration = new VirtualMachineConfiguration(
                            new ImageReference(
                                "UbuntuServer", "Canonical", "18.04-LTS", "latest"
                            ),
                            "batch.node.ubuntu 18.04"
                        ),
                        TaskSlotsPerNode = configurationSettings.TaskSlotsPerNode,
                        TaskSchedulingPolicy = new TaskSchedulingPolicy(ComputeNodeFillType.Spread),
                        VirtualMachineSize = configurationSettings.PoolNodeVirtualMachineSize,
                        ApplicationPackageReferences = new List<ApplicationPackageReference>
                        {
                            new ApplicationPackageReference {
                                    ApplicationId = configurationSettings.ApplicationId,
                                    Version = configurationSettings.ApplicationVersion }
                        },
                    },
                    KeepAlive = configurationSettings.PoolKeepAlive,
                    PoolLifetimeOption = PoolLifetimeOption.Job
                }
            };

            // Commit Job to create it in the service
            await unboundJob.CommitAsync();

            // create a simple task. Each task within a job must have a unique ID
            //await batchClient.JobOperations.AddTaskAsync(jobId, new CloudTask("task-env", "env"));
            //await batchClient.JobOperations.AddTaskAsync(jobId, new CloudTask("task-hostname", "/bin/sh -c \"hostname\""));
            // Task could should equal poolTargetNodeCount * # of cores in VM SKU
            //TODO https://docs.microsoft.com/en-us/azure/batch/large-number-tasks#example-batch-net
            // Create list of CloudTasks and addTaskAysnc in single operation.
            int i;
            List<CloudTask> tasksToAdd = new List<CloudTask>(); // Populate with your tasks
            //string cmd = "/bin/sh -c \"$AZ_BATCH_APP_PACKAGE_sleeper_1_2/sleeper-queue consume\"";
            for (i = 0; i < configurationSettings.TaskCount; i++) {
                tasksToAdd.Add(new CloudTask($"{configurationSettings.ApplicationId}-{i}", configurationSettings.TaskCommand));
            }
            // https://docs.microsoft.com/en-us/azure/batch/large-number-tasks#example-batch-net
            BatchClientParallelOptions parallelOptions = new BatchClientParallelOptions()
            {
                MaxDegreeOfParallelism = configurationSettings.MaxDegreeOfParallelism
            };
            await batchClient.JobOperations.AddTaskAsync(jobId, tasksToAdd, parallelOptions);
        }

        /// <summary>
        /// Waits for all tasks under the specified job to complete and then prints each task's output to the console.
        /// </summary>
        /// <param name="batchClient">The BatchClient to use when interacting with the Batch service.</param>
        /// <param name="jobId">The ID of the job.</param>
        /// <returns>An asynchronous <see cref="Task"/> representing the operation.</returns>
        private static async Task WaitForJobAndPrintOutputAsync(BatchClient batchClient, string jobId)
        {
            Console.WriteLine("Waiting for all tasks to complete on job: {0} ...", jobId);

            // We use the task state monitor to monitor the state of our tasks -- in this case we will wait for them all to complete.
            TaskStateMonitor taskStateMonitor = batchClient.Utilities.CreateTaskStateMonitor();

            List<CloudTask> ourTasks = await batchClient.JobOperations.ListTasks(jobId).ToListAsync();

            // Wait for all tasks to reach the completed state.
            // If the pool is being resized then enough time is needed for the nodes to reach the idle state in order
            // for tasks to run on them.
            await taskStateMonitor.WhenAll(ourTasks, TaskState.Completed, TimeSpan.FromMinutes(20));

            // dump task output
            foreach (CloudTask t in ourTasks)
            {
                Console.WriteLine("Task {0}", t.Id);

                //Read the standard out of the task
                NodeFile standardOutFile = await t.GetNodeFileAsync(Constants.StandardOutFileName);
                string standardOutText = await standardOutFile.ReadAsStringAsync();
                Console.WriteLine("Standard out:");
                Console.WriteLine(standardOutText);
                Console.WriteLine();
            }
        }
    }
}
