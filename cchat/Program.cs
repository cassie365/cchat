using cchat;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();
UserConnectionManager cm = new();



app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30),
});


app.Map("/", async (HttpContext ctx) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = 400;
        return;
    }

    UserConnection? connection = await cm.CreateUserConnectionAsync(ctx);

    if (connection == null)
    {
        ctx.Response.StatusCode = 500;
        return;
    }

    await using (connection)
    {
        while (connection.IsOpen())
        {
            string? message = await connection.ReceiveMessageAsync();
            if (message is null) continue;
            cm.SendToAllAsync(connection, message);
        }
    }

});

app.Run();
