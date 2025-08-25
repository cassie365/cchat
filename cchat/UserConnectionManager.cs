using System.Text;

namespace cchat
{
    public class UserConnectionManager
    {
        private readonly Dictionary<Guid, UserConnection> connections = [];


        public void SendToAllAsync(UserConnection connection, string message)
        {
            foreach (UserConnection uc in connections.Values)
            {
                SendToUserAsync(connection, uc, message);
            }
        }

        public void SendToUserAsync(UserConnection senderConnection, Guid recipientId, string message)
        {
            UserConnection recipient = connections[recipientId];
            SendToUserAsync(senderConnection, recipient, message);
        }

        private async void SendToUserAsync(UserConnection senderConnection, UserConnection recipient, string message)
        {
            await recipient.SendMessageAsync(EncodeUserMessage(senderConnection, message));
        }

        public async Task<UserConnection?> CreateUserConnectionAsync(HttpContext ctx)
        {
            try
            {
                UserConnection connection = await UserConnection.CreateAsync(ctx);
                connections.Add(connection.Id, connection);

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

        public bool RemoveConnection(Guid guid)
        {
            UserConnection connection = connections[guid];
            connection.CloseAsync();
            return connections.Remove(guid);
        }

        private byte[] EncodeUserMessage(UserConnection connection, string message)
        {
            return Encoding.UTF8.GetBytes($"{connection.Username}: {message}");
        }
    }

}
