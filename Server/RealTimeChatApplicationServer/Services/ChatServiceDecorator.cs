using System.Collections.Concurrent;
using System.Net.Sockets;
using ChatApplication;
using Grpc.Core;
using Microsoft.AspNetCore.Connections;

namespace RealTimeChatApplicationServer.Services
{
    public class ChatServiceDecorator : Chat.ChatBase
    {
        private static readonly ConcurrentDictionary<IServerStreamWriter<ChatMessage>, bool> Clients = new();

        public override async Task ChatStream(
            IAsyncStreamReader<ChatMessage> requestStream,
            IServerStreamWriter<ChatMessage> responseStream,
            ServerCallContext context)
        {
            Clients.TryAdd(responseStream, true);
            try
            {
                while (true)
                {
                    if (!await SafeMoveNext(requestStream, context.CancellationToken))
                    {
                        break;
                    }

                    var message = requestStream.Current;
                    var disconnectedClients = new ConcurrentBag<IServerStreamWriter<ChatMessage>>();

                    foreach (var client in Clients.Keys)
                    {
                        if (!await SafeWriteAsync(client, message))
                        {
                            disconnectedClients.Add(client);
                        }
                    }

                    foreach (var client in disconnectedClients)
                    {
                        Clients.TryRemove(client, out _);
                    }
                }
            }
            finally
            {
                Clients.TryRemove(responseStream, out _);
            }
        }

        private static async Task<bool> SafeMoveNext(IAsyncStreamReader<ChatMessage> requestStream, CancellationToken cancellationToken)
        {
            try
            {
                return await requestStream.MoveNext(cancellationToken);
            }
            catch (IOException ex) when (ex.InnerException is ConnectionAbortedException || ex.InnerException is ConnectionResetException || ex.InnerException is SocketException)
            {
                return false;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled || ex.StatusCode == StatusCode.Unavailable)
            {
                return false;
            }
        }

        private static async Task<bool> SafeWriteAsync(IServerStreamWriter<ChatMessage> client, ChatMessage message)
        {
            try
            {
                await client.WriteAsync(message);
                return true;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled || ex.StatusCode == StatusCode.Unavailable)
            {
                return false;
            }
        }
    }
}
