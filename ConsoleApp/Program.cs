using System;
using System.Text.Json;
using System.Threading;
using Confluent.Kafka;

class Program
{
    static void Main(string[] args)
    {
        var config = new ConsumerConfig
        {
            // Pointing to your Docker Kafka container's external port
            BootstrapServers = "localhost:9092",
            GroupId = "debezium-poc-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();

        // Debezium automatically names topics: prefix.database.schema.table
        consumer.Subscribe("testenv.DebeziumTest.dbo.Employee");

        CancellationTokenSource cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        Console.WriteLine("Listening for SQL Server CDC events...");

        try
        {
            while (true)
            {
                var consumeResult = consumer.Consume(cts.Token);
                var jsonMessage = consumeResult.Message.Value;

                // Parse the Debezium JSON envelope
                using JsonDocument doc = JsonDocument.Parse(jsonMessage);
                JsonElement root = doc.RootElement;

                // Ensure we are looking at the payload data, not just the schema envelope
                if (root.TryGetProperty("payload", out JsonElement payload) && payload.ValueKind != JsonValueKind.Null)
                {
                    string operation = payload.GetProperty("op").GetString();

                    Console.WriteLine($"\n--- Database Change Detected ---");
                    Console.WriteLine($"Operation Type: {operation} (c=create, u=update, d=delete)");

                    if (operation == "c" || operation == "u")
                    {
                        Console.WriteLine("New Row Data: " + payload.GetProperty("after").GetRawText());
                    }
                    if (operation == "u" || operation == "d")
                    {
                        Console.WriteLine("Old Row Data: " + payload.GetProperty("before").GetRawText());
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            consumer.Close();
        }
    }
}