using System.Net.WebSockets;
using System.Text;

namespace cchat
{
    public sealed class UserConnection : IAsyncDisposable
    {
        private readonly HttpContext _ctx;
        public Guid Id { get; } = Guid.NewGuid();
        public WebSocket? Socket { get; private set; }
        public string Username { get; }

        public event Action<UserConnection>? Disposed;
        public bool IsClosing { get; private set; } = false;
        private string? CloseMessage { get; set; }
        private WebSocketCloseStatus? CloseStatus { get; set; }

        public UserConnection(HttpContext ctx)
        {
            _ctx = ctx;
            string? username = ctx.Request.Query["username"];
            if (username is null)
            {
                throw new InvalidOperationException("Missing username");
            }
            Username = username;
        }

        public static async Task<UserConnection> CreateAsync(HttpContext ctx)
        {
            UserConnection connection = new(ctx);
            connection.Socket = await ctx.WebSockets.AcceptWebSocketAsync();
            return connection;
        }


        public async ValueTask DisposeAsync()
        {
            if (Socket is null) return;
            try
            {
                if (Socket.State is WebSocketState.Open or WebSocketState.CloseReceived or WebSocketState.CloseSent)
                {
                    var message = CloseMessage ?? $"Websocket Connection {Id} closed by server.";
                    WebSocketCloseStatus closeStatus = CloseStatus ?? WebSocketCloseStatus.NormalClosure;
                    await Socket.CloseAsync(closeStatus, message, CancellationToken.None);
                }
            }
            catch (Exception e) 
            {
                    Console.WriteLine($"Error when disposing UserConnection: {e.Message}");
            }

            Socket.Dispose();

            // Notify that his has been disposed
            // to perform cleanup operations outside of this class
            Disposed?.Invoke(this);
        }

        public async Task<string?> ReceiveMessageAsync()
        {
            if (Socket is null) return null;

            const int MaxMessageBytes = 1 * 1024 * 1024;

            var buffer = new byte[8 * 1024];

            var ms = new MemoryStream(capacity: 8 * 1024);
            WebSocketReceiveResult result;
            do
            {
                result = await Socket.ReceiveAsync(buffer, _ctx.RequestAborted);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Client requested to terminate connection.");
                    MarkClosed(WebSocketCloseStatus.NormalClosure, "Client requested to terminate connection.");
                    return null;
                }

                if (result.MessageType != WebSocketMessageType.Text)
                {
                    // Ignore any binary frames for now
                    return null;
                }

                if (ms.Length + result.Count > MaxMessageBytes)
                {
                    Console.WriteLine($"Message from {Id} exceeded limit ({MaxMessageBytes} bytes). Closing socket.");
                    MarkClosed(WebSocketCloseStatus.MessageTooBig, $"Message exceeded limit ({MaxMessageBytes} bytes)");
                    return null;
                }

                ms.Write(buffer.AsSpan(0, result.Count));
            }
            while (!result.EndOfMessage);

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private void MarkClosed(WebSocketCloseStatus closeStatus, string? closeMessage = null)
        {
            CloseMessage = closeMessage;
            CloseStatus = closeStatus;
            IsClosing = true;
        }

        public async Task<bool> SendMessageAsync(byte[] message)
        {
            if (Socket is null || !IsOpen()) return false;

            try
            {
                await Socket.SendAsync(message, WebSocketMessageType.Text, endOfMessage: true, _ctx.RequestAborted);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message to {Id}: {ex.Message}");
                return false;
            }
        }

        public bool IsOpen()
        {
            if (Socket is null) return false;

            var socketIsOpen = Socket.State == WebSocketState.Open;
            var requestCancellationRequested = _ctx.RequestAborted.IsCancellationRequested;

            return socketIsOpen && !requestCancellationRequested && !IsClosing;
        }

    }

}
