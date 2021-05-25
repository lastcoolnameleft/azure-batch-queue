using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;

namespace qeda_batch
{
    class Program
    {
        static string connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
        static string batchAccountName = Environment.GetEnvironmentVariable("BATCH_ACCOUNT_NAME");
        static string batchAccountKey = Environment.GetEnvironmentVariable("BATCH_ACCOUNT_KEY");
        static string batchAccountUrl = Environment.GetEnvironmentVariable("BATCH_ACCOUNT_URL");
        static string qedaMonitoringQueueName = Environment.GetEnvironmentVariable("JOB_QUEUE");
        static string jobName = Environment.GetEnvironmentVariable("JOB_ID");

        static async Task<int> Main(string[] args)
        {          

            Console.WriteLine($"Running on {System.Environment.MachineName}");           

            QueueClient queueClient = new QueueClient(connectionString, qedaMonitoringQueueName);

            Console.WriteLine($"Going to Consume Messages");

            await ConsumeMessages(queueClient);

            return 0;

        }
        
        static async Task<bool> ConsumeMessages(QueueClient queue)
        {
            try
            {
                QueueProperties properties = await queue.GetPropertiesAsync();

                if (properties != null)
                {
                    while (properties.ApproximateMessagesCount > 0)
                    {
                        await SubmitTasksAsync();
                    }
                }
                Console.WriteLine($"{DateTime.Now} Queue drained. (properties = null)");
                return true;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return false;
            }
        }

        static async Task SubmitTasksAsync()
        {
            List<CloudTask> tasks = new List<CloudTask>();

            BatchSharedKeyCredentials cred = new BatchSharedKeyCredentials(batchAccountUrl, batchAccountName, batchAccountKey);

            using (BatchClient batchClient = BatchClient.Open(cred))
            {
                for (int i = 0; i < 10; i++)
                {
                    string taskId = String.Format("Task{0}", i);

                    string taskCommandLine = String.Format("cmd /");

                    CloudTask task = new CloudTask(taskId, taskCommandLine);

                    tasks.Add(task);
                }

                await batchClient.JobOperations.AddTaskAsync(jobName, tasks);
            }
                      
        }
    }
}
