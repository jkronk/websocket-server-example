using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace WebSocketServer.Middleware
{
    public class WebSocketServerConnectionManager
    {
        private ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

        public ConcurrentDictionary<string, WebSocket> GetAllSockets()
        {
            return _sockets;
        }

        public string AddSocket(WebSocket socket)
        {
            string connectionId = Guid.NewGuid().ToString();
            _sockets.TryAdd(connectionId, socket);
            Console.WriteLine("Connection Added: " + connectionId);
            return connectionId;
        }
    }
}