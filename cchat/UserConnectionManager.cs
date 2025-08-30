using System.Text;

namespace cchat
{
    public class UserConnectionManager
    {
        private readonly Dictionary<Guid, UserConnection> connections = [];


        public void BulkSend(UserConnection connection, string message)
        {
            List<Task> messages = [];
            var recipientCount = 0;
            var failureCount = 0;
            foreach (UserConnection uc in connections.Values)
            {
                try
                {
                    messages.Add(SendToUserAsync(connection, uc, message).ContinueWith(sentTask =>
                    {
                        return sentTask.Result ? recipientCount++ : failureCount++;
                    }, TaskContinuationOptions.OnlyOnRanToCompletion));

                } catch (Exception ex) {

                    Console.WriteLine($"Error during Bulk Send: {ex.Message}");

                }
                
            }

            Task.WhenAll(messages).ContinueWith(task =>
            {
                Console.WriteLine($"Bulk message succeeded. Sent to {recipientCount} recipients and had {failureCount} failures");
            });
        }

        public async Task<bool> SendToUserAsync(UserConnection senderConnection, Guid recipientId, string message)
        {
            UserConnection recipient = connections[recipientId];
            return await SendToUserAsync(senderConnection, recipient, message);
        }

        private async Task<bool> SendToUserAsync(UserConnection senderConnection, UserConnection recipient, string message)
        {
            return await recipient.SendMessageAsync(EncodeUserMessage(senderConnection, message));
        }

        public async Task<UserConnection?> CreateUserConnectionAsync(HttpContext ctx)
        {
            try
            {
                UserConnection connection = await UserConnection.CreateAsync(ctx);
                connections.Add(connection.Id, connection);
                connection.Disposed += OnConnectionDisposed;

                return connection;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create user connection: {ex.Message}");
                return null;
            }
        }

        public UserConnection GetUserConnection(Guid connectionId)
        {
            return connections[connectionId];
        }

        public async Task<bool> RemoveConnection(Guid guid)
        {
            if (!connections.TryGetValue(guid, out UserConnection? connection)) return false;
            await connection.DisposeAsync();
            return true;
        }

        private void OnConnectionDisposed(UserConnection connection)
        {
            connections.Remove(connection.Id);
            connection.Disposed -= OnConnectionDisposed;
        }

        private byte[] EncodeUserMessage(UserConnection connection, string message)
        {
            return Encoding.UTF8.GetBytes($"{connection.Username}: {message}");
        }
    }

}
