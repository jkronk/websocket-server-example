using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Newtonsoft.Json;

namespace WebSocketServer.Middleware
{
    public class WebSocketServerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WebSocketServerConnectionManager _manager;

        public WebSocketServerMiddleware(RequestDelegate next, WebSocketServerConnectionManager manager)
        {
            _next = next;
            _manager = manager;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                Console.WriteLine("Connected");

                string connId = _manager.AddSocket(webSocket);
                await SendConnectionIdAsync(webSocket, connId);

                await ReceiveMessage(webSocket, async (result, buffer) =>
                {
                    //also have byte[]
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        Console.WriteLine($"Message: {Encoding.UTF8.GetString(buffer, 0, result.Count)}");
                        await RouteJsonMessageAsync(Encoding.UTF8.GetString(buffer, 0, result.Count));
                        return;
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        string id = _manager.GetAllSockets().FirstOrDefault(x => x.Value == webSocket).Key;
                        _manager.GetAllSockets().TryRemove(id, out WebSocket sock);
                        await sock.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                        Console.WriteLine("Rx close message");                        
                        return;
                    }
                });
            }
            else
            {
                Console.WriteLine("Hello from 2nd request delegate");
                await _next(context);
            }
        }

        private async Task SendConnectionIdAsync(WebSocket socket, string connectionId)
        {
            var buffer = Encoding.UTF8.GetBytes("ConnID: " + connectionId);
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task ReceiveMessage(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                handleMessage(result, buffer);
            }
        }

        public async Task RouteJsonMessageAsync(string message)
        {
            var routeOb = JsonConvert.DeserializeObject<dynamic>(message);

            if (Guid.TryParse(routeOb.To.ToString(), out Guid guidOut))
            {
                Console.WriteLine("Targetted message");
                var sock = _manager.GetAllSockets().FirstOrDefault(x => x.Key == routeOb.To.ToString());
                if (sock.Value != null)
                {
                    if (sock.Value.State == WebSocketState.Open)
                    {
                        await sock.Value.SendAsync(Encoding.UTF8.GetBytes(routeOb.Message.ToString()), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                else
                {
                    Console.WriteLine("Invalid recipient");
                }
            }
            else
            {
                Console.WriteLine("Broadcast message");
                foreach (var socket in _manager.GetAllSockets())
                {
                    if (socket.Value.State == WebSocketState.Open)
                    {
                        await socket.Value.SendAsync(Encoding.UTF8.GetBytes(routeOb.Message.ToString()), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
        }
    }
}