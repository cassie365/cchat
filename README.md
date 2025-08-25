# CChat — Minimal WebSocket chat (for fun)

A tiny .NET 8 WebSocket chat server that broadcasts text messages from any client to all connected clients. It was created for learning/fun and is not production‑ready.

## Requirements
- .NET SDK 8.0+
- Visual Studio 2022 (optional)

## Run

- Visual Studio:
  - Open the solution/project and use __Debug > Start Debugging__ (F5) or __Debug > Start Without Debugging__ (Ctrl+F5).
- CLI:
  - From the `cchat` directory: `dotnet run`

## WebSocket endpoint
Note: It is recommended that you test this from Postman (see below) as most browser will not allow you to open an unsecure web socket connection!
- Path: `/`
- Query string parameter (required): `username`
- Text frames only. Binary frames are ignored; Close frames terminate the connection.

Example:
- `ws://localhost:5000/?username=alice`

Server behavior:
- Each received text message is broadcast to all connections as: `Username: message`.

## Test with Postman (WebSocket)

1. Open Postman and create a new WebSocket Request.
2. In the URL box, enter the endpoint with a username, e.g.:
   - `ws://localhost:5000/?username=alice` (HTTP)
3. Click Connect.
4. In a second tab, connect a different user, e.g.:
   - `ws://localhost:5000/?username=bob`
5. Type a text message in one tab and Send. Both tabs should receive a message like:
   - `alice: Hello world`
6. To test broadcast further, open more tabs with different `username` values.

Notes:
- If `username` is missing, the server will respond with HTTP 500 and the WebSocket will not upgrade.
- Only text messages are processed; other frame types are ignored.
- The server sends keep-alive pings every 30 seconds.

## Why this is insecure (by design)
This project was a way for me to learn WebSocket and play around with the concept.

This project is NOT ready to ship in its current state:
- No authentication or authorization. Anyone knowing the URL can connect and spoof `username`.
- No transport security when using `ws://` (plaintext). Use `wss://` + a valid certificate in real deployments.
- No input validation, rate limiting, or message size caps beyond a rolling buffer; large or frequent messages could impact memory/CPU.
- No origin checks (no CSRF/CORS style restrictions for WebSocket origins).
- Broadcast trust: messages are forwarded as-is to every client.
- Concurrency and error handling are minimal. Exceptions may be lost and concurrent mutations could race.

Use this as a learning toy! Do not run this in production!

## Project structure

- `Program.cs` — minimal hosting + WebSocket endpoint at `/`
- `UserConnection.cs` — per-connection WebSocket I/O
- `UserConnectionManager.cs` — in-memory connection registry + broadcast logic
