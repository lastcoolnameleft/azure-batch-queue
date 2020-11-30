using System;
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
                int count;
                int duration;
                int.TryParse(args[1], out count);
                int.TryParse(args[2], out duration);
                Console.WriteLine($"Going to Product {count} Messages. Value: {duration}");
                await ProduceMessages(queue, count, duration);
                return 0;
            } else if (args[0] == "consume") {
                Console.WriteLine($"Going to Consume Messages");
                await ConsumeMessages(queue);
                return 0;
            } else {
                Console.WriteLine($"Unexpected value {args[1]}");
                return 1;
            }
        }

        static async Task ConsumeMessages(QueueClient queue)
        {
            try {
                QueueProperties properties = await queue.GetPropertiesAsync();

                if (properties != null) {
                    while (properties.ApproximateMessagesCount > 0)
                    {
                        int duration;
                        QueueMessage[] retrievedMessage = await queue.ReceiveMessagesAsync(1, TimeSpan.FromSeconds(40));
                        if (retrievedMessage.Length > 0) {
                            //QueueMessage[] retrievedMessage = await queue.ReceiveMessagesAsync(1, TimeSpan.FromSeconds(40));
                            duration = int.Parse(retrievedMessage[0].MessageText);
                            Console.WriteLine($"Sleeping for {duration} seconds");
                            Thread.Sleep(duration * 1000);
                            await queue.DeleteMessageAsync(retrievedMessage[0].MessageId, retrievedMessage[0].PopReceipt);
                            properties = await queue.GetPropertiesAsync();
                        } else {
                            Console.WriteLine($"Queue drained. (retrievedMessage.Length = 0)");
                            return;
                        }
                    }
                }
                Console.WriteLine($"Queue drained. (properties = null)");
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
        static async Task ProduceMessages(QueueClient queue, int count, int duration) 
        {
            int i;
            for (i = 0; i < count; i++) {
                Console.WriteLine($"Inserting {duration} into queue.");
               await InsertMessageAsync(queue, duration.ToString()); 
            }
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
