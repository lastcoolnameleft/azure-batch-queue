using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace sleeper_queue
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine($"Running on {System.Environment.MachineName}");
            
            string connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

            if (args.Length == 0)
            {
                Console.WriteLine("First argument must be 'consume' or 'produce'");
                Console.WriteLine("Usage: sleeper-queue consume or sleeper-queue produce <num>");
                return 1;
            }

         

            QueueClient queue = new QueueClient(connectionString, "sleeper");

            if (args[0] == "produce")
            {
                // int proCount = Environment.ProcessorCount;

                //setting the default limit as 64 for getting more throughput for sending messages
                int proCount = 64;

                ServicePointManager.DefaultConnectionLimit = proCount;

                int count;
                int duration;
                int.TryParse(args[1], out count);
                int.TryParse(args[2], out duration);
                Console.WriteLine($"Going to Product {count} Messages. Value: {duration}");

                List<Task<bool>> tasks = new List<Task<bool>>();

                Stopwatch stopwatch = new Stopwatch();

                // Begin timing.
                stopwatch.Start();

                for (int i = 0; i < proCount; i++)
                {
                    tasks.Add(Task.Run(() => ProduceMessages(queue, count, duration)));
                }

                Task t = Task.WhenAll(tasks.ToArray());
                try
                {
                    await t;
                }
                catch (AggregateException) { }

                if (t.Status == TaskStatus.RanToCompletion)
                {
                    Console.WriteLine("All tasks are done with sending messages");
                    // Stop timing.
                    stopwatch.Stop();

                    // Write result.
                    Console.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);

                }               
                else if (t.Status == TaskStatus.Faulted)
                    Console.WriteLine("Task failed");
                else
                {
                    foreach (var ct in tasks)
                        Console.WriteLine("Task {0}: {1} with result {2}", ct.Id, ct.Status, ct.Result);
                }

                return 0;
            }
            else if (args[0] == "consume")
            {
                int proCount = Environment.ProcessorCount;
                
                ServicePointManager.DefaultConnectionLimit = proCount;

                Console.WriteLine($"Going to Consume Messages");

                List<Task<bool>> tasks = new List<Task<bool>>();

                for (int i = 0; i < proCount; i++)
                {
                    tasks.Add(Task.Run(() => ConsumeMessages(queue)));
                }

                Task t = Task.WhenAll(tasks.ToArray());
                try
                {
                    await t;
                }
                catch (AggregateException) { }

                if (t.Status == TaskStatus.RanToCompletion)
                    Console.WriteLine("All tasks are done");
                else if (t.Status == TaskStatus.Faulted)
                    Console.WriteLine("Task failed");
                else
                {
                    foreach (var ct in tasks)
                        Console.WriteLine("Task {0}: {1} with result {2}", ct.Id, ct.Status, ct.Result);
                }

                return 0;
            }
            else
            {
                Console.WriteLine($"Unexpected value {args[1]}");
                return 1;
            }
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
                        int duration = 0;
                        QueueMessage[] retrievedMessage = await queue.ReceiveMessagesAsync(1, TimeSpan.FromSeconds(40));
                        if (retrievedMessage.Length > 0)
                        {
                            //QueueMessage[] retrievedMessage = await queue.ReceiveMessagesAsync(1, TimeSpan.FromSeconds(40));
                            if (!String.IsNullOrEmpty(retrievedMessage[0].MessageText))
                            {
                                duration = Convert.ToInt32(retrievedMessage[0].MessageText);

                                Console.WriteLine($"Sleeping for {retrievedMessage[0].MessageText} seconds");
                            }

                            Thread.Sleep(duration * 1000);

                            var tId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                            Console.WriteLine("Processed message {0} in thread {1}", retrievedMessage[0].MessageId, tId.ToString());

                            await queue.DeleteMessageAsync(retrievedMessage[0].MessageId, retrievedMessage[0].PopReceipt);

                            properties = await queue.GetPropertiesAsync();

                        }
                        else
                        {

                            Console.WriteLine($"Queue drained. (retrievedMessage.Length = 0)");

                            return false;
                        }
                    }
                }
                Console.WriteLine($"Queue drained. (properties = null)");
                return true;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return false;
            }
        }
        static async Task<bool> ProduceMessages(QueueClient queue, int count, int duration)
        {
            int i;
            for (i = 0; i < count; i++)
            {
                Console.WriteLine($"Inserting {duration} into queue.");
                await InsertMessageAsync(queue, duration.ToString());
            }

            return true;
        }

        static async Task InsertMessageAsync(QueueClient theQueue, string newMessage)
        {
            if (null != await theQueue.CreateIfNotExistsAsync())
            {
                Console.WriteLine("The queue was created.");
            }

            await theQueue.SendMessageAsync(newMessage);
        }

    }
}
