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
                string message = $"Connection {Id} closed by server.";
                await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, message, CancellationToken.None);
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

        public async Task<string?> RecieveMessageAsync()
        {
            if (Socket is null) return null;

            var buffer = new byte[8 * 1024];
            var result = await Socket.ReceiveAsync(buffer, _ctx.RequestAborted);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine("Client requested to terminate connection.");
                return null;
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                return null;
            }

            var ms = new MemoryStream();
            ms.Write(buffer.AsSpan(0, result.Count));
            while (!result.EndOfMessage)
            {
                result = await Socket.ReceiveAsync(buffer, _ctx.RequestAborted);
                ms.Write(buffer.AsSpan(0, result.Count));
            }

            return Encoding.UTF8.GetString(ms.ToArray());
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

            bool socketIsOpen = Socket.State == WebSocketState.Open;
            bool requestCancellationRequested = _ctx.RequestAborted.IsCancellationRequested;

            return socketIsOpen && !requestCancellationRequested;
        }

    }

}
