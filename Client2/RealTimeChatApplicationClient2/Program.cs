using Grpc.Net.Client;
using Grpc.Core;
using System;
using ChatApp.Protos;

class Program
{
    static async Task Main(string[] args)
    {
        using var channel = GrpcChannel.ForAddress("https://localhost:7265");
        var client = new Chat.ChatClient(channel);
    
        using var chat = client.ChatStream();
    
        // Start reading messages from the server
        // Stopwatch start
        var stopwatch = Stopwatch.StartNew();
        int counter = 0;
        var responseTask = Task.Run(async () =>
        {
            await foreach (var message in chat.ResponseStream.ReadAllAsync())
            {
                counter++;
                Console.WriteLine($"{message.User}: {message.Message}");
            }
        });
    
        Console.WriteLine("Enter your name:");
        var user = Console.ReadLine();
    
        Console.WriteLine("You can now start chatting:");
        string? line;
        while (string.IsNullOrWhiteSpace(line = Console.ReadLine()))
        {
            await chat.RequestStream.WriteAsync(new ChatMessage
            {
                User = user,
                Message = line + " " + counter
            });
        }
    
        await chat.RequestStream.CompleteAsync();
        await responseTask;
    
        stopwatch.Stop();
        Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
    }
}
