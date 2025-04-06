using Grpc.Net.Client;
using Grpc.Core;
using ChatApp.Protos;

class Program
{
    static async Task Main(string[] args)
    {
        using var channel = GrpcChannel.ForAddress("https://localhost:7265");
        var client = new Chat.ChatClient(channel);

        using var chat = client.ChatStream();

        // Start reading messages from the server
        var responseTask = Task.Run(async () =>
        {
            await foreach (var message in chat.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine($"{message.User}: {message.Message}");
            }
        });

        // Sending messages
        Console.WriteLine("Enter your name:");
        var user = Console.ReadLine();

        Console.WriteLine("You can now start chatting:");
        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            await chat.RequestStream.WriteAsync(new ChatMessage
            {
                User = user,
                Message = line
            });
        }

        await chat.RequestStream.CompleteAsync();
        await responseTask;
    }
}